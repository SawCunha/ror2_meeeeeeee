namespace Ror2Meeeee.Core.Stats
{
    // Item "zzz cervo <3": toda a velocidade de movimento acima da neutra vira dano.
    // 1 cópia converte 1:1 (+50% de velocidade = +50% de dano); cada cópia extra soma +50% na taxa.
    // Lentidões (velocidade abaixo da neutra) não são tocadas.
    public static class SpeedToDamage
    {
        public const float FirstStackRatio = 1f;
        public const float ExtraStackRatio = 0.5f;

        public static float Ratio(int stacks) => stacks <= 0 ? 0f : FirstStackRatio + ExtraStackRatio * (stacks - 1);

        // neutralSpeed = velocidade sem bônus (base + nível, com sprint se correndo).
        // Devolve false (sem mudança) se não há bônus a converter.
        public static bool Convert(float moveSpeed, float neutralSpeed, float damage, int stacks, out float newSpeed, out float newDamage)
        {
            newSpeed = moveSpeed;
            newDamage = damage;
            if (stacks <= 0 || !(neutralSpeed > 0f) || !(moveSpeed > neutralSpeed) || float.IsInfinity(moveSpeed) || float.IsNaN(damage)) return false;
            var bonus = moveSpeed / neutralSpeed - 1f;
            newSpeed = neutralSpeed;
            newDamage = damage * (1f + bonus * Ratio(stacks));
            return true;
        }
    }
}
