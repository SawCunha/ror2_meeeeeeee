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

        private readonly ConfigFile _file;
        private readonly ManualLogSource _log;
        private readonly ConfigEntry<bool> _verboseLogging;
        private readonly ConfigEntry<int> _maxFailures;

        private readonly List<ConfigEntryBase> _entries = new List<ConfigEntryBase>();

        // Todas as entradas do mod, na ordem de criação (para a tela de mods do jogo).
        public IReadOnlyList<ConfigEntryBase> Entries => _entries;

        public ConfigEntry<bool> ProbeEnabledEntry { get; }
        // Lidos na inicialização (requerem reiniciar o jogo).
        public bool ProbeEmptyHook { get; }
        public int ProbeAutoWindowSeconds { get; }
        public bool VerboseLogging { get; private set; }
        public int MaxFailures { get; private set; }

        public Cfg(ConfigFile file, ManualLogSource log)
        {
            _file = file;
            _log = log;

            _verboseLogging = Bind("General", "VerboseLogging", false,
                "Logs extras de depuração no LogOutput.log. Deixe desligado no dia a dia: log verboso custa FPS.");
            // Sem AcceptableValueRange: o clamp do BepInEx seria silencioso e o mínimo exige Warning.
            _maxFailures = Bind("General", "MaxFailures", MinMaxFailures,
                $"Quantos erros uma feature pode ter antes de se desligar sozinha para proteger a partida. Mínimo {MinMaxFailures}.");

            ProbeEnabledEntry = Bind("General", "ProbeEnabled", false,
                "Ferramenta de medição de desempenho: grava tempo de frame, RAM, projéteis e dano em BepInEx/ror2meeeee-probe.csv. Só para testes; desligada não custa nada. Não muda o jogo.");
            ProbeEmptyHook = Bind("General", "ProbeEmptyHook", false,
                "Teste do Probe: adiciona um hook vazio para medir quanto custa um hook sozinho. Exige reiniciar o jogo.").Value;

            ProbeAutoWindowSeconds = Math.Max(0, Bind("General", "ProbeAutoWindowSeconds", 30,
                "Probe: mede sozinho durante a run em janelas de X segundos (cada janela vira uma linha no CSV e no log). 0 = só com os comandos ror2meeeee_probe_*. Exige reiniciar o jogo.").Value);

            _verboseLogging.SettingChanged += (_, __) => Refresh();
            _maxFailures.SettingChanged += (_, __) => Refresh();
            Refresh();
        }

        private const string EnabledSuffix = " Liga/desliga na hora; o efeito vale a partir do próximo evento ou estágio.";

        // Toda feature tem seu próprio kill-switch: <Seção>.Enabled, padrão desligado (salvo exceção explícita).
        // `summary` = o que a feature faz, em linguagem de jogador (é o que a tela de mods mostra).
        // A primeira chamada para a seção define o texto (o BepInEx devolve a mesma entrada depois).
        public ConfigEntry<bool> BindEnabled(string section, string summary = null, bool defaultValue = false)
        {
            return Bind(section, "Enabled", defaultValue, (summary ?? "Liga esta feature.") + EnabledSuffix);
        }

        // Config de feature: sempre por aqui, nunca ConfigFile.Bind direto.
        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description)
        {
            var entry = _file.Bind(section, key, defaultValue, description);
            if (!_entries.Contains(entry)) _entries.Add(entry); // mesma chave ligada por duas features = mesma entrada
            return entry;
        }

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
