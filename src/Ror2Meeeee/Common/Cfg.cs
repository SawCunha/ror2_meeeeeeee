using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace Ror2Meeeee
{
    // Único lugar que chama ConfigFile.Bind. Valores em cache; atualizados só em SettingChanged.
    public sealed class Cfg
    {
        public const int MinMaxFailures = 50;
        // Versão do formato do .cfg. Migração só quando uma chave mudar de significado ou de padrão.
        // 2 (0.11.1): Tempest.ProtectionSeconds 3 -> 10 (só se ainda estava no padrão antigo).
        // 3 (0.11.3): Tempest.ProtectionSeconds 3 ou 10 -> 5.
        public const int ConfigVersion = 3;

        private readonly ConfigFile _file;
        private readonly ManualLogSource _log;
        private readonly ConfigEntry<bool> _verboseLogging;
        private readonly ConfigEntry<int> _maxFailures;

        private readonly List<ConfigEntryBase> _entries = new List<ConfigEntryBase>();
        private readonly Dictionary<ConfigEntryBase, string> _pt = new Dictionary<ConfigEntryBase, string>();

        // Todas as entradas do mod, na ordem de criação (para a tela de mods do jogo).
        public IReadOnlyList<ConfigEntryBase> Entries => _entries;

        public ConfigEntry<bool> ProbeEnabledEntry { get; }
        // Lidos na inicialização (requerem reiniciar o jogo).
        public bool ProbeEmptyHook { get; }
        public int ProbeAutoWindowSeconds { get; }
        public bool VerboseLogging { get; private set; }
        public int MaxFailures { get; private set; }
        // Versão que o .cfg tinha ao abrir (igual a ConfigVersion em config nova): as features migram suas chaves com ela.
        public int LoadedConfigVersion { get; }

        public Cfg(ConfigFile file, ManualLogSource log)
        {
            _file = file;
            _log = log;

            _verboseLogging = Bind("General", "VerboseLogging", false,
                "Extra debug logs in LogOutput.log. Keep it off for regular play: verbose logging costs FPS.",
                "Logs extras de depuração no LogOutput.log. Deixe desligado no dia a dia: log verboso custa FPS.");
            // Sem AcceptableValueRange: o clamp do BepInEx seria silencioso e o mínimo exige Warning.
            _maxFailures = Bind("General", "MaxFailures", MinMaxFailures,
                $"How many errors a feature may have before it turns itself off to protect the match. Minimum {MinMaxFailures}.",
                $"Quantos erros uma feature pode ter antes de se desligar sozinha para proteger a partida. Mínimo {MinMaxFailures}.");

            ProbeEnabledEntry = Bind("General", "ProbeEnabled", false,
                "Performance measurement tool: records frame time, RAM, projectiles and damage to BepInEx/ror2meeeee-probe.csv. For testing only; costs nothing when off. Does not change the game.",
                "Ferramenta de medição de desempenho: grava tempo de frame, RAM, projéteis e dano em BepInEx/ror2meeeee-probe.csv. Só para testes; desligada não custa nada. Não muda o jogo.");
            ProbeEmptyHook = Bind("General", "ProbeEmptyHook", false,
                "Probe test: adds an empty hook to measure what a hook alone costs. Requires restarting the game.",
                "Teste do Probe: adiciona um hook vazio para medir quanto custa um hook sozinho. Exige reiniciar o jogo.").Value;

            ProbeAutoWindowSeconds = Math.Max(0, Bind("General", "ProbeAutoWindowSeconds", 30,
                "Probe: measures on its own during the run in X-second windows (each window becomes a line in the CSV and the log). 0 = only with the ror2meeeee_probe_* commands. Requires restarting the game.",
                "Probe: mede sozinho durante a run em janelas de X segundos (cada janela vira uma linha no CSV e no log). 0 = só com os comandos ror2meeeee_probe_*. Exige reiniciar o jogo.").Value);

            var version = Bind("General", "ConfigVersion", ConfigVersion,
                "Format version of this file, used to migrate keys between mod versions. Do not change.",
                "Versão do formato deste arquivo, usada para migrar chaves entre versões do mod. Não altere.");
            LoadedConfigVersion = version.Value;
            if (version.Value != ConfigVersion)
            {
                _log.LogInfo($"General.ConfigVersion={version.Value} -> {ConfigVersion}: as features migram as chaves que mudaram.");
                version.Value = ConfigVersion;
            }

            _verboseLogging.SettingChanged += (_, __) => Refresh();
            _maxFailures.SettingChanged += (_, __) => Refresh();
            Refresh();
        }

        private const string EnabledSuffixEn = " Toggles immediately; the effect applies from the next event or stage.";
        private const string EnabledSuffixPt = " Liga/desliga na hora; o efeito vale a partir do próximo evento ou estágio.";

        // Toda feature tem seu próprio kill-switch: <Seção>.Enabled, padrão desligado (salvo exceção explícita).
        // `summaryEn`/`summaryPt` = o que a feature faz, em linguagem de jogador (é o que a tela de mods mostra).
        // A primeira chamada para a seção define o texto (o BepInEx devolve a mesma entrada depois).
        public ConfigEntry<bool> BindEnabled(string section, string summaryEn = null, string summaryPt = null, bool defaultValue = false)
        {
            return Bind(section, "Enabled", defaultValue,
                (summaryEn ?? "Turns this feature on.") + EnabledSuffixEn, (summaryPt ?? "Liga esta feature.") + EnabledSuffixPt);
        }

        // Config de feature: sempre por aqui, nunca ConfigFile.Bind direto.
        // O .cfg leva a descrição em inglês; a em português vai só para a tela de mods (ModOptions).
        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string descriptionEn, string descriptionPt)
        {
            var entry = _file.Bind(section, key, defaultValue, descriptionEn);
            if (!_entries.Contains(entry)) // mesma chave ligada por duas features = mesma entrada
            {
                _entries.Add(entry);
                _pt[entry] = descriptionPt;
            }
            return entry;
        }

        public string DescriptionPt(ConfigEntryBase entry) => _pt.TryGetValue(entry, out var pt) ? pt : entry.Description.Description;

        private void Refresh()
        {
            VerboseLogging = _verboseLogging.Value;

            var max = _maxFailures.Value;
            if (max < MinMaxFailures)
            {
                _log.LogWarning($"General.MaxFailures={max} inválido (mínimo {MinMaxFailures}); usando {MinMaxFailures}.");
                max = MinMaxFailures;
                _maxFailures.Value = max; // dispara SettingChanged; o Refresh aninhado já vê o valor válido
            }
            MaxFailures = max;
        }
    }
}
