using System;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Helluva;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Item vermelho "Contrato da I.M.P.": se o time tem o item, o primeiro elite ou campeão que nascer no estágio
    // vira o alvo do contrato (marcado com Death Mark, visível a todos). Matá-lo paga ouro extra
    // (ouro do alvo x cópias do time) e solta um item verde. Um contrato por estágio. Só no host.
    // Aviso e marcador na tela de TODOS: o host reenvia a cada 1 s um efeito (ContractBeacon) que referencia o alvo;
    // cada cópia some em 1,1 s, então o marcador some sozinho quando o alvo morre ou o estágio acaba.
    public sealed class ContractFeature : Feature
    {
        public const string ItemName = "Ror2MeeeeeImpContract";
        public const string Token = "ROR2MEEEEE_ITEM_IMPCONTRACT";

        private static ItemDef _def;
        private static ItemIndex _index = ItemIndex.None;

        private static ItemIndex Index
        {
            get
            {
                if (_index == ItemIndex.None && _def) _index = _def.itemIndex;
                return _index;
            }
        }

        private CharacterMaster _target;
        private bool _doneThisStage;
        private float _targetSince;
        private readonly BepInEx.Configuration.ConfigEntry<int> _maxGold;
        private uint _maxGoldValue;
        private Xoroshiro128Plus _rng; // do mod: não consome RNG da run

        private const float BeaconSeconds = 1f;
        private static GameObject _beacon; // efeito com ContractBeacon; null = só o chat avisa
        private bool _ticking;
        private float _nextBeacon;
        private int _bannerSends; // primeiros envios pedem o aviso (cobre um envio perdido ou alvo ainda não visto no cliente)

        private const string NewToken = Token + "_CHAT_NEW", DoneToken = Token + "_CHAT_DONE";

        public static void Register(ManualLogSource log)
        {
            Loc.Add(NewToken, "<color=#e06c75>I.M.P.:</color> new contract! Eliminate {0} (marked). Payment: ${1} extra and an item.",
                "<color=#e06c75>I.M.P.:</color> novo contrato! Eliminem {0} (marcado). Pagamento: R$ {1} extra e um item.");
            Loc.Add(DoneToken, "<color=#e06c75>I.M.P.:</color> contract fulfilled! ${0} extra and an item for the head.",
                "<color=#e06c75>I.M.P.:</color> contrato cumprido! R$ {0} extra e um item pela cabeça.");
            _def = ModItem.Register(ItemName, Token, ItemTier.Tier3, new[] { ItemTag.Utility, ItemTag.AIBlacklist },
                "contrato_imp.png", new Color32(200, 40, 60, 255),
                ("I.M.P. Contract",
                 "One target per stage. Kill it and get paid.",
                 "At the start of each stage, the first <style=cIsDamage>elite or boss</style> becomes the contract target " +
                 "(<style=cDeath>marked for death</style>). Killing it pays <style=cShrine>double gold</style> " +
                 "<style=cStack>(+100% per stack)</style> and drops a <style=cIsHealing>green item</style>.",
                 "We kill people! And we're good at it!"),
                ("Contrato da I.M.P.",
                 "Um alvo por estágio. Mate-o e receba o pagamento.",
                 "No início de cada estágio, o primeiro <style=cIsDamage>elite ou chefe</style> vira o alvo do contrato " +
                 "(<style=cDeath>marcado para morrer</style>). Matá-lo paga <style=cShrine>ouro em dobro</style> " +
                 "<style=cStack>(+100% por cópia)</style> e solta um <style=cIsHealing>item verde</style>.",
                 "Matamos pessoas! E fazemos isso bem!"),
                log, "RoR2/Base/DeathMark/PickupDeathMark.prefab");
            try
            {
                var holder = new GameObject("Ror2MeeeeeContractHolder") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.Object.DontDestroyOnLoad(holder); // sem isto a 1ª troca de cena destrói o prefab e o envio é pulado
                holder.SetActive(false);
                var root = new GameObject("Ror2MeeeeeContractBeacon");
                root.transform.SetParent(holder.transform, false);
                root.AddComponent<EffectComponent>();
                var vfx = root.AddComponent<VFXAttributes>();
                vfx.vfxPriority = VFXAttributes.VFXPriority.Always;
                vfx.DoNotPool = true; // ContractBeacon usa Start/OnDestroy
                root.AddComponent<DestroyOnTimer>().duration = BeaconSeconds + 0.1f;
                root.AddComponent<ContractBeacon>();
                if (R2API.ContentAddition.AddEffect(root)) _beacon = root;
                else log.LogWarning("ImpContract: ContentAddition recusou o marcador; aviso só no chat.");
            }
            catch (Exception e)
            {
                log.LogWarning($"ImpContract: falha ao criar o marcador (aviso só no chat): {e.Message}");
            }
        }

        public ContractFeature(Cfg cfg, ManualLogSource log) : base("ImpContract", cfg, log,
            "I.M.P. Contract item (red): the first elite or boss of the stage becomes a marked target; killing it pays extra gold and drops a green item. Off: the item exists but does nothing.",
            "Item Contrato da I.M.P. (vermelho): o primeiro elite ou chefe do estágio vira alvo marcado; matá-lo paga ouro extra e solta um item verde. Desligado: o item existe mas não faz nada.",
            enabledByDefault: true)
        {
            _maxGold = cfg.Bind("ImpContract", "MaxGold", 20000,
                "Cap on the extra gold paid per contract (the target's gold grows with difficulty and has gone past 80k). 0 = no cap.",
                "Teto do ouro extra pago por contrato (o ouro do alvo cresce com a dificuldade e já passou de 80 mil). 0 = sem teto.");
            _maxGoldValue = (uint)Math.Max(0, _maxGold.Value);
            _maxGold.SettingChanged += (_, __) => _maxGoldValue = (uint)Math.Max(0, _maxGold.Value);
        }

        public override void ResetRunState()
        {
            _target = null;
            _doneThisStage = false;
            _rng = null;
            SetTicking(false);
        }

        private void SetTicking(bool on)
        {
            on &= _beacon != null;
            if (on == _ticking) return;
            if (on) RoR2Application.onFixedUpdate += OnFixedUpdate;
            else RoR2Application.onFixedUpdate -= OnFixedUpdate;
            _ticking = on;
        }

        // Só com alvo ativo (SetTicking): reenvia o efeito-marcador a cada BeaconSeconds.
        private void OnFixedUpdate()
        {
            try
            {
                if (!NetworkServer.active || !_target) { SetTicking(false); return; }
                var now = Time.time;
                if (now < _nextBeacon) return;
                var body = _target.GetBody();
                if (!body) return;
                _nextBeacon = now + BeaconSeconds;
                var data = new EffectData { origin = body.corePosition, rotation = Quaternion.identity, genericUInt = _bannerSends > 0 ? 1u : 0u };
                data.SetNetworkedObjectReference(body.gameObject);
                EffectManager.SpawnEffect(_beacon, data, transmit: true);
                if (_bannerSends > 0) _bannerSends--;
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        protected override void OnEnable()
        {
            Track("Stage.onServerStageBegin", () => Stage.onServerStageBegin += OnStageBegin);
            Track("CharacterBody.onBodyStartGlobal", () => CharacterBody.onBodyStartGlobal += OnBodyStart);
            Track("GlobalEventManager.onCharacterDeathServer", () => GlobalEventManager.onCharacterDeathServer += OnDeath);
        }

        protected override void OnDisable()
        {
            Stage.onServerStageBegin -= OnStageBegin;
            CharacterBody.onBodyStartGlobal -= OnBodyStart;
            GlobalEventManager.onCharacterDeathServer -= OnDeath;
            _target = null;
            SetTicking(false);
        }

        private void OnStageBegin(Stage stage)
        {
            if (_target) Log.LogInfo($"[ImpContract] alvo não eliminado no estágio anterior (vivo há {Time.time - _targetSince:F0} s).");
            _target = null;
            _doneThisStage = false;
            SetTicking(false);
        }

        private static int TeamStacks()
        {
            var idx = Index;
            if (idx == ItemIndex.None) return 0;
            var args = new Util.GetTotalItemCountFromTeamArgs(idx);
            args.requiredTeam = TeamIndex.Player;
            args.requiresAlive = false;
            args.requiresConnected = true;
            args.onlyPlayerControlledCharacters = true;
            return Util.GetTotalItemCountFromTeam(args);
        }

        // Escolhe o alvo: primeiro elite/campeão de monstro a nascer no estágio (se o time tem o item).
        private void OnBodyStart(CharacterBody body)
        {
            try
            {
                if (_doneThisStage || _target || !NetworkServer.active || !body) return;
                if (!body.isElite && !body.isChampion) return;
                var team = body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.None;
                if (team != TeamIndex.Monster && team != TeamIndex.Void && team != TeamIndex.Lunar) return;
                var master = body.master;
                if (!master || TeamStacks() <= 0) return;

                _target = master;
                _targetSince = Time.time;
                body.AddBuff(RoR2Content.Buffs.DeathMark); // marca visível (caveira) para todos
                _nextBeacon = 0f;
                _bannerSends = 3;
                SetTicking(true);
                var gold = body.TryGetComponent<DeathRewards>(out var dr) ? HelluvaRules.ContractGoldCapped(dr.goldReward, TeamStacks(), _maxGoldValue) : 0u;
                Loc.Broadcast(NewToken, Util.GetBestBodyName(body.gameObject), gold.ToString());
                Log.LogInfo($"[ImpContract] alvo: {master.name} (ouro extra {gold})");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnDeath(DamageReport report)
        {
            try
            {
                if (!_target || report.victimMaster != _target) return;
                _target = null;
                _doneThisStage = true;
                SetTicking(false);

                var stacks = TeamStacks();
                var body = report.victimBody;
                var gold = body && body.TryGetComponent<DeathRewards>(out var dr) ? HelluvaRules.ContractGoldCapped(dr.goldReward, stacks, _maxGoldValue) : 0u;
                if (gold > 0 && TeamManager.instance) TeamManager.instance.GiveTeamMoney(TeamIndex.Player, gold, bypassStatManager: false);

                var run = Run.instance;
                if (run && run.availableTier2DropList.Count > 0)
                {
                    if (_rng == null) _rng = new Xoroshiro128Plus(run.seed ^ 0x494D50434F4E5452UL); // "IMPCONTR"
                    var pos = body ? body.corePosition : report.damageInfo.position;
                    PickupDropletController.CreatePickupDroplet(new UniquePickup(_rng.NextElementUniform(run.availableTier2DropList)), pos, Vector3.up * 20f, isDuplicated: false);
                }
                Loc.Broadcast(DoneToken, gold.ToString());
                Log.LogInfo($"[ImpContract] cumprido em {Time.time - _targetSince:F0} s: +{gold} ouro, item verde");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }

    // Vai no efeito-marcador do contrato; roda em cada máquina que o recebe (host incluso). Só local (Overlay).
    internal sealed class ContractBeacon : MonoBehaviour
    {
        private static readonly Color LabelColor = new Color(0.9f, 0.25f, 0.3f);

        private void Start()
        {
            var ec = GetComponent<EffectComponent>();
            var data = ec ? ec.effectData : null;
            var target = data != null ? data.ResolveNetworkedObjectReference() : null;
            if (!target) return; // alvo ainda não existe neste cliente: o próximo envio (1 s) resolve
            Overlay.Mark(this, target.transform, Loc.T("CONTRACT TARGET", "ALVO DO CONTRATO"), LabelColor, 26);
            if (data.genericUInt == 1u)
                Overlay.Banner(Loc.T($"I.M.P.: new contract! Eliminate {Util.GetBestBodyName(target)} (marked in red).",
                    $"I.M.P.: novo contrato! Elimine {Util.GetBestBodyName(target)} (marcado em vermelho)."));
        }

        private void OnDestroy() => Overlay.Unmark(this);
    }
}
