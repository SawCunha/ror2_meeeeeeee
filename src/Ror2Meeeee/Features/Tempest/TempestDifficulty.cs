using System;
using BepInEx.Logging;
using R2API;
using RoR2;
using UnityEngine;

namespace Ror2Meeeee
{
    // Dificuldade Tormenta (interno Tempest; "Tempestade" é o Rainstorm no pt-BR do jogo), registrada pelo R2API.Difficulty (índice negativo: sem modificadores de Eclipse).
    // O efeito é aplicado pela TempestFeature; com Tempest.Enabled = false ela se comporta só pelo scalingValue.
    public static class TempestDifficulty
    {
        public const string Token = "ROR2MEEEEE_DIFFICULTY_TEMPEST";

        public static DifficultyIndex Index { get; private set; } = DifficultyIndex.Invalid;

        // Chamar no Awake do plugin, depois do Cfg (antes do DifficultyCatalog). Falha não derruba o plugin.
        public static void Register(ManualLogSource log, Cfg cfg)
        {
            try
            {
                var entry = cfg.Bind("Tempest", "ScalingValue", 3.5f,
                    "How fast the Tempest difficulty scales over time (Drizzle 1, Rainstorm 2, Monsoon 3). Range 2 to 10. Requires restarting the game.",
                    "Quão rápido a dificuldade Tormenta escala com o tempo (Garoa 1, Tempestade 2, Monção 3). Faixa 2 a 10. Exige reiniciar o jogo.");
                var scaling = Core.Tempest.TempestRules.Clamp(entry.Value, 3.5f, 2f, 10f, out var corrected);
                if (corrected)
                {
                    log.LogWarning($"Tempest.ScalingValue={entry.Value} inválido; usando {scaling}.");
                    entry.Value = scaling;
                }

                Loc.Add(Token + "_NAME", "Tempest", "Tormenta");
                Loc.Add(Token + "_DESC",
                    "For those who think Monsoon is a breeze.<style=cStack>\n\n" +
                    ">Difficulty scaling: +" + Percent(scaling) + "\n" +
                    ">Larger, constant hordes; more elites\n" +
                    ">New enemies are invulnerable for 3 s\n" +
                    ">Each stage cleared gives enemies damage reduction and the same in max health (up to 99%)\n" +
                    ">The Primordial Teleporter only opens on stage 10</style>",
                    "Para quem acha a Monção uma brisa.<style=cStack>\n\n" +
                    ">Escalada da dificuldade: +" + Percent(scaling) + "\n" +
                    ">Hordas maiores e constantes; mais elites\n" +
                    ">Inimigos novos ficam invulneráveis por 3 s\n" +
                    ">Cada estágio concluído dá aos inimigos redução de dano e o mesmo em vida máxima (até 99%)\n" +
                    ">O Teleportador Primordial só se abre no estágio 10</style>");

                var def = new DifficultyDef(scaling, Token + "_NAME", "", Token + "_DESC", new Color32(120, 50, 170, 255), "tm", countsAsHardMode: true);
                var icon = TitheArtifact.LoadIcon("tempestade.png", new Color32(120, 50, 170, 255));
                Index = DifficultyAPI.AddDifficulty(def, icon);
                if (Index == DifficultyIndex.Invalid) log.LogError("Tempest: R2API.Difficulty recusou a dificuldade.");
                else log.LogInfo($"Tempest: dificuldade registrada (índice {(int)Index}, scalingValue {scaling}).");
            }
            catch (Exception e)
            {
                log.LogError($"Tempest: falha ao registrar a dificuldade (o plugin segue sem ela): {e}");
                Index = DifficultyIndex.Invalid;
            }
        }

        // Mesma conta das descrições do jogo (Garoa -50%, Chuva +0%, Monção +50%): 50% por ponto acima de 2.
        private static string Percent(float scaling) => ((scaling - 2f) * 50f).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%";

        public static bool IsSelected()
        {
            var run = Run.instance;
            return Index != DifficultyIndex.Invalid && run && run.selectedDifficulty == Index;
        }
    }
}
