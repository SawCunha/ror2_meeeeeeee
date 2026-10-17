using System;

namespace Ror2Meeeee.Core.Helluva
{
    // Regras dos itens/artefato "Helluva Boss": Grimório do Stolas, Contrato da I.M.P. e Artefato da Ressurreição.
    public static class HelluvaRules
    {
        // Grimório: crédito de spawn que o "contrato do Stolas" dá ao diretor, escalado pela dificuldade atual.
        public static float GrimoireCredit(float baseCredit, float difficultyCoefficient)
        {
            if (float.IsNaN(baseCredit) || baseCredit <= 0f || float.IsNaN(difficultyCoefficient)) return 0f;
            var c = (double)baseCredit * Math.Max(1f, difficultyCoefficient);
            return (float)Math.Min(c, 1e7);
        }

        // Mesmo crédito, com duas travas: teto por uso (maxPerUse > 0) e, com monstros vivos >= maxMonsters
        // (maxMonsters > 0), nenhum crédito (o teleporte segue valendo). Evita onda sobre onda -> lag.
        public static float GrimoireCreditGuarded(float baseCredit, float difficultyCoefficient, float maxPerUse, int monstersAlive, int maxMonsters)
        {
            if (maxMonsters > 0 && monstersAlive >= maxMonsters) return 0f;
            var c = GrimoireCredit(baseCredit, difficultyCoefficient);
            return maxPerUse > 0f ? Math.Min(c, maxPerUse) : c;
        }

        // Contrato: ouro EXTRA pago pela morte do alvo = ouro do alvo * cópias do time (1 cópia = ouro em dobro).
        // Mesmo pagamento, com teto (maxGold > 0). O ouro do alvo cresce com a dificuldade e já passou de 80 mil.
        public static uint ContractGoldCapped(uint goldReward, int teamStacks, uint maxGold)
        {
            var gold = ContractGold(goldReward, teamStacks);
            return maxGold > 0 && gold > maxGold ? maxGold : gold;
        }

        public static uint ContractGold(uint goldReward, int teamStacks)
        {
            if (goldReward == 0 || teamStacks <= 0) return 0;
            var total = (ulong)goldReward * (ulong)teamStacks;
            return total > uint.MaxValue ? uint.MaxValue : (uint)total;
        }

        // Ressurreição: progresso de "tocar a lápide". Sem aliado perto, zera (precisa ser contínuo).
        // Devolve true quando completa.
        public static bool ReviveStep(ref float progress, bool allyNear, float dt, float required)
        {
            if (!allyNear || float.IsNaN(dt) || dt < 0f) { progress = 0f; return false; }
            progress += dt;
            if (progress < required) return false;
            progress = 0f;
            return true;
        }

        // Custo de quem reviveu: perde `fraction` da vida ATUAL, sem morrer (fica com pelo menos 1).
        public static float ReviverCost(float currentHealth, float fraction)
        {
            if (float.IsNaN(currentHealth) || currentHealth <= 1f || float.IsNaN(fraction) || fraction <= 0f) return 0f;
            var cost = currentHealth * Math.Min(1f, fraction);
            return Math.Min(cost, currentHealth - 1f);
        }
    }
}
