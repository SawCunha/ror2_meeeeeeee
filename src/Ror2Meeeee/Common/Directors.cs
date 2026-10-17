using System;
using System.Reflection;
using RoR2;

namespace Ror2Meeeee
{
    // Acesso aos diretores de combate, compartilhado pelas features (Difficulty, Gold, Grimoire).
    // CombatDirector.moneyWaves é privado: reflexão resolvida uma vez; nunca chamar por tick.
    internal static class Directors
    {
        private static readonly FieldInfo MoneyWavesField =
            typeof(CombatDirector).GetField("moneyWaves", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool Available => MoneyWavesField != null;

        public static Array GetMoneyWaves(CombatDirector d) => MoneyWavesField?.GetValue(d) as Array;

        // Diretor principal de monstros: o que acumula ondas de crédito (os de boss/evento não têm).
        public static bool IsMainMonster(CombatDirector d) =>
            d.teamIndex == TeamIndex.Monster && GetMoneyWaves(d) is Array a && a.Length > 0;
    }
}
