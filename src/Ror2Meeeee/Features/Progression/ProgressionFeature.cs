using System;
using System.IO;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Progression;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Dificuldade que cresce entre runs, só no host. Durante a run só conta (barato, sem alocar).
    // No game over a principal copia os contadores e um worker calcula a nota, o próximo nível,
    // grava o estado e o CSV e monta o resumo; o chat volta para a principal por RoR2Application.onNextUpdate.
    // Efeito: CombatDirector.creditMultiplier escalado ANTES do Awake do jogo (que o copia para as ondas de
    // crédito), então compõe com o Difficulty.Factor e a Horda em qualquer ordem de hook.
    public sealed class ProgressionFeature : Feature
    {
        private static readonly object FileLock = new object();

        private readonly ConfigEntry<float> _step;
        private readonly ConfigEntry<float> _maxMultiplier;
        private readonly ConfigEntry<float> _raiseAt;
        private readonly ConfigEntry<float> _parCharge;
        private readonly ConfigEntry<float> _parDeaths;
        private readonly ConfigEntry<float> _parDamage;
        private readonly ConfigEntry<float> _parKills;
        private readonly ConfigEntry<bool> _showInChat;
        private readonly string _statePath = System.IO.Path.Combine(Paths.ConfigPath, "ror2meeeee-progress.txt");
        private readonly string _csvPath = System.IO.Path.Combine(Paths.BepInExRootPath, "ror2meeeee-runs.csv");

        // Escritos pelo worker, lidos na principal: sempre via Volatile.
        private int _level;
        private int _runs;

        private float _runMultiplier = 1f; // fixado no início da run
        private bool _gameOver;            // um resultado por run

        // Contadores da run (só a principal mexe).
        private float _stageStart;
        private bool _stageCharged;
        private int _chargedStages;
        private double _chargeSeconds;
        private int _deaths;
        private int _kills;
        private double _healthBarsLost;

        public ProgressionFeature(Cfg cfg, ManualLogSource log) : base("Progression", cfg, log,
            "Progressão entre runs: no fim de cada run o grupo recebe uma nota (velocidade, mortes, dano sofrido, abates), o resumo aparece no chat e o diretor fica mais forte na próxima run se vocês foram bem (vitória com nota alta sobe um nível; derrota desce um). Comandos: ror2meeeee_progress e ror2meeeee_progress_reset.",
            enabledByDefault: true)
        {
            _step = cfg.Bind("Progression", "Step", 0.15f, "Quanto o diretor ganha de orçamento de spawn por nível (0.15 = +15% de inimigos por nível).");
            _maxMultiplier = cfg.Bind("Progression", "MaxMultiplier", 3f, "Limite do fortalecimento (3 = no máximo 3x mais orçamento de spawn). Também define o nível máximo.");
            _raiseAt = cfg.Bind("Progression", "RaiseAt", 0.5f, "Nota mínima (0 a 1) que uma vitória precisa para subir de nível. 0.5 = desempenho médio. Derrota sempre desce um nível.");
            _parCharge = cfg.Bind("Progression", "ParChargeSeconds", 240f, "Referência de velocidade: segundos do início do estágio até carregar o teleportador considerados normais. Mais rápido que isso aumenta a nota.");
            _parDeaths = cfg.Bind("Progression", "ParDeathsPerPlayer", 1f, "Referência de mortes por jogador na run consideradas normais. Menos mortes aumenta a nota.");
            _parDamage = cfg.Bind("Progression", "ParHealthBarsPerMinute", 0.5f, "Referência de dano sofrido por jogador por minuto, em barras de vida inteiras (0.5 = meia barra por minuto). Menos dano aumenta a nota.");
            _parKills = cfg.Bind("Progression", "ParKillsPerMinute", 20f, "Referência de abates por minuto do time todo. Mais abates aumenta a nota.");
            _showInChat = cfg.Bind("Progression", "ShowInChat", true, "Mostra o resumo da run no chat para todos. Desligado: só no log.");
            LoadState();
        }

        public int Level => Volatile.Read(ref _level);
        public int Runs => Volatile.Read(ref _runs);
        public float CurrentMultiplier => ProgressionLadder.Multiplier(Level, Rules());

        private LadderRules Rules() => new LadderRules(_raiseAt.Value, _step.Value, _maxMultiplier.Value);

        // Início e fim da run: zera contadores e fixa o multiplicador da próxima run.
        public override void ResetRunState()
        {
            _gameOver = false;
            _stageStart = 0f;
            _stageCharged = false;
            _chargedStages = 0;
            _chargeSeconds = 0;
            _deaths = 0;
            _kills = 0;
            _healthBarsLost = 0;
            _runMultiplier = CurrentMultiplier;
        }

        protected override void OnEnable()
        {
            _runMultiplier = CurrentMultiplier;
            Track("CombatDirector.Awake", () => On.RoR2.CombatDirector.Awake += OnDirectorAwake);
            Track("Stage.onServerStageBegin", () => Stage.onServerStageBegin += OnStageBegin);
            Track("TeleporterInteraction.onTeleporterChargedGlobal", () => TeleporterInteraction.onTeleporterChargedGlobal += OnTeleporterCharged);
            Track("GlobalEventManager.onServerDamageDealt", () => GlobalEventManager.onServerDamageDealt += OnDamageDealt);
            Track("GlobalEventManager.onCharacterDeathServer", () => GlobalEventManager.onCharacterDeathServer += OnCharacterDeath);
            Track("Run.onServerGameOver", () => Run.onServerGameOver += OnGameOver);
            Log.LogInfo($"[Progression] nível {Level} ({Runs} runs) => orçamento de spawn x{_runMultiplier:F2}");
        }

        protected override void OnDisable()
        {
            On.RoR2.CombatDirector.Awake -= OnDirectorAwake;
            Stage.onServerStageBegin -= OnStageBegin;
            TeleporterInteraction.onTeleporterChargedGlobal -= OnTeleporterCharged;
            GlobalEventManager.onServerDamageDealt -= OnDamageDealt;
            GlobalEventManager.onCharacterDeathServer -= OnCharacterDeath;
            Run.onServerGameOver -= OnGameOver;
        }

        // ---- efeito ----

        private void OnDirectorAwake(On.RoR2.CombatDirector.orig_Awake orig, CombatDirector self)
        {
            try
            {
                if (NetworkServer.active && _runMultiplier != 1f && self.teamIndex == TeamIndex.Monster)
                    self.creditMultiplier *= _runMultiplier;
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(self);
        }

        // ---- contadores (caminho quente: sem alocação) ----

        private void OnStageBegin(Stage stage)
        {
            try
            {
                var run = Run.instance;
                if (!NetworkServer.active || !run) return;
                _stageStart = run.GetRunStopwatch();
                _stageCharged = false;
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnTeleporterCharged(TeleporterInteraction tp)
        {
            try
            {
                var run = Run.instance;
                if (!NetworkServer.active || _stageCharged || !run) return;
                _stageCharged = true;
                _chargedStages++;
                _chargeSeconds += Math.Max(0f, run.GetRunStopwatch() - _stageStart);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnDamageDealt(DamageReport report)
        {
            try
            {
                if (report.victimTeamIndex != TeamIndex.Player) return; // filtro barato primeiro: a maioria é dano em monstro
                var body = report.victimBody;
                if (!body || !body.isPlayerControlled) return;
                var full = report.victim ? report.victim.fullHealth : 0f;
                if (full > 0f) _healthBarsLost += report.damageDealt / full;
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnCharacterDeath(DamageReport report)
        {
            try
            {
                if (!NetworkServer.active) return;
                var team = report.victimTeamIndex;
                if (team == TeamIndex.Monster || team == TeamIndex.Lunar || team == TeamIndex.Void) _kills++;
                else if (team == TeamIndex.Player && report.victimBody && report.victimBody.isPlayerControlled) _deaths++;
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // ---- fim da run: cópia na principal, cálculo no worker ----

        private void OnGameOver(Run run, GameEndingDef ending)
        {
            try
            {
                if (!NetworkServer.active || _gameOver || !run) return;
                _gameOver = true;
                var stats = new RunStats(run.participatingPlayerCount, run.GetRunStopwatch(), _chargedStages, _chargeSeconds,
                    _deaths, _kills, _healthBarsLost, ending && ending.isWin);
                var pars = new ScorePars(_parCharge.Value, _parDeaths.Value, _parDamage.Value, _parKills.Value);
                var rules = Rules();
                var chat = _showInChat.Value;
                Plugin.RunInBackground(() => Finish(stats, pars, rules, chat)); // fechando o jogo: grava na hora
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Worker: sem API do Unity. O que precisa da principal (chat) é agendado em onNextUpdate,
        // que o jogo consome com Interlocked.Exchange (seguro a partir de outra thread).
        private void Finish(RunStats stats, ScorePars pars, LadderRules rules, bool chat)
        {
            try
            {
                var score = RunScore.Compute(stats, pars);
                int oldLevel, newLevel, runs;
                lock (FileLock)
                {
                    oldLevel = Volatile.Read(ref _level);
                    newLevel = ProgressionLadder.Next(oldLevel, score.Total, stats.Won, rules);
                    runs = Volatile.Read(ref _runs) + 1;
                    WriteState(newLevel, runs);
                    AppendCsv(stats, score, oldLevel, newLevel);
                    Volatile.Write(ref _level, newLevel);
                    Volatile.Write(ref _runs, runs);
                }
                var summary = ProgressionLadder.Summary(stats, score, oldLevel, newLevel, ProgressionLadder.Multiplier(newLevel, rules));
                Log.LogInfo("[Progression] " + summary.Replace('\n', ' '));
                if (chat) RoR2Application.onNextUpdate += () => PostChat(summary);
            }
            catch (Exception e)
            {
                Log.LogError($"Progression: falha ao fechar a run: {e}");
            }
        }

        private void PostChat(string summary)
        {
            try
            {
                if (NetworkServer.active) Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = summary });
            }
            catch (Exception e)
            {
                Log.LogError($"Progression: falha ao postar no chat: {e.Message}");
            }
        }

        // ---- estado em disco ----

        // Uma vez, no Awake: arquivo de poucos bytes.
        private void LoadState()
        {
            try
            {
                if (!File.Exists(_statePath)) return;
                ProgressionLadder.Parse(File.ReadAllText(_statePath), out var level, out var runs);
                _level = level;
                _runs = runs;
            }
            catch (Exception e)
            {
                Log.LogWarning($"Progression: estado ilegível ({e.Message}); começando do nível 0.");
            }
        }

        // Comando de console; grava fora da principal.
        public void Reset()
        {
            Plugin.RunInBackground(() =>
            {
                try
                {
                    lock (FileLock)
                    {
                        WriteState(0, Volatile.Read(ref _runs));
                        Volatile.Write(ref _level, 0);
                    }
                }
                catch (Exception e)
                {
                    Log.LogError($"Progression: falha ao zerar: {e.Message}");
                }
            });
        }

        private void WriteState(int level, int runs) => File.WriteAllText(_statePath, ProgressionLadder.Serialize(level, runs));

        private void AppendCsv(RunStats s, RunScoreResult score, int oldLevel, int newLevel)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder();
            if (!File.Exists(_csvPath))
                sb.Append("utc,won,players,minutes,charged_stages,avg_charge_s,deaths,kills,health_bars_lost,score_speed,score_survival,score_damage,score_kills,score_total,level_before,level_after\n");
            sb.Append(DateTime.UtcNow.ToString("o", inv)).Append(',')
              .Append(s.Won ? "true" : "false").Append(',')
              .Append(s.Players.ToString(inv)).Append(',')
              .Append((s.Seconds / 60.0).ToString("F2", inv)).Append(',')
              .Append(s.ChargedStages.ToString(inv)).Append(',')
              .Append((s.ChargedStages > 0 ? s.ChargeSeconds / s.ChargedStages : 0).ToString("F1", inv)).Append(',')
              .Append(s.Deaths.ToString(inv)).Append(',')
              .Append(s.Kills.ToString(inv)).Append(',')
              .Append(s.HealthBarsLost.ToString("F2", inv)).Append(',')
              .Append(score.Speed.ToString("F3", inv)).Append(',')
              .Append(score.Survival.ToString("F3", inv)).Append(',')
              .Append(score.Damage.ToString("F3", inv)).Append(',')
              .Append(score.Kills.ToString("F3", inv)).Append(',')
              .Append(score.Total.ToString("F3", inv)).Append(',')
              .Append(oldLevel.ToString(inv)).Append(',')
              .Append(newLevel.ToString(inv)).Append('\n');
            File.AppendAllText(_csvPath, sb.ToString());
        }
    }
}
