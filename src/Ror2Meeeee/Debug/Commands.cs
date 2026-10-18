using System.Text;
using HG.Reflection;
using RoR2;
using UnityEngine;

[assembly: SearchableAttribute.OptIn]

namespace Ror2Meeeee
{
    // Comandos de console. Rodam só sob demanda: nada por tick, nada registrado em hook.
    internal static class Commands
    {
        [ConCommand(commandName = "ror2meeeee_status", flags = ConVarFlags.None, helpText = "Lista features, falhas e hooks do mod.")]
        private static void CcStatus(ConCommandArgs args)
        {
            Debug.Log(BuildStatus(Plugin.Instance));
        }

        // ExecuteOnServer: roda no host com o remetente certo, mesmo vindo de um cliente.
        [ConCommand(commandName = "ror2meeeee_drop", flags = ConVarFlags.ExecuteOnServer, helpText = "Solta itens do seu inventário no chão: ror2meeeee_drop [nome] [quantidade]. Sem argumentos, lista o que dá para soltar.")]
        private static void CcDrop(ConCommandArgs args)
        {
            DropFeature feature = null;
            foreach (var f in Plugin.Instance.Features)
                if (f is DropFeature d) { feature = d; break; }
            if (feature == null || !feature.Enabled) { Debug.Log("Drop desligado (Drop.Enabled=false)."); return; }

            var words = new System.Collections.Generic.List<string>();
            for (var i = 0; i < args.Count; i++) words.Add(args[i]);
            var qty = 1;
            if (words.Count > 1 && int.TryParse(words[words.Count - 1], out var n)) { qty = n; words.RemoveAt(words.Count - 1); }
            Debug.Log(feature.Execute(args.senderMaster, string.Join(" ", words), qty));
        }

        [ConCommand(commandName = "ror2meeeee_drop_menu", flags = ConVarFlags.None, helpText = "Abre a tela de soltar itens, já com o item escolhido: ror2meeeee_drop_menu [nome]")]
        private static void CcDropMenu(ConCommandArgs args)
        {
            var words = new System.Collections.Generic.List<string>();
            for (var i = 0; i < args.Count; i++) words.Add(args[i]);
            Debug.Log(DropMenu.OpenWith(string.Join(" ", words)));
        }

        [ConCommand(commandName = "ror2meeeee_demo", flags = ConVarFlags.None, helpText = "Mostra por 15 s um aviso e marcadores de teste (lápide e alvo) à frente do jogador.")]
        private static void CcDemo(ConCommandArgs args)
        {
            var body = args.senderBody;
            if (!body) { Debug.Log("Ror2Meeeee: entre em uma run primeiro."); return; }
            var ahead = body.footPosition + body.transform.forward * 15f;
            Overlay.Mark("demo-tomb", ahead, "LÁPIDE de Teste", new Color(0.95f, 0.85f, 0.35f));
            Overlay.Mark("demo-target", body.footPosition - body.transform.forward * 20f, "ALVO DO CONTRATO", new Color(0.9f, 0.25f, 0.3f));
            Overlay.Banner("DEMO: assim aparecem a lápide (amarela) e o alvo do contrato (vermelho).", 15f);
            Chat.AddMessage("<color=#e5c07b>Ror2Meeeee:</color> demonstração na tela por 15 s.");
            Plugin.Instance.StartCoroutine(ClearDemo());
        }

        private static System.Collections.IEnumerator ClearDemo()
        {
            yield return new WaitForSecondsRealtime(15f);
            Overlay.Unmark("demo-tomb");
            Overlay.Unmark("demo-target");
        }

        // O jogo não recarrega o .cfg sozinho (sob Proton o watcher não dispara): este comando relê o arquivo
        // e dispara SettingChanged só nas entradas que mudaram (Mode, Limit, Factor, bônus, Value...).
        [ConCommand(commandName = "ror2meeeee_reload", flags = ConVarFlags.None, helpText = "Relê o .cfg do mod (Mode, Limit, Factor, bônus...). Enabled já vale na hora, entre partidas.")]
        private static void CcReload(ConCommandArgs args)
        {
            Plugin.Instance.Config.Reload();
            Plugin.Instance.LogIdentity();
            Debug.Log("Ror2Meeeee: config recarregada. As linhas [LimitedCalc]/[Multiplier]/[Difficulty] no log mostram o que mudou.");
        }

