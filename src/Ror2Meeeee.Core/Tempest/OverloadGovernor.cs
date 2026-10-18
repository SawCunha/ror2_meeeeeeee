using System;

namespace Ror2Meeeee.Core.Tempest
{
    public enum OverloadState
    {
        Normal,
        Reduced,
        Recovering,
    }

    // Degradação por desempenho com histerese (§9.2): entra com o frame médio acima de EnterMs por EnterSeconds,
    // sai com ele abaixo de ExitMs por ExitSeconds e então recupera em rampa de RecoverySeconds.
    // Level = fator do crédito do mod (1 normal, 0 reduzido).
    public sealed class OverloadGovernor
    {
        private float _above, _below;

        public float EnterMs { get; private set; } = 33f;
        public float EnterSeconds { get; private set; } = 10f;
        public float ExitMs { get; private set; } = 25f;
        public float ExitSeconds { get; private set; } = 20f;
        public float RecoverySeconds { get; private set; } = 30f;

        public OverloadState State { get; private set; } = OverloadState.Normal;
        public float Level { get; private set; } = 1f;

        // Saída sempre abaixo da entrada; senão a saída vira 75% da entrada.
        public void Configure(float enterMs, float enterSeconds, float exitMs, float exitSeconds, float recoverySeconds)
        {
            EnterMs = enterMs;
            EnterSeconds = Math.Max(0f, enterSeconds);
            ExitMs = exitMs < enterMs ? exitMs : enterMs * 0.75f;
            ExitSeconds = Math.Max(0f, exitSeconds);
            RecoverySeconds = Math.Max(0.001f, recoverySeconds);
        }

        public void Reset()
        {
            State = OverloadState.Normal;
            Level = 1f;
            _above = _below = 0f;
        }

        // Devolve true quando o estado muda (para logar entrada/saída).
        public bool Update(float frameMs, float dt)
        {
            var before = State;
            _above = frameMs > EnterMs ? _above + dt : 0f;
            _below = frameMs < ExitMs ? _below + dt : 0f;
            switch (State)
            {
                case OverloadState.Normal:
                    if (_above >= EnterSeconds) Enter();
                    break;
                case OverloadState.Reduced:
                    if (_below >= ExitSeconds)
                    {
                        State = OverloadState.Recovering;
                        Level = 0f;
                    }
                    break;
                case OverloadState.Recovering:
                    if (_above >= EnterSeconds) Enter();
                    else
                    {
                        Level = Math.Min(1f, Level + dt / RecoverySeconds);
                        if (Level >= 1f) State = OverloadState.Normal;
                    }
                    break;
            }
            return State != before;
        }

        private void Enter()
        {
            State = OverloadState.Reduced;
            Level = 0f;
            _above = _below = 0f;
        }
    }
}
