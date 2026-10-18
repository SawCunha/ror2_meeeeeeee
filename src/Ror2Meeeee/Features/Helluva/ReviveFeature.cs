using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using RoR2;
using Ror2Meeeee.Core.Helluva;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

#pragma warning disable CS0618 // smallIcon*Sprite: campos obsoletos no 1.5.0, mas ainda lidos pelo jogo

namespace Ror2Meeeee
{
    // Artefato da Ressurreição: jogador que morre deixa uma lápide (pulso de luz no chão + modelo do Dio's Best Friend
    // com marcador na tela de todos: efeito transmitido pelo host, todos têm o mod). Um aliado vivo que
    // fique perto dela por RequiredSeconds seguidos o revive ali: volta com metade da vida e 3 s de imunidade;
    // quem reviveu perde metade da vida atual (sem morrer). Só no host.
    // Custo: o tick (onFixedUpdate) só fica registrado enquanto existe lápide; sem mortos, custo zero.
    public sealed class ReviveFeature : Feature
    {
        public const string ArtifactName = "Ror2MeeeeeRevive";
        public const string Token = "ROR2MEEEEE_ARTIFACT_REVIVE";
        private const string FellToken = Token + "_CHAT_FELL", RevivedToken = Token + "_CHAT_REVIVED";

        private const float PulseSeconds = 1f;
        private const float PulseWindow = 2f; // o efeito do pulso tem som: só toca nos primeiros segundos da lápide
        private const float ReviverHealthFraction = 0.5f;
        private const float RevivedHealthFraction = 0.5f;

        private static ArtifactDef _artifact;
        private static GameObject _silentPulse; // clone de ShrineUseEffect sem som; null = não deu para criar
        private static GameObject _tombModel;   // efeito: modelo do Dio's Best Friend + TombstoneBeacon; null = sem modelo
        private const float ModelSeconds = 1.1f; // reenviado a cada PulseSeconds: o novo nasce antes do antigo sumir
        private const float ModelHeight = 1.5f;

        private sealed class Tombstone
        {
            public CharacterMaster Master;
            public Vector3 Position;
            public float Progress;
            public float NextPulse;
            public float Created;
        }

        private readonly List<Tombstone> _tombstones = new List<Tombstone>();
        private readonly HashSet<CharacterMaster> _halfHealthOnSpawn = new HashSet<CharacterMaster>();
        private readonly ConfigEntry<float> _radius;
        private readonly ConfigEntry<float> _seconds;
        private float _radiusSqr;
        private float _secondsValue;
        private bool _ticking;
        private bool _pulseWarned;
        private GameObject _pulseEffect;
        private GameObject _rezEffect;

        public static void Register(ManualLogSource log)
        {
            try
            {
                _artifact = ScriptableObject.CreateInstance<ArtifactDef>();
                _artifact.cachedName = ArtifactName;
                _artifact.nameToken = Token + "_NAME";
                _artifact.descriptionToken = Token + "_DESC";
                _artifact.smallIconSelectedSprite = TitheArtifact.LoadIcon("ressurreicao_selecionado.png", new Color32(240, 220, 120, 255));
                _artifact.smallIconDeselectedSprite = TitheArtifact.LoadIcon("ressurreicao_desmarcado.png", new Color32(90, 90, 90, 255));
                Loc.Add(Token + "_NAME", "Artifact of Resurrection", "Artefato da Ressurreição");
                Loc.Add(Token + "_DESC",
                    "Whoever dies leaves a tombstone. An ally who stays near it for a few seconds revives the player with half health, paying with half of their own health.",
                    "Quem morre deixa uma lápide. Um aliado que fique perto dela por alguns segundos revive o jogador com metade da vida, " +
                    "pagando com metade da própria vida.");
                Loc.Add(FellToken, "<color=#e5c07b>Tombstone:</color> {0} fell. Stay near the tombstone for {1} s to revive (costs half your health).",
                    "<color=#e5c07b>Lápide:</color> {0} caiu. Fique perto da lápide por {1} s para reviver (custa metade da sua vida).");
                Loc.Add(RevivedToken, "<color=#e5c07b>Tombstone:</color> {0} revived {1}! (paid {2} health)",
                    "<color=#e5c07b>Lápide:</color> {0} reviveu {1}! (pagou {2} de vida)");
                RegisterSilentPulse(log);
                RegisterTombModel(log);
                if (!ContentAddition.AddArtifactDef(_artifact)) { log.LogError("Revive: ContentAddition recusou o artefato."); _artifact = null; }
                else log.LogInfo($"Revive: artefato '{ArtifactName}' registrado.");
            }
            catch (Exception e)
            {
                log.LogError($"Revive: falha ao registrar o artefato (o plugin segue sem ele): {e}");
                _artifact = null;
            }
        }

