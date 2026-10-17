namespace Ror2Meeeee.Core.Stats
{
    // Item vermelho "Bastião": escudo de 40% da vida máxima por cópia (linear, sem teto) e recarga que começa 2 s depois do
    // último golpe (o jogo espera 7 s).
    public static class Bastion
    {
        public const float ShieldPerStack = 0.4f;
        public const float RegenDelaySeconds = 2f;

        public static float ExtraShield(float maxHealth, int stacks) => stacks <= 0 || maxHealth <= 0f ? 0f : maxHealth * ShieldPerStack * stacks;

        public static bool ShouldForceRegen(float timeSinceLastHit, float shield, float maxShield)
            => maxShield > 0f && shield < maxShield && timeSinceLastHit >= RegenDelaySeconds;
    }
}
