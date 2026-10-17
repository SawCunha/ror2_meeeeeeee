using System;

namespace Ror2Meeeee.Core.Limits
{
    // Aggregate só muda o comportamento em quem o implementa (Fireworks: menos mísseis, mesmo dano total).
    // Para a política de stacks é um Clamp.
    public enum StackMode { Off, Clamp, Aggregate }

    // Política pura de stacks: sem jogo, sem Unity. Instâncias imutáveis, reutilizáveis no caminho quente.
    public interface IStackPolicy
    {
        // Menor valor que Apply pode devolver para stack grande (int.MaxValue se sem limite).
        int Limit { get; }
        int Apply(int stack);
    }

    public static class StackPolicy
    {
        public static readonly IStackPolicy Off = new OffPolicy();

        public static IStackPolicy Clamp(int limit) => new ClampPolicy(Math.Max(1, limit));

        public static IStackPolicy Create(StackMode mode, int limit) => mode == StackMode.Off ? Off : Clamp(limit);

        private sealed class OffPolicy : IStackPolicy
        {
            public int Limit => int.MaxValue;
            public int Apply(int stack) => stack;
        }

        private sealed class ClampPolicy : IStackPolicy
        {
            private readonly int _limit;
            public ClampPolicy(int limit) { _limit = limit; }
            public int Limit => _limit;
            public int Apply(int stack) => stack < 0 ? 0 : (stack > _limit ? _limit : stack);
        }
    }
}
