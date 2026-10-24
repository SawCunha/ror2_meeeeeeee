using System;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Difficulty;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Escala quantidade, ritmo, vida e dano dos inimigos por um fator único (só no host) e aplica
    // o efeito do item "Chamado da Horda" na quantidade. Além disso aplica um bônus fixo de vida e dano
    // (MonsterHealthBonus/MonsterDamageBonus). Factor = 1.0 e bônus 0 não alteram nada.
    //  - Quantidade: CombatDirector.moneyWaves[i].multiplier (privado; reflexão só ao aplicar, nunca por tick).
    //  - Ritmo: Run.difficultyCoefficient / compensatedDifficultyCoefficient após o recálculo do jogo.
    //  - Vida/dano: itens BoostHp/BoostDamage (+10% por stack) dados uma vez por inimigo, como o próprio jogo faz.
    public sealed class DifficultyFeature : Feature
    {
        private static FieldInfo _waveMultiplierField; // DirectorMoneyWave.multiplier; resolvido na primeira aplicação

        private readonly ConfigEntry<float> _factor;
        private readonly ConfigEntry<float> _healthGain;
        private readonly ConfigEntry<float> _damageGain;
        private readonly ConfigEntry<float> _quantityGain;
        private readonly ConfigEntry<float> _paceGain;
        private readonly ConfigEntry<float> _healthBonus;
        private readonly ConfigEntry<float> _damageBonus;
        private readonly ConfigEntry<float> _hordePerStack;
        private readonly ConfigEntry<float> _hordeMax;

        private DifficultyMultipliers _mult = DifficultyMultipliers.Identity;
        private float _healthTotal = 1f; // fator * bônus fixo
        private float _damageTotal = 1f;
        private float _hordeMult = 1f;
        private int _boostedCount;

        public DifficultyFeature(Cfg cfg, ManualLogSource log) : base("Difficulty", cfg, log,
            "Difficulty: stronger and more numerous enemies (Factor and fixed health and damage bonuses). Also enables the effect of the Call of the Horde item. Host only.", "Dificuldade: inimigos mais fortes e numerosos (Factor e bônus fixos de vida e dano). Também ativa o efeito do item Chamado da Horda. Só o host.",
            enabledByDefault: true)
        {
            _factor = cfg.Bind("Difficulty", "Factor", DifficultyCurve.DefaultFactor,
                "Overall difficulty multiplier. 1.0 = normal game; 2.0 = double, split between health, damage, quantity and pace by the gains below. Invalid falls back to 1.0.", "Multiplicador geral de dificuldade. 1.0 = jogo normal; 2.0 = o dobro, repartido entre vida, dano, quantidade e ritmo pelos ganhos abaixo. Inválido volta para 1.0.");
            _healthGain = cfg.Bind("Difficulty", "HealthGain", 1f, "How much of Factor goes to enemy health. 1 = Factor 2.0 gives +100% health; 0 = health doesn't change.", "Quanto do Factor vai para a vida dos inimigos. 1 = Factor 2.0 dá +100% de vida; 0 = a vida não muda.");
            _damageGain = cfg.Bind("Difficulty", "DamageGain", 0.5f, "How much of Factor goes to enemy damage. 0.5 = Factor 2.0 gives +50% damage; 0 = damage doesn't change.", "Quanto do Factor vai para o dano dos inimigos. 0.5 = Factor 2.0 dá +50% de dano; 0 = o dano não muda.");
            _quantityGain = cfg.Bind("Difficulty", "QuantityGain", 2f, "How much of Factor goes to enemy quantity. 2 = Factor 2.0 gives triple spawns; 0 = quantity doesn't change.", "Quanto do Factor vai para a quantidade de inimigos. 2 = Factor 2.0 dá o triplo de spawns; 0 = a quantidade não muda.");
            _paceGain = cfg.Bind("Difficulty", "PaceGain", 0.5f, "How much Factor speeds up the difficulty ramp over time. 0.5 = Factor 2.0 scales 50% faster; 0 = normal pace.", "Quanto do Factor acelera a subida da dificuldade com o tempo. 0.5 = Factor 2.0 escala 50% mais rápido; 0 = ritmo normal.");
            _healthBonus = cfg.Bind("Difficulty", "MonsterHealthBonus", 75f,
                "Extra health for all enemies, in %, added to Factor (75 = +75%). 0 = no bonus. Applies to enemies spawned afterwards, in 10% steps.", "Vida extra de todos os inimigos, em %, somada ao Factor (75 = +75%). 0 = sem bônus. Vale para inimigos que nascerem depois, em passos de 10%.");
            _damageBonus = cfg.Bind("Difficulty", "MonsterDamageBonus", 50f,
                "Extra damage for all enemies, in %, added to Factor (50 = +50%). 0 = no bonus. Applies to enemies spawned afterwards, in 10% steps.", "Dano extra de todos os inimigos, em %, somado ao Factor (50 = +50%). 0 = sem bônus. Vale para inimigos que nascerem depois, em passos de 10%.");
            _hordePerStack = cfg.Bind("Horde", "QuantityPerStack", 0.5f, "Call of the Horde item: each stack the team has increases enemy spawns by this fraction (0.5 = +50%). Requires Difficulty on.", "Item Chamado da Horda: cada cópia que o time tiver aumenta os spawns de inimigos nesta fração (0.5 = +50%). Precisa da Dificuldade ligada.");
            _hordeMax = cfg.Bind("Horde", "MaxMultiplier", 20f, "Call of the Horde item: cap on the spawn increase (20 = at most 20x).", "Item Chamado da Horda: teto do aumento de spawns (20 = no máximo 20x).");

            foreach (var e in new[] { _factor, _healthGain, _damageGain, _quantityGain, _paceGain, _healthBonus, _damageBonus, _hordePerStack, _hordeMax })
                e.SettingChanged += (_, __) => Refresh();
            Refresh();
        }

        private void Refresh()
        {
            if (DifficultyCurve.SanitizeFactor(_factor.Value) != _factor.Value)
            {
                Log.LogWarning($"Difficulty.Factor={_factor.Value} inválido; usando {DifficultyCurve.DefaultFactor}.");
                _factor.Value = DifficultyCurve.DefaultFactor; // dispara SettingChanged; o Refresh aninhado vê o valor válido
                return;
            }
            _mult = DifficultyCurve.Evaluate(_factor.Value,
                new DifficultyGains(_healthGain.Value, _damageGain.Value, _quantityGain.Value, _paceGain.Value));
            _healthTotal = DifficultyCurve.WithBonus(_mult.Health, _healthBonus.Value);
            _damageTotal = DifficultyCurve.WithBonus(_mult.Damage, _damageBonus.Value);
            Log.LogInfo($"[Difficulty] Factor={_factor.Value} => vida x{_healthTotal:F2} (bônus {_healthBonus.Value:F0}%), dano x{_damageTotal:F2} (bônus {_damageBonus.Value:F0}%), quantidade x{_mult.Quantity:F2}, ritmo x{_mult.Pace:F2}");
            if (Enabled) ApplyQuantityToAll();
        }

        public override void ResetRunState()
        {
            _boostedCount = 0;
            _hordeMult = 1f;
        }

        protected override void OnEnable()
        {
            Track("CombatDirector.Awake", () => On.RoR2.CombatDirector.Awake += OnDirectorAwake);
            Track("Run.RecalculateDifficultyCoefficentInternal", () => On.RoR2.Run.RecalculateDifficultyCoefficentInternal += OnRecalculate);
            Track("CharacterMaster.onStartGlobal", () => CharacterMaster.onStartGlobal += OnMasterStart);
            Track("Inventory.onServerItemGiven", () => Inventory.onServerItemGiven += OnItemGiven);
        }

        protected override void OnDisable()
        {
            On.RoR2.CombatDirector.Awake -= OnDirectorAwake;
            On.RoR2.Run.RecalculateDifficultyCoefficentInternal -= OnRecalculate;
            CharacterMaster.onStartGlobal -= OnMasterStart;
            Inventory.onServerItemGiven -= OnItemGiven;
        }

        // ---- quantidade ----

        private void OnDirectorAwake(On.RoR2.CombatDirector.orig_Awake orig, CombatDirector self)
        {
            orig(self);
            try
            {
                if (!NetworkServer.active) return;
                RecountHorde();
                if (ApplyQuantity(self) && (_mult.Quantity != 1f || _hordeMult != 1f))
                    Log.LogInfo($"[Difficulty] novo diretor: orçamento de spawn x{self.creditMultiplier * _mult.Quantity * _hordeMult:F2}");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnItemGiven(Inventory inventory, ItemIndex item, int count)
        {
            try
            {
                if (item != HordeItem.Index || item == ItemIndex.None || !NetworkServer.active) return;
                var before = _hordeMult;
                RecountHorde();
                if (_hordeMult != before) ApplyQuantityToAll();
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void RecountHorde()
        {
            var idx = HordeItem.Index;
            var stacks = 0;
            if (idx != ItemIndex.None)
            {
                var args = new Util.GetTotalItemCountFromTeamArgs(idx);
                args.requiresAlive = false;
                args.requiredTeam = TeamIndex.Player;
                args.requiresConnected = true;
                args.onlyPlayerControlledCharacters = true; // torretas do Engi copiam o inventário: contavam como cópias
                stacks = Util.GetTotalItemCountFromTeam(args);
            }
            var m = HordeCurve.QuantityMultiplier(stacks, _hordePerStack.Value, _hordeMax.Value);
            if (m != _hordeMult)
                Log.LogInfo($"[Horde] stacks={stacks} => orçamento de spawn x{m:F2}");
            _hordeMult = m;
        }

        private void ApplyQuantityToAll()
        {
            if (!NetworkServer.active) return;
            var n = 0;
            foreach (var d in CombatDirector.instancesList)
                if (d && ApplyQuantity(d)) n++;
            Log.LogInfo($"[Difficulty] quantidade aplicada a {n} diretor(es): fator x{_mult.Quantity:F2} * horda x{_hordeMult:F2}");
        }

        // Absoluto e idempotente: multiplier = creditMultiplier do diretor * fator * horda.
        private bool ApplyQuantity(CombatDirector d)
        {
            if (d.teamIndex != TeamIndex.Monster || !Directors.Available) return false;
            var waves = Directors.GetMoneyWaves(d);
            if (waves == null || waves.Length == 0) return false;
            var field = _waveMultiplierField ??= waves.GetType().GetElementType().GetField("multiplier", BindingFlags.Instance | BindingFlags.Public);
            if (field == null) return false;
            var value = d.creditMultiplier * _mult.Quantity * _hordeMult;
            foreach (var w in waves) // DirectorMoneyWave é classe: SetValue altera a instância no lugar
                if (w != null) field.SetValue(w, value);
            return true;
        }

        // ---- ritmo ----

        private void OnRecalculate(On.RoR2.Run.orig_RecalculateDifficultyCoefficentInternal orig, Run self)
        {
            orig(self);
            try
            {
                var pace = _mult.Pace;
                if (!NetworkServer.active || pace == 1f) return;
                self.difficultyCoefficient *= pace;
                self.compensatedDifficultyCoefficient *= pace;
                self.oneOverCompensatedDifficultyCoefficientSquared =
                    1f / (self.compensatedDifficultyCoefficient * self.compensatedDifficultyCoefficient);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // ---- vida e dano ----

        private void OnMasterStart(CharacterMaster master)
        {
            try
            {
                if (!NetworkServer.active || !master) return;
                var team = master.teamIndex;
                if (team != TeamIndex.Monster && team != TeamIndex.Lunar && team != TeamIndex.Void) return;
                if (master.playerCharacterMasterController) return;
                // Almas do Artefato da Alma (uma por monstro morto): morrem num golpe, e o Boost só custava
                // GiveItem + RecalculateStats a cada uma (chegaram a milhares por run).
                var soul = GlobalEventManager.CommonAssets.wispSoulMasterPrefabMasterComponent;
                if (soul && master.masterIndex == soul.masterIndex) return;
                var inv = master.inventory;
                if (!inv) return; // onStartGlobal vem de Start: uma vez por master, sem precisar de guarda

                // BoostHp/BoostDamage: +10% por stack (CharacterBody.RecalculateStats).
                var hp = Mathf.RoundToInt((_healthTotal - 1f) * 10f);
                var dmg = Mathf.RoundToInt((_damageTotal - 1f) * 10f);
                if (hp > 0) inv.GiveItemPermanent(RoR2Content.Items.BoostHp, hp);
                if (dmg > 0) inv.GiveItemPermanent(RoR2Content.Items.BoostDamage, dmg);
                _boostedCount++;
                // Poucas linhas: as primeiras de cada run e depois 1 a cada 50.
                if ((hp > 0 || dmg > 0) && (_boostedCount <= 5 || _boostedCount % 50 == 0))
                    Log.LogInfo($"[Difficulty] inimigo #{_boostedCount} ({master.name}): +{hp} vida, +{dmg} dano (stacks de Boost, +10% cada)");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }
}
