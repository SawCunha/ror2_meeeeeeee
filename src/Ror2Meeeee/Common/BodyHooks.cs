using System;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Stats;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Item com lista de portadores (mantida pelos eventos de inventário/corpo da própria feature).
    internal interface IHolderSet
    {
        bool HasHolders { get; }
        bool Holds(GameObject body);
    }

    // Um hook por ponto do jogo, com a ORDEM DE APLICAÇÃO escrita aqui (antes, cada item tinha o seu hook e a ordem
    // dependia de quem registrou primeiro). Cada hook só existe enquanto algum item que o usa tem portador.
    //
    // CharacterBody.RecalculateStats (toda máquina; depois do cálculo do jogo):
    //   0. Tormenta: vida máxima x(1 + stacks/100) do item oculto dos monstros (runs da Tormenta)
    //   1. Coração Pesado: vida máxima x1,5^n
    //   2. Bastião: escudo = 40% da vida máxima JÁ multiplicada
    //   3. Cervo: excedente de velocidade vira dano
    //   4. Teto: vida máxima + escudo <= 1 bilhão (a barra de vida do jogo usa int e estoura acima de ~2,1 bi)
    //   5. Host: vida e escudo atuais reajustados uma vez (o jogo os corta quando o máximo que ELE calculou cai)
    //
    // HealthComponent.TakeDamageProcess (host):
    //   1. Égide: sorteio do bloqueio (5%) e se o golpe é elegível (imunidade/invencibilidade não contam)
    //   2. Coração Pesado: dano de inimigo x1,3^n, só se não foi bloqueado
    //   3. Tormenta: redução de dano do monstro vítima (tabela por estágio)
    //   4. jogo processa o dano (na Tormenta, as Lentes do Vidente Perdido não executam: IL da TempestFeature)
    //   5. Égide: se bloqueou, rouba vida
    //   6. Tormenta: Lentes do Vidente Perdido tiram 90% da vida restante, sem matar
    //
    // Taxa do Taxade: SEMPRE POR ÚLTIMO e sem modificadores. É calculada na compra sobre a vida máxima final (todos os
    // passos de stats acima já aplicados) e o dano passa direto (RawDamage): nem bloqueio da Égide nem aumento do Coração.
    // Na compra, o Taxade também é o último passo do PurchaseHub. O custo de vida do revive é cobrança e também passa direto.
    // Ouro (MoneyHub): o Capitalismo aumenta o ouro antes do jogo; o Dízimo calcula o crédito sobre o valor ORIGINAL,
    // então o efeito do Taxade não entra em nenhum outro cálculo.
    internal static class BodyHooks
    {
        // Dano aplicado enquanto true passa sem nenhum passo (taxa do Taxade). Só na thread principal, dentro de TakeDamage.
        internal static bool RawDamage;

        internal static HeavyHeartFeature Heart;
        internal static BastionFeature Bastion;
        internal static CervoFeature Cervo;
        internal static AegisFeature Aegis;
        internal static TempestFeature Tempest;

        private static bool _stats, _damage;

        private static readonly Action<CharacterBody, float> SetMaxHealth = Setter("maxHealth");
        private static readonly Action<CharacterBody, float> SetMaxShield = Setter("maxShield");
        private static readonly Action<CharacterBody, float> SetMaxBarrier = Setter("maxBarrier");
        private static readonly Action<CharacterBody, float> SetMaxHull = Setter("maxHull");

        private static Action<CharacterBody, float> Setter(string property)
        {
            var set = typeof(CharacterBody).GetProperty(property)?.GetSetMethod(true);
            return set == null ? null : (Action<CharacterBody, float>)Delegate.CreateDelegate(typeof(Action<CharacterBody, float>), set);
        }

        private static bool Active(IHolderSet f) => f != null && f.HasHolders;

        // Chamado pelas features quando a lista de portadores muda (ou a feature liga/desliga).
        public static void Sync()
        {
            var tempestHealth = Tempest != null && Tempest.HealthHook;
            var stats = Active(Heart) || Active(Bastion) || Active(Cervo) || tempestHealth;
            if (stats != _stats)
            {
                if (stats) On.RoR2.CharacterBody.RecalculateStats += OnRecalculateStats;
                else On.RoR2.CharacterBody.RecalculateStats -= OnRecalculateStats;
                _stats = stats;
                Diag.Log?.LogInfo($"[BodyHooks] hook de stats {(stats ? "ligado" : "desligado")} (Coração/Bastião/Cervo/Tormenta)");
            }
            var damage = Active(Heart) || Active(Aegis) || (Tempest != null && Tempest.DamageHook);
            if (damage != _damage)
            {
                if (damage) On.RoR2.HealthComponent.TakeDamageProcess += OnTakeDamage;
                else On.RoR2.HealthComponent.TakeDamageProcess -= OnTakeDamage;
                _damage = damage;
                Diag.Log?.LogInfo($"[BodyHooks] hook de dano {(damage ? "ligado" : "desligado")} (Égides/Coração/Tormenta)");
            }
        }

        private static void OnRecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            var go = self ? self.gameObject : null;
            var heart = go && Heart != null && Heart.Holds(go);
            var bastion = go && Bastion != null && Bastion.Holds(go);
            var cervo = go && Cervo != null && Cervo.Holds(go);
            var vigor = go && Tempest != null && Tempest.HealthHook ? TempestStacks(self) : 0;
            if (!heart && !bastion && !cervo && vigor == 0) { orig(self); return; } // quem não tem item: 3 consultas de dicionário

            var hc = self.healthComponent;
            var previousMaxHealth = self.maxHealth;
            var health = hc ? hc.health : 0f;
            var previousMaxShield = self.maxShield;
            var shield = hc ? hc.shield : 0f;

            orig(self);

            if (vigor > 0) ApplyTempestHealth(self, vigor);
            if (heart) Heart.ApplyStats(self);
            if (bastion) Bastion.ApplyStats(self);
            if (cervo) Cervo.ApplyStats(self);
            ClampPools(self);

            if (!NetworkServer.active || !hc || !hc.alive) return;
            try
            {
                if (heart || vigor > 0) hc.Networkhealth = StatPool.Adjust(health, previousMaxHealth, self.maxHealth);
                if (bastion) hc.Networkshield = StatPool.Adjust(shield, previousMaxShield, self.maxShield);
            }
            catch (Exception e) { Diag.Log?.LogWarning($"[BodyHooks] reajuste de vida/escudo falhou: {e.Message}"); }
        }

        private static int TempestStacks(CharacterBody self)
        {
            var idx = TempestHealthItem.Index;
            var inv = self.inventory;
            return idx != ItemIndex.None && inv ? inv.GetItemCountPermanent(idx) : 0;
        }

        // Passo 0: vida máxima x(1 + stacks/100); barreira e casco derivam dela (como no Coração Pesado).
        private static void ApplyTempestHealth(CharacterBody self, int stacks)
        {
            try
            {
                if (SetMaxHealth == null || SetMaxBarrier == null || SetMaxHull == null) return;
                var before = self.maxHealth;
                var newMax = before * (1f + stacks / 100f);
                SetMaxHealth(self, newMax);
                SetMaxBarrier(self, newMax + self.maxShield);
                SetMaxHull(self, newMax + self.maxShield);
                if (Tempest != null && Tempest.DebugOn) Tempest.LogHealth(self, before, stacks);
            }
            catch (Exception e) { Diag.Log?.LogWarning($"[BodyHooks] vida da Tormenta falhou: {e.Message}"); }
        }

        // Passo 4: teto de vida + escudo (proporcional), para a barra de vida do jogo não estourar o int.
        private static void ClampPools(CharacterBody self)
        {
            try
            {
                var scale = StatPool.ClampScale(self.maxHealth, self.maxShield);
                if (scale >= 1f || SetMaxHealth == null || SetMaxShield == null || SetMaxBarrier == null || SetMaxHull == null) return;
                var health = self.maxHealth * scale;
                var shield = self.maxShield * scale;
                SetMaxHealth(self, health);
                SetMaxShield(self, shield);
                SetMaxBarrier(self, health + shield);
                SetMaxHull(self, health + shield);
            }
            catch (Exception e) { Diag.Log?.LogWarning($"[BodyHooks] teto de vida/escudo falhou: {e.Message}"); }
        }

        private static void OnTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (RawDamage) { orig(self, damageInfo); return; }

            CharacterBody aegisBody = null;
            var aegis = Aegis != null && Aegis.BeforeDamage(self, damageInfo, out aegisBody);
            Heart?.AmplifyDamage(self, damageInfo);
            Tempest?.ReduceDamage(self, damageInfo);

            orig(self, damageInfo);

            if (aegis && damageInfo.rejected) Aegis.AfterBlocked(aegisBody, damageInfo);
            Tempest?.AfterDamage(self, damageInfo);
        }
    }
}
