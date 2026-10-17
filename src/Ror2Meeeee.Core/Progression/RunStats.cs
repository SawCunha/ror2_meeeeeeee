namespace Ror2Meeeee.Core.Progression
{
    // Contadores de uma run, copiados na thread principal no game over e entregues ao worker.
    public readonly struct RunStats
    {
        public RunStats(int players, double seconds, int chargedStages, double chargeSeconds,
            int deaths, int kills, double healthBarsLost, bool won)
        {
            Players = players;
            Seconds = seconds;
            ChargedStages = chargedStages;
            ChargeSeconds = chargeSeconds;
            Deaths = deaths;
            Kills = kills;
            HealthBarsLost = healthBarsLost;
            Won = won;
        }

        public int Players { get; }
        public double Seconds { get; }          // duração da run (cronômetro do jogo)
        public int ChargedStages { get; }       // estágios com teleportador carregado
        public double ChargeSeconds { get; }    // soma do tempo início do estágio -> teleportador carregado
        public int Deaths { get; }              // mortes de jogadores
        public int Kills { get; }               // inimigos mortos
        public double HealthBarsLost { get; }   // dano sofrido por jogadores, em vidas máximas (1 = uma barra inteira)
        public bool Won { get; }
    }
}
