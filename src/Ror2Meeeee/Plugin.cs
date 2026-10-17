using System;
using System.Collections.Generic;
using System.Threading;
using BepInEx;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace Ror2Meeeee
{
    [BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(R2API.ItemAPI.PluginGUID)]
    [BepInDependency(R2API.LanguageAPI.PluginGUID)]
    [BepInDependency("com.bepis.r2api.content_management")]
    [BepInDependency(R2API.EliteAPI.PluginGUID)] // elites: os tiers do diretor já precisam estar copiados no Awake
    [BepInDependency("com.bepis.r2api.networking")] // totais da tela de status (F8) do host para os clientes
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static string GameVersion { get; private set; }

        public IReadOnlyList<Feature> Features => _features;
        public Probe Probe { get; private set; }

        private readonly List<Feature> _features = new List<Feature>();
        private Cfg _cfg;

        private void Awake()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew(); // tempo de carga do mod (linha [Carga] no fim)
            Instance = this;

            var build = GetBuildString();
            var gameVersion = string.IsNullOrEmpty(build) ? Application.version : $"{Application.version}#{build}";
            GameVersion = gameVersion;
            Logger.LogInfo($"{ModInfo.Guid} v{ModInfo.Version} carregado; jogo {gameVersion}");

            if (string.IsNullOrEmpty(build))
                Logger.LogWarning("Build do jogo indisponível; versão não verificada. O plugin segue carregado.");
            else if (gameVersion != ModInfo.ExpectedGameVersion)
                Logger.LogWarning($"Versão do jogo divergente: esperada {ModInfo.ExpectedGameVersion}, atual {gameVersion}. O plugin segue carregado.");

            Overlay.Install(gameObject);
            DropMenu.Install(gameObject, Config.Bind("Drop", "MenuKey", "F7", "Tecla (UnityEngine.KeyCode, ex.: F7, G, Insert) que abre o menu na tela para soltar itens."));
            StatsPanel.Install(gameObject, Config.Bind("Stats", "MenuKey", "F8", "Tecla (UnityEngine.KeyCode) que abre/fecha a tela de status: atributos atuais e totais da run (dano, cura, dano recebido)."));
            Diag.Install(Logger);
            TitheArtifact.Register(Logger);
            TaxArtifact.Register(Logger);
            SharingFeature.Register(Logger);
            var tRegister = sw.ElapsedMilliseconds;
            HordeItem.Register(Logger); // antes do ItemCatalog; falha não derruba o plugin
            CervoItem.Register(Logger);
            CanoItem.Register(Logger);
            GrimoireFeature.Register(Logger);
            ContractFeature.Register(Logger);
            ReviveFeature.Register(Logger);
            TaxReliefItems.Register(Logger);
            SelfHealFeature.Register(Logger);
            AegisFeature.Register(Logger);
            HeavyHeartFeature.Register(Logger);
            VoidBountyFeature.Register(Logger);
            PortalBatteryFeature.Register(Logger);
            BastionFeature.Register(Logger);
            RustyNailFeature.Register(Logger);
            ElitesFeature.Register(Logger);
            tRegister = sw.ElapsedMilliseconds - tRegister;
            _cfg = new Cfg(Config, Logger);
            Probe = new Probe(_cfg, Logger);
            _features.Add(Probe);
            _features.Add(new LimitedCalcFeature(_cfg, Logger));
            _features.Add(new MultiplierFeature(_cfg, Logger));
            _features.Add(new SharingFeature(_cfg, Logger));
            _features.Add(new DifficultyFeature(_cfg, Logger));
            _features.Add(new GoldFeature(_cfg, Logger));
            _features.Add(new TaxFeature(_cfg, Logger));
            _features.Add(new SpendFeature(_cfg, Logger));
            _features.Add(new ParticlesFeature(_cfg, Logger));
            _features.Add(new ProgressionFeature(_cfg, Logger));
            _features.Add(new CervoFeature(_cfg, Logger));
            _features.Add(new CanoFeature(_cfg, Logger));
            _features.Add(new GrimoireFeature(_cfg, Logger));
            _features.Add(new ContractFeature(_cfg, Logger));
            _features.Add(new ReviveFeature(_cfg, Logger));
            _features.Add(new DropFeature(_cfg, Logger));
            _features.Add(new GhostBladeFeature(_cfg, Logger));
            _features.Add(new CleanupFeature(_cfg, Logger));
            _features.Add(new SelfHealFeature(_cfg, Logger));
            _features.Add(new AegisFeature(_cfg, Logger));
            _features.Add(new HeavyHeartFeature(_cfg, Logger));
            _features.Add(new VoidBountyFeature(_cfg, Logger));
            _features.Add(new PortalBatteryFeature(_cfg, Logger));
            _features.Add(new BastionFeature(_cfg, Logger));
            _features.Add(new RustyNailFeature(_cfg, Logger));
            _features.Add(new ProjectileBudgetFeature(_cfg, Logger));
            _features.Add(new GameFixesFeature(_cfg, Logger));
            _features.Add(new BazaarFeature(_cfg, Logger));
            _features.Add(new ElitesFeature(_cfg, Logger));

            LogIdentity();

            foreach (var feature in _features)
            {
                if (feature.ConfigEnabled) feature.Enable();
                else Logger.LogInfo($"{feature.Name}: desligada por config");

                // Liga/desliga em jogo (tela de mods): só registra/remove os hooks, entre partidas.
                var f = feature;
                if (f.EnabledEntry != null) f.EnabledEntry.SettingChanged += (_, __) =>
                {
                    if (f.ConfigEnabled) f.Enable(); else f.Disable();
                    Logger.LogInfo($"{f.Name}: {(f.Enabled ? "ligada" : "desligada")} em jogo");
                };
            }

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
                ModOptions.Register(_cfg, Logger);

            // Único ponto de reset por run: início e fim.
            Run.onRunStartGlobal += OnRunBoundary;
            Run.onRunDestroyGlobal += OnRunBoundary;

            Logger.LogInfo($"[Carga] Awake em {sw.ElapsedMilliseconds} ms (registro de itens/artefatos {tRegister} ms; features e hooks {sw.ElapsedMilliseconds - tRegister} ms)");
        }

        // Uma linha dizendo QUAL DLL rodou e com quais configs; sem isso só se deduz a versão pela ausência de linhas.
        internal void LogIdentity()
        {
            try
            {
                var asm = typeof(Plugin).Assembly;
                var built = System.IO.File.GetLastWriteTimeUtc(asm.Location);
                Logger.LogInfo($"[Identidade] Ror2Meeeee v{ModInfo.Version} | build da DLL {built:yyyy-MM-dd HH:mm:ss}Z | {asm.Location} | jogo {GameVersion}");
                var bySection = new SortedDictionary<string, List<string>>();
                foreach (var def in Config.Keys)
                {
                    if (!bySection.TryGetValue(def.Section, out var list)) bySection[def.Section] = list = new List<string>();
                    list.Add($"{def.Key}={Config[def].BoxedValue}");
                }
                foreach (var kv in bySection) Logger.LogInfo($"[Config] [{kv.Key}] {string.Join(", ", kv.Value)}");
            }
            catch (Exception e)
            {
                Logger.LogWarning($"[Identidade] falha ao listar a config: {e.Message}");
            }
        }

        // Gravações (CSV, estado) vão para o ThreadPool; ao fechar o jogo essas threads morrem com o processo
        // antes de terminar. Fechando: tudo roda na hora, na principal.
        internal static bool Quitting { get; private set; }

        internal static void RunInBackground(Action work)
        {
            if (Quitting) work();
            else ThreadPool.QueueUserWorkItem(_ => work());
        }

        // Sair do jogo no meio da run não dispara onRunDestroyGlobal a tempo: fecha a run aqui (Spend, Probe...).
        private void OnApplicationQuit()
        {
            Quitting = true;
            if (Run.instance) OnRunBoundary(Run.instance);
        }

        private void OnDestroy()
        {
            Run.onRunStartGlobal -= OnRunBoundary;
            Run.onRunDestroyGlobal -= OnRunBoundary;
            foreach (var feature in _features) feature.Disable();
        }

        // Todas as features, ligadas ou não: evita cache velho se uma for religada.
        private void OnRunBoundary(Run run)
        {
            if (_cfg.VerboseLogging) Logger.LogDebug("ResetRunState de todas as features");
            foreach (var feature in _features) feature.ResetRunStateSafe();
        }

        // O jogo exibe "ver. <Application.version>#<AutomatedBuildVersion.buildVersionString>".
        // Reflexão porque uma const referenciada seria inlinada em compilação.
        // Qualquer falha vira null: a checagem de versão nunca pode derrubar o plugin.
        private static string GetBuildString()
        {
            try
            {
                var field = System.Type.GetType("AutomatedBuildVersion, RoR2")?.GetField("buildVersionString");
                return (field?.IsLiteral == true ? field.GetRawConstantValue() : field?.GetValue(null)) as string;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
