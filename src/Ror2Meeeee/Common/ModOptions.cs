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
        private static readonly string[] RestartKeys = { "ProbeEmptyHook", "ProbeAutoWindowSeconds", "ScalingValue", "ConfigVersion" };

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Register(Cfg cfg, ManualLogSource log)
        {
            try
            {
                Loc.Add(DescToken, "Item multiplier and sharing, adjustable difficulty with progression between runs, the Tempest difficulty, the Tithe and Taxade artifacts (with the Capitalism items), particle limiting and measurement tools. Each section has an Enabled that explains what it does. Only the host needs to configure; changes apply immediately or from the next stage.",
                    "Multiplicador e compartilhamento de itens, dificuldade ajustável com progressão entre runs, a dificuldade Tormenta, artefatos do Dízimo e Taxade (com os itens Capitalismo), corte de partículas e ferramentas de medição. Cada seção tem um Enabled que explica o que ela faz. Só o host precisa configurar; alterações valem na hora ou a partir do próximo estágio.");
                ModSettingsManager.SetModDescriptionToken(DescToken, ModInfo.Guid, ModInfo.Name);
                // Ícone do mod na tela de mods (PNG embutido); sem ele a tela usa o padrão.
                var icon = TitheArtifact.LoadIcon("mod_icon.png", default(UnityEngine.Color32));
                if (icon) ModSettingsManager.SetModIcon(icon, ModInfo.Guid, ModInfo.Name);
                var n = 0;
                foreach (var entry in cfg.Entries)
                {
                    var option = Create(entry);
                    if (option == null) continue;
                    AddLocalized(option, entry, cfg);
                    n++;
                }
                log.LogInfo($"Opções na tela de mods: {n} entradas.");
            }
            catch (Exception e)
            {
                log.LogWarning($"Tela de mods indisponível (o plugin segue; use o .cfg): {e.GetType().Name}: {e.Message}");
            }
        }

        private const string DescToken = "ROR2MEEEEE_OPTIONS_DESC";

        // Tokens próprios (fora do prefixo RISK_OF_OPTIONS, que o Risk of Options responde sem olhar o idioma):
        // descrição em inglês como base e em português no pt-BR; o nome é a chave do .cfg nos dois.
        private static void AddLocalized(BaseOption option, ConfigEntryBase entry, Cfg cfg)
        {
            var def = entry.Definition;
            var token = ("ROR2MEEEEE_OPTION_" + def.Section + "_" + def.Key).Replace(' ', '_').Replace('.', '_').ToUpperInvariant();
            R2API.LanguageAPI.Add(token + "_NAME", def.Key);
            Loc.Add(token + "_DESC", entry.Description.Description, cfg.DescriptionPt(entry));
            ModSettingsManager.AddOption(option, ModInfo.Guid, ModInfo.Name, token + "_NAME", token + "_DESC");
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
