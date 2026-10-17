using System;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using BepInEx.Logging;
using RiskOfOptions;
using RiskOfOptions.Options;

namespace Ror2Meeeee
{
    // Expõe toda a config na tela de mods do jogo (Risk of Options, dependência opcional).
    // Classe separada e sem inline: o plugin carrega sem o Risk of Options instalado.
    internal static class ModOptions
    {
        // Lidas só na inicialização do Probe: a tela avisa que exigem reiniciar.
        private static readonly string[] RestartKeys = { "ProbeEmptyHook", "ProbeAutoWindowSeconds" };

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Register(Cfg cfg, ManualLogSource log)
        {
            try
            {
                ModSettingsManager.SetModDescription("Multiplicador e compartilhamento de itens, dificuldade ajustável com progressão entre runs, artefatos do Dízimo e Taxade (com os itens Capitalismo), corte de partículas e ferramentas de medição. Cada seção tem um Enabled que explica o que ela faz. Só o host precisa configurar; alterações valem na hora ou a partir do próximo estágio.",
                    ModInfo.Guid, ModInfo.Name);
                // Ícone do mod na tela de mods (PNG embutido); sem ele a tela usa o padrão.
                var icon = TitheArtifact.LoadIcon("mod_icon.png", default(UnityEngine.Color32));
                if (icon) ModSettingsManager.SetModIcon(icon, ModInfo.Guid, ModInfo.Name);
                var n = 0;
                foreach (var entry in cfg.Entries)
                {
                    var option = Create(entry);
                    if (option == null) continue;
                    ModSettingsManager.AddOption(option, ModInfo.Guid, ModInfo.Name);
                    n++;
                }
                log.LogInfo($"Opções na tela de mods: {n} entradas.");
            }
            catch (Exception e)
            {
                log.LogWarning($"Tela de mods indisponível (o plugin segue; use o .cfg): {e.GetType().Name}: {e.Message}");
            }
        }

        private static BaseOption Create(ConfigEntryBase entry)
        {
            var restart = Array.IndexOf(RestartKeys, entry.Definition.Key) >= 0;
            switch (entry)
            {
                case ConfigEntry<bool> b: return new CheckBoxOption(b, restart);
                case ConfigEntry<int> i: return new IntFieldOption(i, restart);
                case ConfigEntry<float> f: return new FloatFieldOption(f, restart);
                case ConfigEntry<string> s: return new StringInputFieldOption(s, restart);
                default:
                    return entry.SettingType.IsEnum ? new ChoiceOption(entry, restart) : null;
            }
        }
    }
}