        // Os pulsos repetidos tocavam o som do santuário a cada segundo, para todos. O clone sem AkEvent/soundName
        // mantém o visual; o som original só toca uma vez, na criação da lápide.
        private static void RegisterSilentPulse(ManualLogSource log)
        {
            try
            {
                var src = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect");
                if (!src) { log.LogWarning("Revive: ShrineUseEffect indisponível no registro; pulsos seguem com som (só nos 2 primeiros s)."); return; }
                var holder = new GameObject("Ror2MeeeeeReviveHolder") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.Object.DontDestroyOnLoad(holder); // sem isto a 1ª troca de cena destrói o prefab e o envio é pulado
                holder.SetActive(false); // o clone não "acorda" nem toca nada aqui
                var clone = UnityEngine.Object.Instantiate(src, holder.transform);
                clone.name = "Ror2MeeeeeRevivePulse";
                var removed = 0;
                foreach (var ev in clone.GetComponentsInChildren<AkEvent>(true)) { UnityEngine.Object.DestroyImmediate(ev); removed++; }
                var ec = clone.GetComponent<EffectComponent>();
                var sound = ec ? ec.soundName : null;
                if (ec) ec.soundName = "";
                if (!ContentAddition.AddEffect(clone)) { log.LogWarning("Revive: ContentAddition recusou o pulso silencioso."); return; }
                _silentPulse = clone;
                log.LogInfo($"Revive: pulso silencioso criado (AkEvent removidos: {removed}; soundName original: '{sound}').");
            }
            catch (Exception e)
            {
                log.LogWarning($"Revive: falha ao criar o pulso silencioso (segue com o original): {e.Message}");
            }
        }

