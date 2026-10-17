namespace Ror2Meeeee.Core.Stats
{
    // Fungo Egoísta e a versão do Vazio: curam sempre (andando ou parado), só o dono e os minions dele.
    // Base FIXA (vida/s, cresce com o nível como a regeneração do jogo); a parte em % da vida máxima vem só das cópias
    // extras. Em % pura a cura escalava com a vida inflada pelo multiplicador (~44/s no estágio 1 com 80 FlatHealth).
    public static class SelfHeal
    {
        public const float SelfishFlat = 5f;          // vida/s no nível 1
        public const float SelfishPerStack = 0.0001f;  // +0,01% da vida máxima/s por cópia extra
        public const float VoidFlat = 15f;
        public const float VoidPerStack = 0.005f;      // +0,5% por cópia extra
        public const float LevelScaling = 0.2f;        // +20% da base por nível, como a regeneração do jogo

        public static float PerSecond(int stacks, float flat, float perExtraStack, float fullHealth, float level)
        {
            if (stacks <= 0) return 0f;
            var lvl = level < 1f ? 1f : level;
            return flat * (1f + LevelScaling * (lvl - 1f)) + perExtraStack * (stacks - 1) * fullHealth;
        }

        // Vida curada num tick de `interval` s; os dois itens somam (normalmente o Vazio corrompe todas as cópias do outro).
        public static float AmountPerTick(int selfish, int voidStacks, float fullHealth, float level, float interval)
        {
            if (interval <= 0f) return 0f;
            return (PerSecond(selfish, SelfishFlat, SelfishPerStack, fullHealth, level)
                    + PerSecond(voidStacks, VoidFlat, VoidPerStack, fullHealth, level)) * interval;
        }
    }
}
