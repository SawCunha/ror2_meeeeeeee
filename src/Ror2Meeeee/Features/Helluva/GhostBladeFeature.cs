using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Lâminas Fantasmas (item Curse do DLC4): o jogo renova/gera as lâminas sem parar, ~700 vezes por segundo em
    // runs longas, o que enche o jogo de projéteis (20-47 mil por 30 s) e a memória nativa até congelar.
    // Teto global de chamadas por segundo em SpawnGhostBlade + RefreshOldestGhostBlade; acima disso a chamada é pulada.
    // Só no host (a lógica do item roda no servidor). Kill-switch: GhostBlades.Enabled; MaxPerSecond = 0 desliga o teto.
    public sealed class GhostBladeFeature : Feature
    {
        private readonly ConfigEntry<int> _max;
        private int _maxValue;
        private float _windowStart;
        private int _windowCount;

        public GhostBladeFeature(Cfg cfg, ManualLogSource log) : base("GhostBlades", cfg, log,
            "Limita quantas Lâminas Fantasmas (item Curse do DLC4) o jogo gera/renova por segundo, para não encher o jogo de projéteis. Só no host.",
            enabledByDefault: true)
        {
            _max = cfg.Bind("GhostBlades", "MaxPerSecond", 10,
                "Máximo de gerações/renovações de lâminas por segundo (somadas, todos os jogadores). O jogo chegava a ~700/s; com 100 ficava sempre no teto (2,8 mil projéteis por 30 s). 0 = sem teto.");
            _maxValue = _max.Value;
            _max.SettingChanged += (_, __) => _maxValue = _max.Value;
        }

        protected override void OnEnable()
        {
            Track("GhostBladesBehavior.SpawnGhostBlade", () => On.RoR2.Items.GhostBladesBehavior.SpawnGhostBlade += OnSpawn);
            Track("GhostBladesBehavior.RefreshOldestGhostBlade", () => On.RoR2.Items.GhostBladesBehavior.RefreshOldestGhostBlade += OnRefresh);
        }

        protected override void OnDisable()
        {
            On.RoR2.Items.GhostBladesBehavior.SpawnGhostBlade -= OnSpawn;
            On.RoR2.Items.GhostBladesBehavior.RefreshOldestGhostBlade -= OnRefresh;
        }

        public override void ResetRunState()
        {
            _windowStart = 0f;
            _windowCount = 0;
        }

        // Janela de 1 s por Time.time; chamado em frequência alta: sem alocação.
        private bool Allow()
        {
            if (_maxValue <= 0 || !NetworkServer.active) return true;
            var t = Time.time;
            if (t - _windowStart >= 1f || t < _windowStart) { _windowStart = t; _windowCount = 0; }
            if (++_windowCount <= _maxValue) return true;
            Diag.GhostBladesSkipped++;
            return false;
        }

        private void OnSpawn(On.RoR2.Items.GhostBladesBehavior.orig_SpawnGhostBlade orig, RoR2.Items.GhostBladesBehavior self)
        {
            if (Allow()) orig(self);
        }

        private void OnRefresh(On.RoR2.Items.GhostBladesBehavior.orig_RefreshOldestGhostBlade orig, RoR2.Items.GhostBladesBehavior self)
        {
            if (Allow()) orig(self);
        }
    }
}
