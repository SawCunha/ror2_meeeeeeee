using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using BepInEx.Logging;
using MonoMod.Cil;
using RoR2;
using Ror2Meeeee.Core.Tempest;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Dificuldade Tormenta (docs/especificacao_mod_risk_of_rain_2.md). Só vale nas runs com ela selecionada; a lógica de
    // gameplay roda no host. Config congelada no primeiro uso da run (EnsureSnapshot), exceto os limiares de desempenho.
    //  - Proteção: buff HiddenRejectAllDamage (rejeita dano, força e DoT no topo do TakeDamageProcess; o ReduceDamage marca
    //    damageInfo.rejected para o atacante não rodar os procs do OnHitEnemy),
    //    uma vez por CharacterMaster. Junto, o buff Immune do jogo pelo mesmo tempo, só pelo brilho dourado (o dano já
    //    sai antes, no HiddenRejectAllDamage, então não aparece o texto "imune" a cada golpe).
    //  - Capacidade: softCharacterLimit do time Monster elevado na run (restaurado no fim).
    //  - Hordas: crédito extra no diretor principal (tick de 1 s), com portão antes do teto, governador de desempenho
    //    e fases de enxame (skipSpawnIfTooCheap = false) / pesada (jogo).
    //  - Vida/dano: BoostHp/BoostDamage pelas curvas; redução de dano (BodyHooks) e o mesmo em vida (item oculto).
    //  - Elites: eliteBias dos diretores de monstros.
    //  - Final: Teleportador Primordial preso no próximo loop até FinalStage; finais alternativos opcionais.
    public sealed class TempestFeature : Feature
    {
        private sealed class FloatKey
        {
            public ConfigEntry<float> Entry;
            public float Default, Min, Max;
        }

        private sealed class IntKey
        {
            public ConfigEntry<int> Entry;
            public int Min, Max;
        }

        // Valores originais de um diretor, guardados na primeira vez que ele é visto no estágio.
        private sealed class DirectorState
        {
            public float EliteBias;
            public bool Skip;
            public int MaxSpawns;
            public bool Main;
            public bool Swarm;
        }

        private sealed class Settings
        {
            public float Protection, CreditPerSecond, RampSeconds, PressureStart, PressurePerMinute, PressureMax;
            public float SwarmSeconds, HeavySeconds, MultiplierExponent;
            public int CapacityBase, CapacityPerPlayer, CapacityMax, Headroom, SwarmWaveSize, FinalStage, LivesNormal, LivesElite;
            public TempestCurve Spawn, Health, Damage, Elite;
            public int[] Steps;
        }

        // Finais alternativos: cards de portal bloqueados até FinalStage (nomes do catálogo de Addressables).
        // ponytail: listas fixas do levantamento de 2026-10-18; confirmar com ror2meeeee_portals em jogo (§8.7).
        private static readonly string[] VoidlingCards = { "iscDeepVoidPortal" };
        private static readonly string[] FalseSonCards = { "iscColossusPortal" };
        private static readonly string[] SolusCards = { "iscEyePortal", "iscHardwareProgPortal", "iscHardwareProgPortal_Haunt" };
        private static readonly string[] ChampRushCards = { "iscChampRushPortal" };

        private const string FinalLockedToken = "ROR2MEEEEE_TEMPEST_FINAL_LOCKED";

        // O FixedUpdate do jogo copia este campo (protected) para a interação a cada tick; Disabled em caixa única: sem alocação.
        private static readonly FieldInfo PreferredInteractabilityField = typeof(EntityStates.LunarTeleporter.LunarTeleporterBaseState)
            .GetField("preferredInteractability", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly object DisabledBoxed = Interactability.Disabled;

        internal static TempestFeature Current { get; private set; }

        private readonly FloatKey _protection, _creditPerSecond, _ramp, _pressureStart, _pressurePerMinute, _pressureMax;
        private readonly FloatKey _swarmSeconds, _heavySeconds, _multiplierExponent;
        private readonly FloatKey _perfEnterMs, _perfEnterSeconds, _perfExitMs, _perfExitSeconds, _perfRecovery;
        private readonly FloatKey[] _spawn, _health, _damage, _elite;
        private readonly IntKey _capBase, _capPerPlayer, _capMax, _headroom, _swarmWave, _finalStage, _livesNormal, _livesElite;
        private readonly ConfigEntry<string> _steps;
        private readonly ConfigEntry<bool> _debug;
        private bool _debugOn;
        private readonly HashSet<int> _dbgHealthBodies = new HashSet<int>();
        private int _dbgSpawns, _dbgHits, _dbgRejected, _dbgHealth;
        private float _dbgNextTick;
        private readonly ConfigEntry<bool> _blockObliteration, _blockVoidling, _blockFalseSon, _blockSolus, _blockChampRush;

        // Lentes do Vidente Perdido: contagem que o jogo leu no golpe atual (IL) e quanto da vida restante o sorteio tira.
        private const float LensHealthCut = 0.9f;
        private int _lensCount, _dbgLens;
        private GameObject _lensEffect;

        private readonly OverloadGovernor _governor = new OverloadGovernor();
        private readonly Dictionary<int, int> _protectedIds = new Dictionary<int, int>(); // master -> vidas extras já gastas ao proteger
        private readonly HashSet<string> _blockedCards = new HashSet<string>();
        private readonly Dictionary<int, DirectorState> _directors = new Dictionary<int, DirectorState>();

        // Estado da run (host).
        private bool _snapshotted, _active, _blockMs;
        private Settings _s;
        private int _players, _capacity;
        private bool _capSaved;
        private int _origLimit;
        private float _reduction; // do estágio atual, 0..0,99
        private int _reductionMultiplier = 1; // Multiplicador lido junto com a redução (log)
        private static MasterCatalog.MasterIndex _infestorIndex = MasterCatalog.MasterIndex.none; // lido no snapshot (catálogo pronto)
        private int _reductionStage = -1;

        // Estado do estágio (host).
        private float _acc, _gate, _chargeSeconds;
        private bool _noticeSent, _overCapWarned, _lockLogged;
        private int _alive, _sPeak, _sOverCap, _sSwitches, _sProtected;
        private double _sCredit;
        private float _sGateClosed, _sDegraded;
        private float _lastSpawn = 1f, _lastHealth = 1f, _lastDamage = 1f, _lastElite = 1f, _lastPressure;
        private bool _lastSwarm, _lastCharged;

        // Todas as máquinas: liga o passo de vida do BodyHooks nas runs da Tormenta.
        internal bool HealthHook { get; private set; }
        // Hook de dano em toda run da Tormenta (host): redução por estágio e Lentes do Vidente Perdido.
        internal bool DamageHook => _active && NetworkServer.active;
        internal bool DebugOn => _debugOn;

        public TempestFeature(Cfg cfg, ManualLogSource log) : base("Tempest", cfg, log,
            "Tempest difficulty: only in runs with it selected in the lobby. Larger hordes (more director credit and a higher monster cap), 3 s of invulnerability for new enemies, health/damage/elites growing with time and stages, damage reduction plus the same in max health per stage cleared, and the Primordial Teleporter only opens on FinalStage. Host only.",
            "Dificuldade Tormenta: só nas runs com ela escolhida no lobby. Hordas maiores (mais crédito do diretor e teto de monstros maior), 3 s de invulnerabilidade para inimigos novos, vida/dano/elites crescendo com o tempo e os estágios, redução de dano e o mesmo em vida máxima por estágio concluído, e o Teleportador Primordial só se abre no FinalStage. Só o host.",
            enabledByDefault: true)
        {
            const string NextRun = " Applies from the next run.";
            const string NextRunPt = " Vale a partir da próxima run.";
            _protection = F("ProtectionSeconds", 5f, 0f, 10f, "Seconds of invulnerability for each new enemy (damage, knockback, damage over time and on-hit effects are ignored; it still moves and attacks). Once per enemy, and again each time it revives with Dio's Best Friend. 0 = off." + NextRun, "Segundos de invulnerabilidade de cada inimigo novo (dano, empurrão, dano contínuo e efeitos ao acertar são ignorados; ele continua andando e atacando). Uma vez por inimigo, e de novo a cada vez que ele revive com o Dio's Best Friend. 0 = desliga." + NextRunPt);
            // Padrões antigos (3 até a 0.11.0, 10 na 0.11.1/0.11.2) viram o atual; valor escolhido à mão fica.
            var oldProtection = _protection.Entry.Value;
            if (cfg.LoadedConfigVersion < 3 && (Math.Abs(oldProtection - 3f) < 0.01f || Math.Abs(oldProtection - 10f) < 0.01f))
            {
                _protection.Entry.Value = 5f;
                Log.LogInfo($"[Tempest] ProtectionSeconds {oldProtection:0} -> 5 (padrão novo da 0.11.3).");
            }
            _livesNormal = I("ExtraLivesNormal", 1, 0, 5, "Dio's Best Friend given to each normal enemy (revives once per copy). Bosses never get it." + NextRun, "Dio's Best Friend dado a cada inimigo normal (revive uma vez por cópia). Chefes nunca recebem." + NextRunPt);
            _livesElite = I("ExtraLivesElite", 2, 0, 5, "Dio's Best Friend given to each elite enemy (bosses never get it, even elite ones)." + NextRun, "Dio's Best Friend dado a cada inimigo elite (chefes nunca recebem, mesmo elites)." + NextRunPt);
            _capBase = I("CapacityBase", 60, 40, 200, "Maximum monsters alive at once with 1 player (game: 40). The director's extra credit stops before reaching it." + NextRun, "Máximo de monstros vivos ao mesmo tempo com 1 jogador (jogo: 40). O crédito extra do diretor para antes de chegar nele." + NextRunPt);
            _capPerPlayer = I("CapacityPerExtraPlayer", 10, 0, 50, "Extra monster cap per player beyond the first, counted at the start of the run." + NextRun, "Teto extra de monstros por jogador além do primeiro, contado no início da run." + NextRunPt);
            _capMax = I("CapacityMax", 120, 40, 300, "Absolute cap of monsters alive." + NextRun, "Teto absoluto de monstros vivos." + NextRunPt);
            _headroom = I("CapacityHeadroom", 5, 1, 20, "How many free slots below the cap make the extra credit stop." + NextRun, "Quantas vagas abaixo do teto fazem o crédito extra parar." + NextRunPt);
            _ramp = F("CapacityRampSeconds", 10f, 1f, 60f, "Seconds for the extra credit to come back to 100% after slots open up (no burst)." + NextRun, "Segundos para o crédito extra voltar a 100% depois que abrem vagas (sem rajada)." + NextRunPt);
            _creditPerSecond = F("SpawnCreditPerSecond", 1f, 0f, 20f, "Base of the director's extra credit per second, with the same growth as the game's credit. 0 = no extra hordes." + NextRun, "Base do crédito extra do diretor por segundo, com o mesmo crescimento do crédito do jogo. 0 = sem hordas extras." + NextRunPt);
            _spawn = Curve("Spawn", 0.02f, 0.10f, 3f, 10f, "extra hordes", "hordas extras");
            _health = Curve("Health", 0.01f, 0.05f, 3f, 20f, "enemy health (10% steps)", "vida dos inimigos (passos de 10%)");
            _damage = Curve("Damage", 0.005f, 0.03f, 2f, 10f, "enemy damage (10% steps)", "dano dos inimigos (passos de 10%)");
            _elite = Curve("Elite", 0.01f, 0.05f, 3f, 10f, "elite frequency (elites get cheaper for the director)", "frequência de elites (elites ficam mais baratos para o diretor)");
            _pressureStart = F("StagePressureStartSeconds", 300f, 0f, 3600f, "Seconds in the stage before the stay pressure starts adding hordes. Frozen while the teleporter charges." + NextRun, "Segundos no estágio antes de a pressão por permanência começar a somar hordas. Congela enquanto o teleportador carrega." + NextRunPt);
            _pressurePerMinute = F("StagePressurePerMinute", 0.1f, 0f, 2f, "How much the stay pressure adds to the spawn curve per minute." + NextRun, "Quanto a pressão por permanência soma à curva de spawn por minuto." + NextRunPt);
            _pressureMax = F("StagePressureMax", 1f, 0f, 5f, "Cap of the stay pressure." + NextRun, "Teto da pressão por permanência." + NextRunPt);
            _perfEnterMs = F("PerfEnterMs", 33f, 10f, 200f, "Average frame time (ms) above which, for PerfEnterSeconds, the extra credit stops. Applies immediately.", "Tempo médio de frame (ms) acima do qual, por PerfEnterSeconds, o crédito extra para. Vale na hora.");
            _perfEnterSeconds = F("PerfEnterSeconds", 10f, 1f, 120f, "Seconds above PerfEnterMs to enter the reduced mode. Applies immediately.", "Segundos acima de PerfEnterMs para entrar no modo reduzido. Vale na hora.");
            _perfExitMs = F("PerfExitMs", 25f, 5f, 200f, "Average frame time (ms) below which, for PerfExitSeconds, the extra credit starts coming back. Must be below PerfEnterMs. Applies immediately.", "Tempo médio de frame (ms) abaixo do qual, por PerfExitSeconds, o crédito extra começa a voltar. Precisa ser menor que PerfEnterMs. Vale na hora.");
            _perfExitSeconds = F("PerfExitSeconds", 20f, 1f, 300f, "Seconds below PerfExitMs to leave the reduced mode. Applies immediately.", "Segundos abaixo de PerfExitMs para sair do modo reduzido. Vale na hora.");
            _perfRecovery = F("PerfRecoverySeconds", 30f, 1f, 300f, "Seconds for the extra credit to go from 0 back to 100% after the reduced mode. Applies immediately.", "Segundos para o crédito extra ir de 0 a 100% depois do modo reduzido. Vale na hora.");
            _swarmSeconds = F("SwarmPhaseSeconds", 45f, 0f, 600f, "Swarm phase: the director also uses cheap monsters (many small enemies). Each stage starts with it. 0 = off (director as in the game)." + NextRun, "Fase de enxame: o diretor também usa monstros baratos (muitos inimigos pequenos). Todo estágio começa nela. 0 = desliga (diretor como no jogo)." + NextRunPt);
            _heavySeconds = F("HeavyPhaseSeconds", 75f, 10f, 600f, "Heavy phase between swarms: director as in the game (expensive monsters and elites)." + NextRun, "Fase pesada entre os enxames: diretor como no jogo (monstros caros e elites)." + NextRunPt);
            _swarmWave = I("SwarmWaveSize", 10, 1, 30, "Monsters per wave during the swarm phase (game: 6)." + NextRun, "Monstros por onda na fase de enxame (jogo: 6)." + NextRunPt);
            _finalStage = I("FinalStage", 10, 5, 50, "Stage where the Primordial Teleporter opens the way to Commencement (multiple of 5; 5 = game). Before it, it leads to the next loop." + NextRun, "Estágio em que o Teleportador Primordial abre o caminho para a Commencement (múltiplo de 5; 5 = jogo). Antes dele, leva ao próximo loop." + NextRunPt);
            _blockObliteration = cfg.Bind("Tempest", "BlockObliterationUntilFinalStage", false, "Celestial Portal (Obliteration and A Moment, Whole) only appears from FinalStage on." + NextRun, "Portal Celeste (Obliteração e Um Momento, Inteiro) só aparece a partir do FinalStage." + NextRunPt);
            _blockVoidling = cfg.Bind("Tempest", "BlockVoidlingUntilFinalStage", false, "Deep Void portal (Voidling) only appears from FinalStage on." + NextRun, "Portal do Vazio Profundo (Voidling) só aparece a partir do FinalStage." + NextRunPt);
            _blockFalseSon = cfg.Bind("Tempest", "BlockFalseSonUntilFinalStage", false, "Path of the Colossus portal (False Son) only appears from FinalStage on." + NextRun, "Portal do Caminho do Colosso (Filho Falso) só aparece a partir do FinalStage." + NextRunPt);
            _blockSolus = cfg.Bind("Tempest", "BlockSolusUntilFinalStage", false, "Solus portals (Alloyed Collective) only appear from FinalStage on." + NextRun, "Portais do Solus (Alloyed Collective) só aparecem a partir do FinalStage." + NextRunPt);
            _blockChampRush = cfg.Bind("Tempest", "BlockChampRushUntilFinalStage", false, "Champ Rush portal only appears from FinalStage on." + NextRun, "Portal do Champ Rush só aparece a partir do FinalStage." + NextRunPt);
            _steps = cfg.Bind("Tempest", "DamageReductionSteps", "20,40,55,70,90,99",
                "Enemy damage reduction AND the same extra max health, in %, per stage cleared (1st value = stage 2; the last one repeats). Each value 0 to 99. Applies from the next stage.",
                "Redução de dano E o mesmo em vida máxima extra dos inimigos, em %, por estágio concluído (1º valor = estágio 2; o último se repete). Cada valor de 0 a 99. Vale a partir do próximo estágio.");
            _multiplierExponent = F("MultiplierReductionExponent", 0.5f, 0f, 2f,
                "With the item Multiplier on (x M), the damage that gets through the reduction is divided by M^this value (0.5 with x10: 20% becomes 75%). Also raises the extra max health. Stage 1 stays at 0; cap 99%. 0 = Multiplier doesn't change the reduction. The Multiplier value is read at each stage start." + NextRun,
                "Com o Multiplicador de itens ligado (x M), o dano que passa pela redução é dividido por M^este valor (0,5 com x10: 20% vira 75%). Também aumenta a vida máxima extra. Estágio 1 continua 0; teto 99%. 0 = o Multiplicador não muda a redução. O valor do Multiplicador é lido no início de cada estágio." + NextRunPt);

            _debug = cfg.Bind("Tempest", "DebugLog", false,
                "Validation log in LogOutput.log ([Tempest:debug]): stacks given to each enemy, health before/after the Tempest bonus, damage before/after the reduction, hits ignored by the protection and a director line every 10 s. Sampled (first 10 of each kind per stage, then 1 in 100). Applies immediately; keep it off for regular play.",
                "Log de validação no LogOutput.log ([Tempest:debug]): stacks dados a cada inimigo, vida antes/depois do bônus da Tormenta, dano antes/depois da redução, golpes ignorados pela proteção e uma linha do diretor a cada 10 s. Por amostragem (10 primeiros de cada tipo por estágio, depois 1 a cada 100). Vale na hora; deixe desligado no dia a dia.");
            _debugOn = _debug.Value;
            _debug.SettingChanged += (_, __) =>
            {
                _debugOn = _debug.Value;
                ResetDebug();
                Log.LogInfo($"[Tempest] DebugLog {(_debugOn ? "ligado" : "desligado")}");
                BodyHooks.Sync();
            };

            foreach (var k in new[] { _perfEnterMs, _perfEnterSeconds, _perfExitMs, _perfExitSeconds, _perfRecovery })
                k.Entry.SettingChanged += (_, __) => ConfigureGovernor();
            ConfigureGovernor();
            Loc.Add(FinalLockedToken, "<color=#a070e0>Tempest:</color> the Primordial Teleporter only opens on stage {0}. For now it leads to the next loop.",
                "<color=#a070e0>Tormenta:</color> o Teleportador Primordial só se abre no estágio {0}. Por enquanto ele leva ao próximo loop.");
        }

        private FloatKey F(string key, float def, float min, float max, string en, string pt)
        {
            return new FloatKey { Entry = Cfg.Bind("Tempest", key, def, en, pt), Default = def, Min = min, Max = max };
        }

        private IntKey I(string key, int def, int min, int max, string en, string pt)
        {
            return new IntKey { Entry = Cfg.Bind("Tempest", key, def, en, pt), Min = min, Max = max };
        }

        private FloatKey[] Curve(string axis, float perMinute, float perStage, float max, float maxCap, string en, string pt) => new[]
        {
            F(axis + "PerMinute", perMinute, 0f, 1f, $"Growth per minute of the run of {en}. Applies from the next run.", $"Crescimento por minuto da run de {pt}. Vale a partir da próxima run."),
            F(axis + "PerStage", perStage, 0f, 2f, $"Growth per stage cleared of {en} (continues in loops). Applies from the next run.", $"Crescimento por estágio concluído de {pt} (continua nos loops). Vale a partir da próxima run."),
            F(axis + "Max", max, 1f, maxCap, $"Cap of the {en} multiplier. Applies from the next run.", $"Teto do multiplicador de {pt}. Vale a partir da próxima run."),
        };

        // Valor validado; corrige o .cfg com um Warning por chave.
        private float Read(FloatKey k)
        {
            var v = TempestRules.Clamp(k.Entry.Value, k.Default, k.Min, k.Max, out var corrected);
            if (corrected)
            {
                Log.LogWarning($"Tempest.{k.Entry.Definition.Key}={k.Entry.Value} fora de {k.Min}..{k.Max}; usando {v}.");
                k.Entry.Value = v;
            }
            return v;
        }

        private int Read(IntKey k)
        {
            var v = TempestRules.Clamp(k.Entry.Value, k.Min, k.Max, out var corrected);
            if (corrected)
            {
                Log.LogWarning($"Tempest.{k.Entry.Definition.Key}={k.Entry.Value} fora de {k.Min}..{k.Max}; usando {v}.");
                k.Entry.Value = v;
            }
            return v;
        }

        private TempestCurve ReadCurve(FloatKey[] c) => new TempestCurve(Read(c[0]), Read(c[1]), Read(c[2]));

        private void ConfigureGovernor() =>
            _governor.Configure(Read(_perfEnterMs), Read(_perfEnterSeconds), Read(_perfExitMs), Read(_perfExitSeconds), Read(_perfRecovery));

        // ---- ciclo de vida ----

        protected override void OnEnable()
        {
            Current = this;
            if (PreferredInteractabilityField == null) Log.LogWarning("[Tempest] LunarTeleporterBaseState.preferredInteractability não encontrado; a alternância do Teleportador Primordial fica visível (o destino continua travado).");
            Track("Stage.onStageStartGlobal", () => Stage.onStageStartGlobal += OnStageStart);
            Track("SceneManager.activeSceneChanged", () => UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged);
            Track("CharacterBody.onBodyStartGlobal", () => CharacterBody.onBodyStartGlobal += OnBodyStart);
            Track("CharacterMaster.onStartGlobal", () => CharacterMaster.onStartGlobal += OnMasterStart);
            Track("RoR2Application.onFixedUpdate", () => RoR2Application.onFixedUpdate += OnFixedUpdate);
            Track("LunarTeleporter.Active.OnEnter", () => On.EntityStates.LunarTeleporter.Active.OnEnter += OnLunarActiveEnter);
            Track("LunarTeleporterBaseState.FixedUpdate", () => On.EntityStates.LunarTeleporter.LunarTeleporterBaseState.FixedUpdate += OnLunarFixedUpdate);
            Track("SceneExitController.Begin", () => On.RoR2.SceneExitController.Begin += OnSceneExitBegin);
            Track("TeleporterInteraction.Start", () => On.RoR2.TeleporterInteraction.Start += OnTeleporterStart);
            Track("PortalSpawner.Start", () => On.RoR2.PortalSpawner.Start += OnPortalSpawnerStart);
            Track("PortalSpawner.AttemptSpawnPortalServer", () => On.RoR2.PortalSpawner.AttemptSpawnPortalServer += OnPortalAttempt);
            Track("HealthComponent.TakeDamageProcess (IL)", () => IL.RoR2.HealthComponent.TakeDamageProcess += ILLensExecute);
            if (!_lensEffect)
                LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/CritGlassesVoidExecuteEffect").Completed += h => _lensEffect = h.Result;
            Track("BodyHooks", () => { BodyHooks.Tempest = this; BodyHooks.Sync(); });
        }

        protected override void OnDisable()
        {
            Stage.onStageStartGlobal -= OnStageStart;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnSceneChanged;
            CharacterBody.onBodyStartGlobal -= OnBodyStart;
            CharacterMaster.onStartGlobal -= OnMasterStart;
            RoR2Application.onFixedUpdate -= OnFixedUpdate;
            On.EntityStates.LunarTeleporter.Active.OnEnter -= OnLunarActiveEnter;
            On.EntityStates.LunarTeleporter.LunarTeleporterBaseState.FixedUpdate -= OnLunarFixedUpdate;
            On.RoR2.SceneExitController.Begin -= OnSceneExitBegin;
            On.RoR2.TeleporterInteraction.Start -= OnTeleporterStart;
            On.RoR2.PortalSpawner.Start -= OnPortalSpawnerStart;
            On.RoR2.PortalSpawner.AttemptSpawnPortalServer -= OnPortalAttempt;
            IL.RoR2.HealthComponent.TakeDamageProcess -= ILLensExecute;
            RestoreAll();
            _active = false;
            HealthHook = false;
            if (BodyHooks.Tempest == this) BodyHooks.Tempest = null;
            BodyHooks.Sync();
            if (Current == this) Current = null;
        }

        // Início e fim da run: devolve o que o mod mudou no jogo e esquece o snapshot.
        public override void ResetRunState()
        {
            if (_active) LogStageSummary();
            RestoreAll();
            _snapshotted = false;
            _active = false;
            _s = null;
            _reduction = 0f;
            _reductionStage = -1;
            HealthHook = false;
            _governor.Reset();
            ResetStage();
            BodyHooks.Sync();
        }

        private void RestoreAll()
        {
            RestoreDirectors();
            if (!_capSaved) return;
            _capSaved = false;
            var def = TeamCatalog.GetTeamDef(TeamIndex.Monster);
            if (def == null) return;
            if (def.softCharacterLimit != _capacity)
                Log.LogWarning($"[Tempest] softCharacterLimit mudou durante a run ({_capacity} -> {def.softCharacterLimit}); outro mod? Restaurando {_origLimit}.");
            def.softCharacterLimit = _origLimit;
            Log.LogInfo($"[Tempest] teto de monstros restaurado para {_origLimit}");
        }

        private void RestoreDirectors()
        {
            if (_directors.Count == 0) return;
            var list = CombatDirector.instancesList;
            for (var i = 0; i < list.Count; i++)
            {
                var d = list[i];
                if (!d || !_directors.TryGetValue(d.GetInstanceID(), out var st)) continue;
                d.eliteBias = st.EliteBias;
                d.skipSpawnIfTooCheap = st.Skip;
                d.maximumNumberToSpawnBeforeSkipping = st.MaxSpawns;
            }
            _directors.Clear();
        }

        private void ResetStage()
        {
            _directors.Clear();
            _protectedIds.Clear();
            _acc = _gate = _chargeSeconds = 0f;
            _noticeSent = _overCapWarned = _lockLogged = false;
            _sPeak = _sOverCap = _sSwitches = _sProtected = 0;
            _sCredit = 0;
            _sGateClosed = _sDegraded = 0f;
            _lastSwarm = _lastCharged = false;
            ResetDebug();
        }

        private void ResetDebug()
        {
            _dbgHealthBodies.Clear();
            _dbgSpawns = _dbgHits = _dbgRejected = _dbgHealth = _dbgLens = 0;
            _dbgNextTick = 0f;
        }

        // Amostragem do log de validação: os 10 primeiros de cada tipo no estágio e depois 1 a cada 100.
        private static bool Sample(ref int n) => ++n <= 10 || n % 100 == 0;

        // Primeiro uso da run no host: decide se a run é Tormenta e congela a config.
        private void EnsureSnapshot()
        {
            if (_snapshotted || !NetworkServer.active) return;
            var run = Run.instance;
            if (!run) return;
            _snapshotted = true;
            _active = Enabled && TempestDifficulty.IsSelected();
            if (!_active) return;

            var s = new Settings
            {
                Protection = Read(_protection),
                CapacityBase = Read(_capBase),
                CapacityPerPlayer = Read(_capPerPlayer),
                CapacityMax = Read(_capMax),
                Headroom = Read(_headroom),
                RampSeconds = Read(_ramp),
                CreditPerSecond = Read(_creditPerSecond),
                Spawn = ReadCurve(_spawn),
                Health = ReadCurve(_health),
                Damage = ReadCurve(_damage),
                Elite = ReadCurve(_elite),
                PressureStart = Read(_pressureStart),
                PressurePerMinute = Read(_pressurePerMinute),
                PressureMax = Read(_pressureMax),
                SwarmSeconds = Read(_swarmSeconds),
                HeavySeconds = Read(_heavySeconds),
                SwarmWaveSize = Read(_swarmWave),
                MultiplierExponent = Read(_multiplierExponent),
                LivesNormal = Read(_livesNormal),
                LivesElite = Read(_livesElite),
            };
            var finalStage = TempestRules.SanitizeFinalStage(Read(_finalStage), out var finalFixed);
            if (finalFixed)
            {
                Log.LogWarning($"Tempest.FinalStage={_finalStage.Entry.Value} não é múltiplo de 5; usando {finalStage}.");
                _finalStage.Entry.Value = finalStage;
            }
            s.FinalStage = finalStage;
            s.Steps = TempestRules.ParseSteps(_steps.Value, out var stepsFixed);
            if (stepsFixed) Log.LogWarning($"Tempest.DamageReductionSteps=\"{_steps.Value}\" corrigido para \"{string.Join(",", s.Steps)}\" (0..99, sem diminuir).");
            if (s.CapacityMax < s.CapacityBase) Log.LogWarning($"Tempest.CapacityMax={s.CapacityMax} menor que CapacityBase={s.CapacityBase}; o teto vale.");
            _s = s;

            _blockedCards.Clear();
            _blockMs = _blockObliteration.Value;
            if (_blockVoidling.Value) _blockedCards.UnionWith(VoidlingCards);
            if (_blockFalseSon.Value) _blockedCards.UnionWith(FalseSonCards);
            if (_blockSolus.Value) _blockedCards.UnionWith(SolusCards);
            if (_blockChampRush.Value) _blockedCards.UnionWith(ChampRushCards);

            _infestorIndex = MasterCatalog.FindMasterIndex("VoidInfestorMaster");
            _players = Math.Max(1, run.participatingPlayerCount);
            _capacity = TempestRules.Capacity(_players, s.CapacityBase, s.CapacityPerPlayer, s.CapacityMax);
            var team = TeamCatalog.GetTeamDef(TeamIndex.Monster);
            if (team != null && !_capSaved)
            {
                _origLimit = team.softCharacterLimit;
                _capSaved = true;
                team.softCharacterLimit = _capacity;
            }
            ConfigureGovernor();
            _governor.Reset();
            Log.LogInfo($"[Tempest] run da Tormenta: {_players} jogador(es) de referência, teto de monstros {_origLimit} -> {_capacity}, " +
                        $"proteção {s.Protection:0.#} s, crédito {s.CreditPerSecond:0.##}/s, enxame {s.SwarmSeconds:0}/{s.HeavySeconds:0} s (onda {s.SwarmWaveSize}), " +
                        $"redução [{string.Join(",", s.Steps)}]%, final no estágio {s.FinalStage}, finais bloqueados: {(_blockMs ? "Obliteração " : "")}{string.Join(" ", _blockedCards)}");
        }

        private float StageReduction(Run run)
        {
            if (!_active || _s == null) return 0f;
            if (_reductionStage != run.stageClearCount)
            {
                _reductionStage = run.stageClearCount;
                _reductionMultiplier = MultiplierFeature.ActiveValue;
                _reduction = TempestRules.Reduction(_reductionStage, _s.Steps, _reductionMultiplier, _s.MultiplierExponent);
            }
            return _reduction;
        }

        private bool FinalLockedNow()
        {
            var run = Run.instance;
            return _active && _s != null && run && TempestRules.FinalLocked(run.stageClearCount, _s.FinalStage);
        }

        // ---- estágio ----

        // Fecha o estágio anterior na troca de cena: antes do Start dos objetos da cena nova, ou seja, antes de o
        // SceneDirector popular os monstros iniciais (que nascem antes do Stage.onStageStartGlobal e contam no estágio novo).
        private void OnSceneChanged(UnityEngine.SceneManagement.Scene from, UnityEngine.SceneManagement.Scene to)
        {
            try
            {
                if (!_active) return;
                LogStageSummary();
                ResetStage();
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnStageStart(Stage stage)
        {
            try
            {
                HealthHook = Enabled && TempestHealthItem.Index != ItemIndex.None && TempestDifficulty.IsSelected();
                if (NetworkServer.active)
                {
                    EnsureSnapshot();
                    if (_active)
                    {
                        var run = Run.instance;
                        var r = StageReduction(run);
                        Log.LogInfo($"[Tempest] estágio {run.stageClearCount + 1} ({(stage && stage.sceneDef ? stage.sceneDef.cachedName : "?")}): redução de dano e vida +{r * 100f:0.#}% (Multiplicador x{_reductionMultiplier}), " +
                                    $"final {(FinalLockedNow() ? "fechado" : "aberto")}");
                    }
                }
                BodyHooks.Sync();
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void LogStageSummary()
        {
            if (_sPeak == 0 && _sProtected == 0) return;
            Log.LogInfo($"[Tempest] resumo do estágio: pico {_sPeak}/{_capacity} monstros, teto excedido {_sOverCap}x, {_sProtected} protegidos, " +
                        $"crédito extra {_sCredit:0}, portão fechado {_sGateClosed:0} s, degradado {_sDegraded:0} s, {_sSwitches} trocas de fase");
        }

        // ---- proteção ----

        private void OnBodyStart(CharacterBody body)
        {
            try
            {
                if (!NetworkServer.active || !body) return;
                EnsureSnapshot();
                if (!_active || _s.Protection <= 0f || !IsEnemy(body)) return;
                var master = body.master;
                var id = master ? master.GetInstanceID() : body.GetInstanceID();
                // Mesmo master: só renova se ele gastou um Dio's desde a última proteção (renasceu); troca de corpo não renova.
                var spent = master && master.inventory ? master.inventory.GetItemCountPermanent(RoR2Content.Items.ExtraLifeConsumed) : 0;
                if (_protectedIds.TryGetValue(id, out var last) && last >= spent) return;
                _protectedIds[id] = spent;
                body.AddTimedBuff(DLC2Content.Buffs.HiddenRejectAllDamage, _s.Protection);
                body.AddTimedBuff(RoR2Content.Buffs.Immune, _s.Protection); // só o visual: brilho dourado do jogo (matImmune)
                _sProtected++;
                if (_debugOn && (_sProtected <= 10 || _sProtected % 100 == 0))
                    Log.LogInfo($"[Tempest:debug] proteção #{_sProtected}: {body.name} invulnerável por {_s.Protection:0.#} s");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Time inimigo, sem jogador no controle e fora as almas do Artefato da Alma (morrem num golpe por design).
        private static bool IsEnemy(CharacterBody body)
        {
            var tc = body.teamComponent;
            if (!tc) return false;
            var team = tc.teamIndex;
            if (team != TeamIndex.Monster && team != TeamIndex.Lunar && team != TeamIndex.Void) return false;
            if (body.isPlayerControlled) return false;
            var master = body.master;
            return !master || !IsSoul(master);
        }

        private static bool IsSoul(CharacterMaster master)
        {
            var soul = GlobalEventManager.CommonAssets.wispSoulMasterPrefabMasterComponent;
            return soul && master.masterIndex == soul.masterIndex;
        }

        // ---- vida e dano ----

        private void OnMasterStart(CharacterMaster master)
        {
            try
            {
                if (!NetworkServer.active || !master) return;
                EnsureSnapshot();
                if (!_active || master.playerCharacterMasterController) return;
                var team = master.teamIndex;
                if (team != TeamIndex.Monster && team != TeamIndex.Lunar && team != TeamIndex.Void) return;
                if (IsSoul(master)) return;
                var inv = master.inventory;
                if (!inv) return;
                var run = Run.instance;
                var minutes = run.GetRunStopwatch() / 60f;
                // Vida e dano acompanham o Multiplicador de itens (o mesmo fator da redução), como a redução só a partir do
                // estágio 2: no 1º o time ainda não tem itens multiplicados (teste da 0.11.1: +220% no minuto 0 = derrota em 1 min).
                var boost = run.stageClearCount > 0 ? TempestRules.MultiplierFactor(MultiplierFeature.ActiveValue, _s.MultiplierExponent) : 1f;
                var hp = TempestRules.BoostStacks(_s.Health.Evaluate(minutes, run.stageClearCount) * boost);
                var dmg = TempestRules.BoostStacks(_s.Damage.Evaluate(minutes, run.stageClearCount) * boost);
                // Invocações (minas, totens, ouriços do elite...) e temporários não revivem: morrer é o fim do trabalho deles.
                // O Void Infestor se mata ao entrar no alvo (Infest.healthComponent.Suicide): com Dio's, infestaria de novo.
                var summon = master.minionOwnership && master.minionOwnership.ownerMaster || master.GetComponent<MasterSuicideOnTimer>()
                             || master.masterIndex == _infestorIndex;
                var lives = summon ? 0 : TempestRules.ExtraLives(IsBoss(master), IsElite(inv), _s.LivesNormal, _s.LivesElite);
                var vigor = TempestRules.HealthStacks(StageReduction(run));
                if (hp > 0) inv.GiveItemPermanent(RoR2Content.Items.BoostHp, hp);
                if (dmg > 0) inv.GiveItemPermanent(RoR2Content.Items.BoostDamage, dmg);
                var idx = TempestHealthItem.Index;
                if (vigor > 0 && idx != ItemIndex.None) inv.GiveItemPermanent(idx, vigor);
                if (lives > 0) inv.GiveItemPermanent(RoR2Content.Items.ExtraLife, lives);
                if (_debugOn && Sample(ref _dbgSpawns))
                    Log.LogInfo($"[Tempest:debug] spawn #{_dbgSpawns} {master.name} (estágio {run.stageClearCount + 1}, {minutes:0.0} min): " +
                                $"BoostHp +{hp} (+{hp * 10}% vida), BoostDamage +{dmg} (+{dmg * 10}% dano), Vigor +{vigor} (+{vigor}% vida, redução {vigor}%), Dio's +{lives} | " +
                                $"no inventário: BoostHp {inv.GetItemCountPermanent(RoR2Content.Items.BoostHp)}, BoostDamage {inv.GetItemCountPermanent(RoR2Content.Items.BoostDamage)}, Vigor {(idx != ItemIndex.None ? inv.GetItemCountPermanent(idx) : 0)}");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Chefe: do teleportador/evento (isBoss) ou corpo campeão (Beetle Queen, Titan... mesmo spawnado como monstro comum).
        private static bool IsBoss(CharacterMaster master)
        {
            if (master.isBoss) return true;
            var body = master.bodyPrefab ? master.bodyPrefab.GetComponent<CharacterBody>() : null;
            return body && body.isChampion;
        }

        // Elite: o diretor põe o equipamento do aspecto no inventário no spawn, antes do Start do master.
        private static bool IsElite(Inventory inv)
        {
            var def = EquipmentCatalog.GetEquipmentDef(inv.currentEquipmentIndex);
            return def && def.passiveBuffDef && def.passiveBuffDef.isElite;
        }

        // Passo 3 do BodyHooks.TakeDamageProcess (host): redução de dano do monstro vítima.
        internal void ReduceDamage(HealthComponent self, DamageInfo damageInfo)
        {
            try
            {
                if (!_active) return;
                _lensCount = 0; // o IL grava durante o orig deste golpe
                if (!self || damageInfo == null || damageInfo.damage <= 0f) return;
                var body = self.body;
                if (!body || !IsEnemy(body)) return;
                if (body.HasBuff(DLC2Content.Buffs.HiddenRejectAllDamage))
                {
                    // O jogo sai do TakeDamageProcess sem marcar o golpe como rejeitado, e quem atacou roda o OnHitEnemy
                    // mesmo assim: todos os procs dos itens disparavam em inimigo imune (teste da 0.11.2: 71 mil golpes num
                    // estágio, travadas de 1,8-4 s com ~3.300 itens). Rejeitado, o OnHitEnemy não processa procs.
                    damageInfo.rejected = true;
                    if (_debugOn && Sample(ref _dbgRejected))
                        Log.LogInfo($"[Tempest:debug] golpe #{_dbgRejected} ignorado pela proteção: {body.name} ({damageInfo.damage:0.#} de dano de {(damageInfo.attacker ? damageInfo.attacker.name : "?")})");
                    return; // o jogo rejeita no topo do TakeDamageProcess; não reduz o que não vai passar
                }
                if (_reduction <= 0f) return;
                var before = damageInfo.damage;
                damageInfo.damage *= 1f - _reduction;
                if (_debugOn && Sample(ref _dbgHits))
                    Log.LogInfo($"[Tempest:debug] golpe #{_dbgHits} em {body.name}: {before:0.#} -> {damageInfo.damage:0.#} (redução {_reduction * 100f:0}%, " +
                                $"vida {self.health:0}/{body.maxHealth:0}, de {(damageInfo.attacker ? damageInfo.attacker.name : "?")})");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // As Lentes não executam na Tormenta: no TakeDamageProcess, a contagem do item que o jogo lê vira 0. O jogo só chega
        // nessa leitura com atacante com inventário, alvo que não é chefe nem imune a execução e golpe não rejeitado;
        // a contagem fica guardada para o AfterDamage. Corpo mudou = loga e as Lentes executam como no jogo.
        private void ILLensExecute(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(x => x.MatchLdsfld(typeof(DLC1Content.Items), nameof(DLC1Content.Items.CritGlassesVoid)))
                || !c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))))
            {
                Log.LogError("[Tempest] Lentes do Vidente Perdido: leitura do item no TakeDamageProcess não encontrada; IL hook não aplicado (as Lentes executam como no jogo).");
                return;
            }
            c.EmitDelegate<Func<int, int>>(LensCount);
        }

        private int LensCount(int count)
        {
            if (!_active) return count;
            _lensCount = count;
            return 0;
        }

        // Passo 6 do BodyHooks.TakeDamageProcess (host): o sorteio do jogo (0,5% por cópia x coeficiente de proc), mas o
        // acerto tira 90% da vida (e do escudo) restante em vez de matar.
        internal void AfterDamage(HealthComponent self, DamageInfo damageInfo)
        {
            var n = _lensCount;
            _lensCount = 0;
            if (n <= 0) return;
            try
            {
                if (!self || !self.alive || damageInfo.rejected) return;
                var attacker = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null; // só com Lentes
                if (!Util.CheckRoll(n * 0.5f * damageInfo.procCoefficient, attacker ? attacker.master : null)) return;
                var before = self.health;
                self.Networkhealth = Mathf.Max(1f, before * (1f - LensHealthCut));
                if (self.shield > 0f) self.Networkshield = self.shield * (1f - LensHealthCut);
                var body = self.body;
                if (_lensEffect && body)
                    EffectManager.SpawnEffect(_lensEffect, new EffectData { origin = body.corePosition, scale = body.radius }, transmit: true);
                if (_debugOn && Sample(ref _dbgLens))
                    Log.LogInfo($"[Tempest:debug] Lentes #{_dbgLens} ({n} cópia(s)) em {(body ? body.name : "?")}: vida {before:0} -> {self.health:0}");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Passo 0 do BodyHooks.RecalculateStats, com DebugLog: uma linha por corpo (amostrada).
        internal void LogHealth(CharacterBody body, float before, int stacks)
        {
            if (!_debugOn || !body || !_dbgHealthBodies.Add(body.GetInstanceID()) || !Sample(ref _dbgHealth)) return;
            Log.LogInfo($"[Tempest:debug] vida #{_dbgHealth} {body.name}: {before:0} -> {body.maxHealth:0} (Vigor {stacks} = x{1f + stacks / 100f:0.00}, " +
                        $"BoostHp {(body.inventory ? body.inventory.GetItemCountPermanent(RoR2Content.Items.BoostHp) : 0)}, nível {body.level:0}, " +
                        $"{(NetworkServer.active ? "host" : "cliente")})");
        }

        // ---- diretor (tick de 1 s, host) ----

        private void OnFixedUpdate()
        {
            if (!_active || !NetworkServer.active) return;
            _acc += Time.fixedDeltaTime;
            if (_acc < 1f) return;
            var dt = _acc;
            _acc = 0f;
            try { Tick(dt); }
            catch (Exception e) { Fail(e); }
        }

        private void Tick(float dt)
        {
            var run = Run.instance;
            var stage = Stage.instance;
            if (!run || !stage || run.isRunStopwatchPaused) return; // entre estágios / bazar: relógio da run parado

            var tp = TeleporterInteraction.instance;
            var charging = tp && tp.isCharging;
            var charged = tp && tp.isCharged;
            if (charging) _chargeSeconds += dt;
            var stageSeconds = stage.entryTime.timeSince;
            var minutes = run.GetRunStopwatch() / 60f;
            var stages = run.stageClearCount;

            _alive = TeamComponent.GetTeamMembers(TeamIndex.Monster).Count;
            if (_governor.Update(Diag.FrameMsAvg, dt))
                Log.LogInfo($"[Tempest] desempenho: {_governor.State} (frame médio {Diag.FrameMsAvg:0.0} ms, {_alive} monstros)");
            var perf = _governor.Level;
            if (perf < 1f) _sDegraded += dt;

            if (_alive > _sPeak) _sPeak = _alive;
            if (_alive > _capacity)
            {
                _sOverCap++;
                if (!_overCapWarned)
                {
                    _overCapWarned = true;
                    Log.LogWarning($"[Tempest] {_alive} monstros acima do teto {_capacity} (chefe/evento ou outro mod); nada é removido. Pico: {Diag.MonsterBreakdown(6)}");
                }
            }
            _gate = TempestRules.Gate(_gate, _capacity, _alive, _s.Headroom, _s.RampSeconds, dt);
            if (_gate <= 0f) _sGateClosed += dt;

            _lastPressure = charged ? 0f : TempestRules.Pressure(stageSeconds - _chargeSeconds, _s.PressureStart, _s.PressurePerMinute, _s.PressureMax);
            _lastSpawn = _s.Spawn.Evaluate(minutes, stages);
            _lastHealth = _s.Health.Evaluate(minutes, stages);
            _lastDamage = _s.Damage.Evaluate(minutes, stages);
            _lastElite = _s.Elite.Evaluate(minutes, stages);
            _lastCharged = charged;
            var credit = charged ? 0f : TempestRules.Credit(_s.CreditPerSecond, run.compensatedDifficultyCoefficient, _players, _lastSpawn + _lastPressure, _gate, perf) * dt;
            var swarm = !charged && perf >= 1f && TempestRules.IsSwarm(stageSeconds, _s.SwarmSeconds, _s.HeavySeconds);
            if (swarm != _lastSwarm)
            {
                _lastSwarm = swarm;
                _sSwitches++; // uma vez por troca, não por diretor
            }
            var eliteBias = TempestRules.EliteBias(_lastElite);

            var list = CombatDirector.instancesList;
            for (var i = 0; i < list.Count; i++) // for: sem enumerador
            {
                var d = list[i];
                if (!d || d.teamIndex != TeamIndex.Monster) continue;
                var st = State(d, tp);
                d.eliteBias = st.EliteBias * eliteBias;
                if (!st.Main) continue;
                if (credit > 0f && d.enabled)
                {
                    d.monsterCredit += credit;
                    _sCredit += credit;
                }
                if (st.Swarm != swarm)
                {
                    st.Swarm = swarm;
                    d.skipSpawnIfTooCheap = !swarm && st.Skip;
                    d.maximumNumberToSpawnBeforeSkipping = swarm ? _s.SwarmWaveSize : st.MaxSpawns;
                    if (Cfg.VerboseLogging || _debugOn) Log.LogInfo($"[Tempest:debug] {d.name}: fase {(swarm ? "enxame" : "pesada")}");
                }
            }

            if (_debugOn && stageSeconds >= _dbgNextTick)
            {
                _dbgNextTick = stageSeconds + 10f;
                Log.LogInfo($"[Tempest:debug] diretor t={stageSeconds:0}s: monstros {_alive}/{_capacity}, portão {_gate:0.00}, desempenho {_governor.State} {perf:0.00} (frame {Diag.FrameMsAvg:0.0} ms), " +
                            $"crédito extra {credit / dt:0.00}/s (total {_sCredit:0}), fase {(swarm ? "enxame" : "pesada")}, spawn x{_lastSpawn:0.00} + pressão {_lastPressure:0.00}, " +
                            $"vida x{_lastHealth:0.00}, dano x{_lastDamage:0.00}, elite x{_lastElite:0.00} (eliteBias {eliteBias:0.00}), redução {_reduction * 100f:0}%, " +
                            $"carga {(charging ? "sim" : charged ? "concluída" : "não")}");
            }
        }

        // Diretor visto pela primeira vez no estágio: guarda os valores originais (alocação 1x por diretor).
        private DirectorState State(CombatDirector d, TeleporterInteraction tp)
        {
            var id = d.GetInstanceID();
            if (_directors.TryGetValue(id, out var st)) return st;
            st = new DirectorState
            {
                EliteBias = d.eliteBias,
                Skip = d.skipSpawnIfTooCheap,
                MaxSpawns = d.maximumNumberToSpawnBeforeSkipping,
                Main = Directors.IsMainMonster(d) && (!tp || (d != tp.bonusDirector && d != tp.bossDirector)),
            };
            _directors[id] = st;
            return st;
        }

        // ---- fase final ----

        private void OnLunarActiveEnter(On.EntityStates.LunarTeleporter.Active.orig_OnEnter orig, EntityStates.LunarTeleporter.Active self)
        {
            orig(self);
            try
            {
                if (!NetworkServer.active || !FinalLockedNow()) return;
                self.outer.SetNextState(new EntityStates.LunarTeleporter.Idle()); // Idle: destino = próximo estágio (loop)
                if (!_lockLogged)
                {
                    _lockLogged = true;
                    Log.LogInfo($"[Tempest] Teleportador Primordial preso no próximo loop até o estágio {_s.FinalStage}");
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnLunarFixedUpdate(On.EntityStates.LunarTeleporter.LunarTeleporterBaseState.orig_FixedUpdate orig, EntityStates.LunarTeleporter.LunarTeleporterBaseState self)
        {
            try
            {
                // Antes do orig: ele já grava Disabled na interação (o valor replica). Escrever depois dele trocava
                // Available -> Disabled em todo tick e reenviava a SyncVar todo tick.
                if (NetworkServer.active && FinalLockedNow())
                {
                    PreferredInteractabilityField?.SetValue(self, DisabledBoxed);
                    if (!_noticeSent)
                    {
                        _noticeSent = true;
                        Loc.Broadcast(FinalLockedToken, _s.FinalStage.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(self);
        }

        private void OnSceneExitBegin(On.RoR2.SceneExitController.orig_Begin orig, SceneExitController self)
        {
            try
            {
                var tp = TeleporterInteraction.instance;
                if (NetworkServer.active && self && tp && tp.sceneExitController == self && !self.useRunNextStageScene && FinalLockedNow())
                {
                    self.useRunNextStageScene = true;
                    Log.LogWarning("[Tempest] destino do teleportador mudado para a Commencement antes do FinalStage (outro mod?); voltando para o próximo estágio.");
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(self);
        }

        private void OnTeleporterStart(On.RoR2.TeleporterInteraction.orig_Start orig, TeleporterInteraction self)
        {
            orig(self);
            try
            {
                if (!NetworkServer.active || !self) return;
                EnsureSnapshot();
                if (_blockMs && self.shouldAttemptToSpawnMSPortal && FinalLockedNow())
                {
                    self.shouldAttemptToSpawnMSPortal = false;
                    Log.LogInfo($"[Tempest] Portal Celeste bloqueado até o estágio {_s.FinalStage}");
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnPortalSpawnerStart(On.RoR2.PortalSpawner.orig_Start orig, PortalSpawner self)
        {
            orig(self);
            try
            {
                if (self && self.WillSpawn && PortalBlocked(self))
                {
                    self.WillSpawn = false;
                    Log.LogInfo($"[Tempest] portal {self.PortalSpawnCard.name} bloqueado até o estágio {_s.FinalStage}");
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Cobre o SpawnPortalNow (força WillSpawn = true) e o fim do teleportador; o orig roda e devolve false.
        private bool OnPortalAttempt(On.RoR2.PortalSpawner.orig_AttemptSpawnPortalServer orig, PortalSpawner self)
        {
            try
            {
                if (self && PortalBlocked(self)) self.WillSpawn = false;
            }
            catch (Exception e)
            {
                Fail(e);
            }
            return orig(self);
        }

        private bool PortalBlocked(PortalSpawner p)
        {
            if (!NetworkServer.active) return false;
            EnsureSnapshot();
            if (_blockedCards.Count == 0 || !FinalLockedNow()) return false;
            var card = p.PortalSpawnCard;
            return card && _blockedCards.Contains(card.name);
        }

        // ---- tela F8 (host) ----

        internal static bool AppendSummary(StringBuilder sb, bool pt)
        {
            var f = Current;
            if (f == null || !f._active || f._s == null || !NetworkServer.active) return false;
            var credit = f._lastCharged ? (pt ? "parado (teleportador)" : "stopped (teleporter)")
                : f._governor.Level < 1f ? (pt ? "degradado" : "degraded")
                : f._gate < 1f ? (pt ? "limitado" : "limited")
                : "normal";
            sb.Append(pt ? "Monstros " : "Monsters ").Append(f._alive).Append('/').Append(f._capacity)
              .Append(pt ? "   crédito extra " : "   extra credit ").Append(credit)
              .Append(pt ? "   fase " : "   phase ").Append(f._lastSwarm ? (pt ? "enxame" : "swarm") : (pt ? "pesada" : "heavy")).Append('\n')
              .Append("spawn x").Append(f._lastSpawn.ToString("0.00")).Append(pt ? "   vida x" : "   health x").Append(f._lastHealth.ToString("0.00"))
              .Append(pt ? "   dano x" : "   damage x").Append(f._lastDamage.ToString("0.00")).Append("   elite x").Append(f._lastElite.ToString("0.00"))
              .Append(pt ? "   redução " : "   reduction ").Append((f._reduction * 100f).ToString("0")).Append('%')
              .Append(pt ? "   pressão +" : "   pressure +").Append(f._lastPressure.ToString("0.00"));
            return true;
        }
    }
}
