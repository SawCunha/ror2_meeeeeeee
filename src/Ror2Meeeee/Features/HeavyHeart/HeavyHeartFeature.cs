using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Stats;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Item lunar "Coração Pesado": cada cópia multiplica a vida máxima por 1,5 e o dano recebido por 1,3 (Core.Stats.HeavyHeart).
    // Vida: passo 1 do BodyHooks.RecalculateStats (multiplica maxHealth e barreira/casco; o BodyHooks reajusta a vida atual).
    // Dano: passo 2 do BodyHooks.TakeDamageProcess, só dano vindo de outro personagem e não bloqueado (cobranças em vida,
    // queda e dano sem atacante não aumentam). Os hooks só existem enquanto alguém tem o item.
    public sealed class HeavyHeartFeature : Feature, IHolderSet
    {
        public const string ItemName = "Ror2MeeeeeHeavyHeart";
        public const string Token = "ROR2MEEEEE_ITEM_HEAVYHEART";

        private static readonly Action<CharacterBody, float> SetMaxHealth = Setter("maxHealth");
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

        public bool HasHolders => _holders.Count > 0;
        public bool Holds(GameObject body) => _holders.ContainsKey(body);

        public static void Register(ManualLogSource log)
        {
            var pt = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
            string P(float f) => (f * 100f).ToString("0.##", pt) + "%";
            _def = ModItem.Register(ItemName, Token, ItemTier.Lunar, new[] { ItemTag.Healing, ItemTag.AIBlacklist },
                "coracao_pesado.png", new Color32(90, 140, 220, 255),
                "Coração Pesado",
                $"Mais vida. <style=cDeath>Cada golpe pesa mais.</style>",
                $"Aumenta a <style=cIsHealing>vida máxima em {P(HeavyHeart.HealthPerStack)}</style> <style=cStack>(multiplica de novo a cada acúmulo)</style>. " +
                $"<style=cDeath>Aumenta o dano recebido de inimigos em {P(HeavyHeart.DamageTakenPerStack)}</style> <style=cStack>(multiplica de novo a cada acúmulo)</style>.",
                "Quanto mais o coração cresce, mais fundo cada ferida chega.",
                log, "RoR2/Base/Pearl/PickupPearl.prefab");
        }

        public HeavyHeartFeature(Cfg cfg, ManualLogSource log) : base("HeavyHeart", cfg, log,
            "Item lunar Coração Pesado: cada cópia multiplica a vida máxima por 1,5 e o dano recebido de inimigos por 1,3 (composto). Desligado: o item existe mas não faz nada.",
            enabledByDefault: true)
        {
        }

        protected override void OnEnable()
        {
            if (SetMaxHealth == null || SetMaxBarrier == null || SetMaxHull == null)
                throw new MissingMemberException("CharacterBody.maxHealth/maxBarrier/maxHull sem setter (versão do jogo mudou?)");
            Track("CharacterBody.onBodyInventoryChangedGlobal", () => CharacterBody.onBodyInventoryChangedGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyStartGlobal", () => CharacterBody.onBodyStartGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyDestroyGlobal", () => CharacterBody.onBodyDestroyGlobal += OnBodyDestroy);
            BodyHooks.Heart = this; // RecalculateStats (passo 1) e TakeDamageProcess (passo 2): hooks únicos em BodyHooks
        }

        protected override void OnDisable()
        {
            CharacterBody.onBodyInventoryChangedGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyStartGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyDestroyGlobal -= OnBodyDestroy;
            _holders.Clear();
            BodyHooks.Sync();
        }

        public override void ResetRunState()
        {
            _holders.Clear();
            BodyHooks.Sync();
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
                        BodyHooks.Sync();
                        body.MarkAllStatsDirty(); // o Start do corpo já pode ter calculado os stats sem o hook
                    }
                }
                else if (_holders.Remove(body.gameObject)) BodyHooks.Sync();
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnBodyDestroy(CharacterBody body)
        {
            if (_holders.Count > 0 && body && _holders.Remove(body.gameObject)) BodyHooks.Sync();
        }

        // Passo 1 do BodyHooks.RecalculateStats: vida máxima x1,5^n (barreira e casco derivam dela).
        internal void ApplyStats(CharacterBody self)
        {
            try
            {
                var mult = HeavyHeart.HealthMultiplier(Stacks(self));
                if (mult == 1f) return;
                var newMax = self.maxHealth * mult;
                SetMaxHealth(self, newMax);
                SetMaxBarrier(self, newMax + self.maxShield);
                SetMaxHull(self, newMax + self.maxShield);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Passo 2 do BodyHooks.TakeDamageProcess (host): dano de OUTRO personagem, não bloqueado, x1,3^n.
        internal void AmplifyDamage(HealthComponent self, DamageInfo damageInfo)
        {
            try
            {
                if (_holders.Count == 0 || !self || damageInfo == null || damageInfo.rejected) return;
                if (!_holders.TryGetValue(self.gameObject, out var body) || !body) return;
                var attacker = damageInfo.attacker;
                if (!attacker || attacker == self.gameObject || !attacker.GetComponent<CharacterBody>()) return;
                var stacks = Stacks(body);
                if (stacks > 0) damageInfo.damage *= HeavyHeart.DamageTakenMultiplier(stacks);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }
}
