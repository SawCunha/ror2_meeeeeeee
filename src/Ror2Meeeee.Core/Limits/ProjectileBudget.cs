namespace Ror2Meeeee.Core.Limits
{
    // Janela de 1 s com teto de disparos. Disparos acima do teto não saem: o dano deles é guardado e somado ao próximo
    // disparo permitido (menos projéteis, cada um mais forte, mesmo dano total; como o Aggregate do Bundle of Fireworks).
    public struct BudgetWindow
    {
        public float Start;
        public int Count;
        public float PendingDamage;
    }

    public static class ProjectileBudget
    {
        // true = dispara (damageToFire já inclui o dano guardado); false = pulado (dano guardado em PendingDamage).
        public static bool Decide(ref BudgetWindow w, float now, int maxPerSecond, float damage, out float damageToFire)
        {
            if (now - w.Start >= 1f || now < w.Start) { w.Start = now; w.Count = 0; }
            if (maxPerSecond <= 0 || w.Count < maxPerSecond)
            {
                w.Count++;
                damageToFire = damage + w.PendingDamage;
                w.PendingDamage = 0f;
                return true;
            }
            w.PendingDamage += damage;
            damageToFire = 0f;
            return false;
        }
    }
}
