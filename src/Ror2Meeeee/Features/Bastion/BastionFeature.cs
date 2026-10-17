using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Stats;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Item vermelho "Bastião": escudo de 40% da vida máxima por cópia (Core.Stats.Bastion) e recarga que começa 2 s depois do
    // último golpe. Escudo: passo 2 do BodyHooks.RecalculateStats, sobre a vida já multiplicada pelo Coração Pesado (o
    // BodyHooks reajusta o escudo atual). Recarga: tick de 0,25 s no host chama HealthComponent.ForceShieldRegen(), a
    // mesma recarga do jogo (50%/s), que o próximo golpe cancela sozinho. Hook e tick só existem enquanto alguém tem o item.
    public sealed class BastionFeature : Feature, IHolderSet
    {
        public const string ItemName = "Ror2MeeeeeBastion";
        public const string Token = "ROR2MEEEEE_ITEM_BASTION";
        private const float Interval = 0.25f;

        private static readonly Action<CharacterBody, float> SetMaxShield = Setter("maxShield");
        private static readonly Action<CharacterBody, float> SetMaxBarrier = Setter("maxBarrier");
        private static readonly Action<CharacterBody, float> SetMaxHull = Setter("maxHull");

        private static Action<CharacterBody, float> Setter(string property)
        {
            var set = typeof(CharacterBody).GetProperty(property)?.GetSetMethod(true);
            return set == null ? null : (Action<CharacterBody, float>)Delegate.CreateDelegate(typeof(Action<CharacterBody, float>), set);
        }

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

        private readonly Dictionary<GameObject, CharacterBody> _holders = new Dictionary<GameObject, CharacterBody>();
        private readonly List<CharacterBody> _tickList = new List<CharacterBody>();
        private bool _ticking;
        private float _acc;

        public bool HasHolders => _holders.Count > 0;
        public bool Holds(GameObject body) => _holders.ContainsKey(body);

        public static void Register(ManualLogSource log)
        {
            var pt = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
            var p = (Bastion.ShieldPerStack * 100f).ToString("0.##", pt) + "%";
            var d = Bastion.RegenDelaySeconds.ToString("0.#", pt);
            _def = ModItem.Register(ItemName, Token, ItemTier.Tier3, new[] { ItemTag.Utility, ItemTag.AIBlacklist },
                "bastiao.png", new Color32(90, 170, 240, 255),
                "Bastião",
                "Um escudo enorme que volta rápido.",
                $"Ganhe <style=cIsHealing>escudo</style> igual a <style=cIsHealing>{p} da sua vida máxima</style> <style=cStack>(+{p} por acúmulo)</style>. " +
                $"O escudo começa a <style=cIsUtility>recarregar {d} s</style> depois do último golpe (normalmente 7 s).",
                "Nenhuma muralha cai se for reerguida mais rápido do que é derrubada.",
                log, "RoR2/Base/PersonalShield/PickupShieldGenerator.prefab");
        }

        public BastionFeature(Cfg cfg, ManualLogSource log) : base("Bastion", cfg, log,
            "Item Bastião (vermelho): escudo de 40% da vida máxima por cópia e recarga do escudo 2 s depois do último golpe (jogo: 7 s). Desligado: o item existe mas não faz nada.",
            enabledByDefault: true)
        {
        }

        protected override void OnEnable()
        {
            if (SetMaxShield == null || SetMaxBarrier == null || SetMaxHull == null)
                throw new MissingMemberException("CharacterBody.maxShield/maxBarrier/maxHull sem setter (versão do jogo mudou?)");
            Track("CharacterBody.onBodyInventoryChangedGlobal", () => CharacterBody.onBodyInventoryChangedGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyStartGlobal", () => CharacterBody.onBodyStartGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyDestroyGlobal", () => CharacterBody.onBodyDestroyGlobal += OnBodyDestroy);
            BodyHooks.Bastion = this; // RecalculateStats (passo 2): hook único em BodyHooks; tick de recarga: SetTicking
        }

        protected override void OnDisable()
        {
            CharacterBody.onBodyInventoryChangedGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyStartGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyDestroyGlobal -= OnBodyDestroy;
            _holders.Clear();
            SetHook(false);
        }

        public override void ResetRunState()
        {
            _holders.Clear();
            SetHook(false);
        }

        // Portadores mudaram: hook de stats (BodyHooks) e tick de recarga acompanham.
        private void SetHook(bool on)
        {
            BodyHooks.Sync();
            if (on == _ticking) return;
            if (on) RoR2Application.onFixedUpdate += OnFixedUpdate;
            else RoR2Application.onFixedUpdate -= OnFixedUpdate;
            _ticking = on;
            _acc = 0f;
        }

        private static int Stacks(CharacterBody body)
        {
            var inv = body ? body.inventory : null;
            var idx = Index;
            return inv && idx != ItemIndex.None ? inv.GetItemCountEffective(idx) : 0;
        }

        private void OnHolderMaybeChanged(CharacterBody body)
        {
            try
            {
                if (!body) return;
                if (Stacks(body) > 0)
                {
                    if (!_holders.ContainsKey(body.gameObject))
                    {
                        _holders[body.gameObject] = body;
                        SetHook(true);
                        body.MarkAllStatsDirty(); // o Start do corpo já pode ter calculado os stats sem o hook
                    }
                }
                else if (_holders.Remove(body.gameObject)) SetHook(_holders.Count > 0);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnBodyDestroy(CharacterBody body)
        {
            if (_holders.Count > 0 && body && _holders.Remove(body.gameObject)) SetHook(_holders.Count > 0);
        }

        // Passo 2 do BodyHooks.RecalculateStats: escudo extra sobre a vida máxima final (depois do Coração Pesado).
        internal void ApplyStats(CharacterBody self)
        {
            try
            {
                var extra = Bastion.ExtraShield(self.maxHealth, Stacks(self));
                if (extra <= 0f) return;
                var newMax = self.maxShield + extra;
                SetMaxShield(self, newMax);
                SetMaxBarrier(self, self.maxHealth + newMax);
                SetMaxHull(self, self.maxHealth + newMax);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Host: recarga do escudo 2 s depois do último golpe, pela recarga do próprio jogo.
        private void OnFixedUpdate()
        {
            try
            {
                _acc += Time.fixedDeltaTime;
                if (_acc < Interval || !NetworkServer.active) return;
                _acc = 0f;
                _tickList.Clear();
                _tickList.AddRange(_holders.Values);
                foreach (var body in _tickList)
                {
                    var hc = body ? body.healthComponent : null;
                    if (hc && hc.alive && Bastion.ShouldForceRegen(hc.timeSinceLastHit, hc.shield, body.maxShield))
                        hc.ForceShieldRegen();
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }
}
