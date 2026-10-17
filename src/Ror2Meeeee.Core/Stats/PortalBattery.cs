namespace Ror2Meeeee.Core.Stats
{
    // Item "Bateria do Portal": +10% de velocidade de carga das zonas (teleportador etc.) por cópia do time, linear, sem teto.
    public static class PortalBattery
    {
        public const float ChargePerStack = 0.1f;

        public static float ChargeRateMultiplier(int teamStacks) => teamStacks <= 0 ? 1f : 1f + ChargePerStack * teamStacks;
    }
}