        // Efeito próprio (não objeto de rede): o host envia a cada PulseSeconds e cada cópia some sozinha em ModelSeconds,
        // então a lápide removida desaparece em ~1 s para todos, sem mensagem extra.
        private static void RegisterTombModel(ManualLogSource log)
        {
            try
            {
                var src = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ExtraLife/PickupHippo.prefab").WaitForCompletion();
                if (!src) { log.LogWarning("Revive: modelo PickupHippo indisponível; lápide só com pulso e marcador do host."); return; }
                var holder = new GameObject("Ror2MeeeeeTombHolder") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.Object.DontDestroyOnLoad(holder); // sem isto a 1ª troca de cena destrói o prefab e o envio é pulado
                holder.SetActive(false);
                var root = new GameObject("Ror2MeeeeeTombstone");
                root.transform.SetParent(holder.transform, false);
                var model = UnityEngine.Object.Instantiate(src, root.transform);
                model.transform.localPosition = Vector3.up * ModelHeight; // tamanho final: TombstoneBeacon.FitModel
                foreach (var c in model.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
                root.AddComponent<EffectComponent>();
                var vfx = root.AddComponent<VFXAttributes>();
                vfx.vfxPriority = VFXAttributes.VFXPriority.Always; // não ser cortado quando há muitos efeitos
                vfx.DoNotPool = true; // TombstoneBeacon usa Start/OnDestroy
                root.AddComponent<DestroyOnTimer>().duration = ModelSeconds;
                root.AddComponent<TombstoneBeacon>();
                if (!ContentAddition.AddEffect(root)) { log.LogWarning("Revive: ContentAddition recusou o modelo da lápide."); return; }
                _tombModel = root;
                log.LogInfo("Revive: modelo da lápide (PickupHippo) registrado.");
            }
            catch (Exception e)
            {
                log.LogWarning($"Revive: falha ao criar o modelo da lápide (segue sem ele): {e.Message}");
            }
        }

        private static bool ArtifactOn()
        {
            var mgr = RunArtifactManager.instance;
            return _artifact && mgr && mgr.IsArtifactEnabled(_artifact);
        }

        public ReviveFeature(Cfg cfg, ManualLogSource log) : base("Revive", cfg, log,
            "Artifact of Resurrection: with the artifact enabled in the lobby, whoever dies leaves a tombstone; an ally near it for a few seconds revives the player (half health), paying half of their own health. Only makes sense in multiplayer.", "Artefato da Ressurreição: com o artefato ativado no lobby, quem morre deixa uma lápide; um aliado perto dela por alguns segundos revive o jogador (metade da vida), pagando metade da própria vida. Só faz sentido em multiplayer.",
            enabledByDefault: true)
        {
            _radius = cfg.Bind("Revive", "Radius", 4f, "Distance (meters) from the tombstone the ally needs to stay within.", "Distância (metros) da lápide em que o aliado precisa ficar.");
            _seconds = cfg.Bind("Revive", "RequiredSeconds", 3f, "Consecutive seconds near the tombstone to revive. Leaving resets the timer.", "Segundos seguidos perto da lápide para reviver. Sair de perto zera o tempo.");
            Refresh();
            _radius.SettingChanged += (_, __) => Refresh();
            _seconds.SettingChanged += (_, __) => Refresh();
        }

        private void Refresh()
        {
            var r = Mathf.Max(0.5f, _radius.Value);
            _radiusSqr = r * r;
            _secondsValue = Mathf.Max(0.1f, _seconds.Value);
        }

        public override void ResetRunState() => ClearAll();

        protected override void OnEnable()
        {
            Track("GlobalEventManager.onCharacterDeathServer", () => GlobalEventManager.onCharacterDeathServer += OnDeath);
            Track("CharacterBody.onBodyStartGlobal", () => CharacterBody.onBodyStartGlobal += OnBodyStart);
            Track("Stage.onServerStageBegin", () => Stage.onServerStageBegin += OnStageBegin);
        }

        protected override void OnDisable()
        {
            GlobalEventManager.onCharacterDeathServer -= OnDeath;
            CharacterBody.onBodyStartGlobal -= OnBodyStart;
            Stage.onServerStageBegin -= OnStageBegin;
            ClearAll();
        }

        private void ClearAll()
        {
            _tombstones.Clear();
            Diag.Tombstones = 0;
            _halfHealthOnSpawn.Clear();
            SetTicking(false);
        }

        private void SetTicking(bool on)
        {
            if (on == _ticking) return;
            if (on) RoR2Application.onFixedUpdate += OnFixedUpdate;
            else RoR2Application.onFixedUpdate -= OnFixedUpdate;
            _ticking = on;
        }

        // Próximo estágio: todos renascem pelo jogo; lápides antigas somem.
        private void OnStageBegin(Stage stage)
        {
            ClearAll();
            if (NetworkServer.active)
                Log.LogInfo($"[Revive] estágio {stage.sceneDef?.cachedName}: artefato {(ArtifactOn() ? "ATIVO" : "desligado (ative no lobby)")}");
        }

        private void OnDeath(DamageReport report)
        {
            try
            {
                if (!NetworkServer.active) return;
                var body = report.victimBody;
                var master = report.victimMaster;
                // Jogador = master com PlayerCharacterMasterController (body.isPlayerControlled é redundante).
                if (!body || !master || !master.playerCharacterMasterController) return;
                if (body.isRemoteOp) return; // morreu o drone operado à distância, não o jogador
                if (WillReviveByExtraLife(master))
                {
                    Log.LogInfo($"[Revive] {Util.GetBestMasterName(master)} morreu com vida extra (Dio's Best Friend): o jogo revive, sem lápide.");
                    return;
                }
                if (!ArtifactOn())
                {
                    Log.LogInfo($"[Revive] {Util.GetBestMasterName(master)} morreu, mas o artefato não está ativo nesta run.");
                    return;
                }
                for (var i = 0; i < _tombstones.Count; i++)
                    if (_tombstones[i].Master == master) return;

                var tomb = new Tombstone { Master = master, Position = SafePosition(body.footPosition, master, out var how), Created = Time.time };
                _tombstones.Add(tomb);
                Diag.Tombstones = _tombstones.Count;
                if (how != "original") Log.LogInfo($"[Revive] posição da lápide ajustada ({how}); morte em {body.footPosition}, lápide em {tomb.Position}.");
                var who = Util.GetBestMasterName(master);
                Overlay.Banner(Loc.T($"TOMBSTONE: {who} fell. Stay {_secondsValue:F0} s near the tombstone to revive (costs half your health).",
                    $"LÁPIDE: {who} caiu. Fique {_secondsValue:F0} s perto da lápide para reviver (custa metade da sua vida)."));
                SetTicking(true);
                Log.LogInfo($"[Revive] lápide de {Util.GetBestMasterName(master)} criada (raio {Mathf.Sqrt(_radiusSqr):F1} m, {_secondsValue:F1} s).");
                Loc.Broadcast(FellToken, who, _secondsValue.ToString("F0", Loc.EnCulture));
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Respawn define a vida cheia no Start do corpo; aqui (depois dele) baixa para a metade.
        private void OnBodyStart(CharacterBody body)
        {
            try
            {
                if (_halfHealthOnSpawn.Count == 0 || !NetworkServer.active || !body) return; // todo corpo que nasce passa aqui
                var master = body.master;
                if (!master || !_halfHealthOnSpawn.Remove(master)) return;
                var hc = body.healthComponent;
                if (hc) hc.Networkhealth = Mathf.Max(1f, hc.fullHealth * RevivedHealthFraction);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnFixedUpdate()
        {
            try
            {
                if (!NetworkServer.active || _tombstones.Count == 0) { SetTicking(false); return; }
                var dt = Time.fixedDeltaTime;
                var now = Time.time;
                for (var i = _tombstones.Count - 1; i >= 0; i--)
                {
                    var t = _tombstones[i];
                    // Some se o jogador saiu ou já voltou por outro meio (Dio, próximo estágio...).
                    // Operação remota (DLC3): o morto controla um drone vivo; isso NÃO conta como ter voltado.
                    var deadBody = t.Master ? t.Master.GetBody() : null;
                    if (!t.Master || (deadBody && !deadBody.isRemoteOp && deadBody.healthComponent && deadBody.healthComponent.alive))
                    {
                        Log.LogInfo($"[Revive] lápide removida após {now - t.Created:F0} s: {(t.Master ? Util.GetBestMasterName(t.Master) : "jogador saiu")} já está vivo ou saiu.");
                        _tombstones.RemoveAt(i);
                        continue;
                    }

                    if (now >= t.NextPulse)
                    {
                        var first = t.NextPulse == 0f;
                        t.NextPulse = now + PulseSeconds;
                        if (_silentPulse || now - t.Created < PulseWindow) Pulse(t.Position, first);
                        if (_tombModel)
                            EffectManager.SpawnEffect(_tombModel, new EffectData
                            {
                                origin = t.Position,
                                rotation = Quaternion.identity,
                                genericUInt = t.Master.netId.Value, // o TombstoneBeacon acha o nome do morto em cada máquina
                            }, transmit: true);
                    }

                    var reviver = AllyNear(t);
                    if (HelluvaRules.ReviveStep(ref t.Progress, reviver, dt, _secondsValue))
                    {
                        Revive(t, reviver);
                        _tombstones.RemoveAt(i);
                    }
                }
                Diag.Tombstones = _tombstones.Count;
                if (_tombstones.Count == 0) SetTicking(false);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Vida extra do jogo: ainda tem Dio's Best Friend (ou a versão do Vazio) ou o jogo já agendou a volta (item consumido
        // nesta morte). Equipamentos de reviver que dependem de uso manual não contam: sem lápide o jogador ficaria preso.
        private static bool WillReviveByExtraLife(CharacterMaster master)
        {
            var inv = master.inventory;
            if (inv && (inv.GetItemCountEffective(RoR2Content.Items.ExtraLife) > 0 || inv.GetItemCountEffective(DLC1Content.Items.ExtraLifeVoid) > 0))
                return true;
            return master.IsExtraLifePendingServer();
        }

        // Morte fora do mapa (vazio, zona de morte) deixaria a lápide onde ninguém chega e o respawn cairia de novo.
        // Com chão até 30 m abaixo: cola a lápide nele (também cobre morte no ar). Sem chão: vai para o aliado vivo
        // mais próximo; sem aliado, mantém o ponto original.
        private static Vector3 SafePosition(Vector3 death, CharacterMaster dead, out string how)
        {
            how = "chão";
            if (Physics.Raycast(death + Vector3.up, Vector3.down, out var hit, 30f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                return hit.point;

            how = "sem chão: aliado mais próximo";

            var best = death;
            var bestSqr = float.MaxValue;
            var list = PlayerCharacterMasterController.instances;
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i] ? list[i].master : null;
                if (!m || m == dead) continue;
                var b = m.GetBody();
                if (!b || b.isRemoteOp || !b.healthComponent || !b.healthComponent.alive) continue;
                var d = (b.footPosition - death).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = b.footPosition; }
            }
            if (bestSqr == float.MaxValue) how = "original";
            return best;
        }

        // Primeiro jogador vivo (que não seja o morto) dentro do raio; null se nenhum.
        private CharacterBody AllyNear(Tombstone t)
        {
            var list = PlayerCharacterMasterController.instances;
            for (var i = 0; i < list.Count; i++)
            {
                var pcmc = list[i];
                var m = pcmc ? pcmc.master : null;
                if (!m || m == t.Master) continue;
                var b = m.GetBody();
                if (!b || b.isRemoteOp || !b.healthComponent || !b.healthComponent.alive) continue; // operando drone = morto
                if ((b.footPosition - t.Position).sqrMagnitude <= _radiusSqr) return b;
            }
            return null;
        }

        // Coluna de pulsos empilhados (0 a 9 m) para a lápide ser vista de longe. O primeiro pulso usa o efeito
        // original (com som); os seguintes, o clone silencioso. Sem clone, o original só nos primeiros PulseWindow s.
        private void Pulse(Vector3 position, bool first)
        {
            if (!_pulseEffect) _pulseEffect = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect");
            var prefab = first || !_silentPulse ? _pulseEffect : _silentPulse;
            if (!prefab)
            {
                if (!_pulseWarned) { _pulseWarned = true; Log.LogWarning("[Revive] efeito ShrineUseEffect não carregou; lápide sem marcador visual."); }
                return;
            }
            for (var h = 0; h <= 9; h += 3)
                EffectManager.SpawnEffect(prefab, new EffectData
                {
                    origin = position + Vector3.up * h,
                    rotation = Quaternion.identity,
                    scale = 4f,
                    color = new Color32(240, 220, 120, 255),
                }, transmit: true);
        }

        private void Revive(Tombstone t, CharacterBody reviver)
        {
            _halfHealthOnSpawn.Add(t.Master);
            var body = t.Master.Respawn(t.Position, Quaternion.identity, wasRevivedMidStage: true);
            if (body) body.AddTimedBuff(RoR2Content.Buffs.Immune, 3f);

            if (!_rezEffect) _rezEffect = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/HippoRezEffect");
            if (_rezEffect)
                EffectManager.SpawnEffect(_rezEffect, new EffectData { origin = t.Position, rotation = Quaternion.identity }, transmit: true);

            var hc = reviver ? reviver.healthComponent : null;
            var cost = hc ? HelluvaRules.ReviverCost(hc.health, ReviverHealthFraction) : 0f;
            if (cost > 0f)
            {
                // Cobrança, como a taxa do Taxade: passa direto, sem passos do BodyHooks (sem bloqueio nem aumento).
                BodyHooks.RawDamage = true;
                try
                {
                    hc.TakeDamage(new DamageInfo
                    {
                        damage = cost,
                        position = reviver.corePosition,
                        damageType = (DamageTypeCombo)(DamageType.BypassArmor | DamageType.BypassBlock | DamageType.BypassOneShotProtection | DamageType.NonLethal),
                        damageColorIndex = DamageColorIndex.Bleed,
                    });
                }
                finally { BodyHooks.RawDamage = false; }
            }
            var who = Util.GetBestMasterName(t.Master);
            var by = reviver ? Util.GetBestBodyName(reviver.gameObject) : "?";
            Loc.Broadcast(RevivedToken, by, who, cost.ToString("F0", Loc.EnCulture));
            Diag.Revives++;
            Log.LogInfo($"[Revive] {by} reviveu {who} após {Time.time - t.Created:F0} s; custo {cost:F0} de vida");
        }
    }
    // Vai no efeito da lápide; roda em cada máquina que recebe o efeito. Marcador grande, só local (Overlay).
    // Sem pool (DoNotPool): Start já tem o EffectData; OnDestroy tira o marcador quando a cópia some.
    internal sealed class TombstoneBeacon : MonoBehaviour
    {
        private const int LabelSize = 30;
        private const float ModelTargetHeight = 2.5f; // metros
        private static readonly Color LabelColor = new Color(0.95f, 0.85f, 0.35f);
        private static bool _logged;

        private void Start()
        {
            var size = FitModel();
            var ec = GetComponent<EffectComponent>();
            var id = ec && ec.effectData != null ? ec.effectData.genericUInt : 0u;
            var go = id != 0u ? Util.FindNetworkObject(new NetworkInstanceId(id)) : null;
            var master = go ? go.GetComponent<CharacterMaster>() : null;
            var label = master ? Loc.T($"{Util.GetBestMasterName(master)}'s TOMBSTONE", $"LÁPIDE de {Util.GetBestMasterName(master)}") : Loc.T("TOMBSTONE", "LÁPIDE");
            Overlay.Mark(this, transform.position + Vector3.up * 2f, label, LabelColor, LabelSize);
            if (!_logged)
            {
                _logged = true;
                Diag.Log?.LogInfo($"[Revive] lápide visível nesta máquina: '{label}' em {transform.position}, modelo {size} (1ª vez na sessão)");
            }
        }

        // A escala nativa dos modelos de pickup varia (o PickupDisplay do jogo a normaliza): mede o modelo já criado
        // e ajusta para ModelTargetHeight. Devolve um resumo para o log.
        private string FitModel()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return "sem renderer";
            var bounds = new Bounds();
            var any = false;
            foreach (var r in renderers)
            {
                r.enabled = true;
                if (r is ParticleSystemRenderer) continue;
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            if (!any || bounds.size.y <= 0.0001f) return $"{renderers.Length} renderer(s), altura 0";
            var model = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (model) model.localScale *= ModelTargetHeight / bounds.size.y;
            return $"{renderers.Length} renderer(s), altura {bounds.size.y:F2} m -> {ModelTargetHeight:F1} m";
        }

        private void OnDestroy() => Overlay.Unmark(this);
    }
}