        [ConCommand(commandName = "ror2meeeee_effects", flags = ConVarFlags.None, helpText = "Efeitos mais frequentes (precisa de Particles.CountSpawns): ror2meeeee_effects [top]")]
        private static void CcEffects(ConCommandArgs args)
        {
            var top = args.Count > 0 && int.TryParse(args[0], out var n) && n > 0 ? n : 20;
            foreach (var f in Plugin.Instance.Features)
                if (f is ParticlesFeature p) { Debug.Log(p.DumpCounts(top)); return; }
            Debug.Log("ParticlesFeature indisponível.");
        }

        [ConCommand(commandName = "ror2meeeee_spend", flags = ConVarFlags.None, helpText = "Compras e ouro gasto por jogador nesta run (precisa de Spend.Enabled).")]
        private static void CcSpend(ConCommandArgs args)
        {
            foreach (var f in Plugin.Instance.Features)
                if (f is SpendFeature s) { Debug.Log(f.Enabled ? s.Report() : "Spend desligada (Spend.Enabled=false)."); return; }
            Debug.Log("SpendFeature indisponível.");
        }

        [ConCommand(commandName = "ror2meeeee_progress", flags = ConVarFlags.None, helpText = "Nível de dificuldade entre runs e multiplicador da próxima run (Progression).")]
        private static void CcProgress(ConCommandArgs args)
        {
            foreach (var f in Plugin.Instance.Features)
                if (f is ProgressionFeature p)
                {
                    Debug.Log($"Progression {(f.Enabled ? "ligada" : "desligada")}: nível {p.Level}, {p.Runs} runs, orçamento de spawn x{p.CurrentMultiplier:F2}");
                    return;
                }
            Debug.Log("ProgressionFeature indisponível.");
        }

        [ConCommand(commandName = "ror2meeeee_progress_reset", flags = ConVarFlags.None, helpText = "Zera o nível de dificuldade entre runs (vale a partir da próxima run).")]
        private static void CcProgressReset(ConCommandArgs args)
        {
            foreach (var f in Plugin.Instance.Features)
                if (f is ProgressionFeature p) { p.Reset(); Debug.Log("Progression: nível zerado (vale na próxima run)."); return; }
            Debug.Log("ProgressionFeature indisponível.");
        }

        [ConCommand(commandName = "ror2meeeee_probe_begin", flags = ConVarFlags.None, helpText = "Inicia uma fase de medição: ror2meeeee_probe_begin <nome>")]
        private static void CcProbeBegin(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);
            Debug.Log(Plugin.Instance.Probe.Begin(args[0]));
        }

        [ConCommand(commandName = "ror2meeeee_probe_label", flags = ConVarFlags.None, helpText = "Rotula as janelas automáticas seguintes (ex.: base, c3): ror2meeeee_probe_label <nome>")]
        private static void CcProbeLabel(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);
            Plugin.Instance.Probe.Label = args[0].Replace(',', ';');
            Debug.Log($"Janelas automáticas rotuladas como '{Plugin.Instance.Probe.Label}'.");
        }

        [ConCommand(commandName = "ror2meeeee_probe_end", flags = ConVarFlags.None, helpText = "Encerra a fase e grava o CSV.")]
        private static void CcProbeEnd(ConCommandArgs args)
        {
            Debug.Log(Plugin.Instance.Probe.End());
        }

        internal static string BuildStatus(Plugin plugin)
        {
            var sb = new StringBuilder();
            sb.Append("Ror2Meeeee v").Append(ModInfo.Version).Append(" | jogo ").Append(Plugin.GameVersion).Append('\n');
            var features = plugin.Features;
            if (features.Count == 0) sb.Append("(nenhuma feature)\n");
            foreach (var f in features)
            {
                var state = f.ShutdownByFailures ? "DESLIGADA POR FALHA"
                    : f.Enabled ? "ligada"
                    : f.ConfigEnabled ? "desligada (hooks não registrados)"
                    : "desligada por config";
                sb.Append("- ").Append(f.Name).Append(": ").Append(state)
                  .Append(" | falhas ").Append(f.FailureCount).Append('\n');
                foreach (var h in f.HookResults) sb.Append("    hook ").Append(h).Append('\n');
            }
            sb.Append(Diag.Report()).Append('\n');
            return sb.ToString().TrimEnd('\n');
        }
    }
}
