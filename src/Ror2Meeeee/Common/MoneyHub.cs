using System;
using System.Diagnostics;
using RoR2;

namespace Ror2Meeeee
{
    // Um único hook em CharacterMaster.GiveMoney (chamado a cada ouro recebido) para Tax e Gold.
    // Before pode alterar o valor antes do jogo; After recebe o valor original, depois do jogo.
    // Registrar/limpar é idempotente; o hook só existe enquanto houver um assinante.
    internal static class MoneyHub
    {
        private static Func<CharacterMaster, uint, uint> _before;
        private static Action<CharacterMaster, uint> _after;
        private static bool _hooked;
        // Custo do mod (before + after, sem o orig do jogo), amostrado 1 a cada 16 chamadas.
        public static long Calls;
        private static long _sampleTicks, _samples;
        public static double AverageMicros => _samples == 0 ? 0 : _sampleTicks * 1e6 / Stopwatch.Frequency / _samples;

        public static void SetBefore(Func<CharacterMaster, uint, uint> f) { _before = f; Sync(); }
        public static void SetAfter(Action<CharacterMaster, uint> f) { _after = f; Sync(); }

        private static void Sync()
        {
            var want = _before != null || _after != null;
            if (want == _hooked) return;
            if (want) On.RoR2.CharacterMaster.GiveMoney_uint_bool += OnGiveMoney;
            else On.RoR2.CharacterMaster.GiveMoney_uint_bool -= OnGiveMoney;
            _hooked = want;
        }

        private static void OnGiveMoney(On.RoR2.CharacterMaster.orig_GiveMoney_uint_bool orig, CharacterMaster self, uint amount, bool bypassStatManager)
        {
            var original = amount;
            var timed = (++Calls & 15) == 0;
            var t0 = timed ? Stopwatch.GetTimestamp() : 0L;
            var before = _before;
            if (before != null) amount = before(self, amount);
            var t1 = timed ? Stopwatch.GetTimestamp() : 0L;
            orig(self, amount, bypassStatManager);
            var t2 = timed ? Stopwatch.GetTimestamp() : 0L;
            _after?.Invoke(self, original);
            if (timed)
            {
                _sampleTicks += (t1 - t0) + (Stopwatch.GetTimestamp() - t2);
                _samples++;
            }
        }
    }
}
