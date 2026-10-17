using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using BepInEx.Logging;
using RoR2;
using UnityEngine;

namespace Ror2Meeeee
{
    // Telemetria sempre ligada e barata, para achar problema cedo (independe do Probe):
    //  - resumo por estágio (duração, pior frame, abates, monstros, ouro/crédito, exceções, erros do jogo);
    //  - amostra de monstros vivos a cada 0,5 s (sem alocação);
    //  - vigia de travada (thread de fundo; o Unity não precisa estar respondendo);
    //  - classificação dos erros do Unity: conhecidos do vanilla contados, desconhecidos logados 1x;
    //  - detector de enxurrada (RateWatch) com pilha de chamadas, 1x por estágio.
    internal static class Diag
    {
        // Cumulativos da sessão; o Probe tira diferenças por janela.
        public static long GoldEvents;
        public static double CreditGiven;
        public static long CreditDropped; // eventos de ouro cujo crédito foi cortado pelo teto/trava
        public static int Exceptions;     // exceções capturadas pelas features
        public static int Revives;
        public static int GrimoireUses;
        public static long GhostBladesSkipped; // gerações/renovações de Lâminas Fantasmas puladas pelo teto
        public static double GrimoireTrimmed; // crédito que o teto/trava do Grimório deixou de dar
        public static int Tombstones;     // lápides existentes agora
        public static int Monsters;       // última amostra
        public static int WindowMaxMonsters; // zerado pelo Probe a cada janela
        public static double MonsterSum;
        public static long MonsterSamples;

        // Erros do vanilla que já sabemos serem ruído (aparecem no log do Unity; aqui só contados).
        private static readonly string[] KnownNoise =
        {
            "PushPickupNotification", "FrolicAway", "DroneCopycat", "has no NetworkIdentity",
            "Non-convex MeshCollider", "InvalidKeyException", "Could not find target object with netId",
        };
        private static readonly int[] KnownCounts = new int[KnownNoise.Length];
        private static readonly HashSet<string> SeenUnknown = new HashSet<string>();
        private static readonly object UnityLogLock = new object();
        private static int _unknownCount;
        private static int _unknownAtStage;

        private const float StallSeconds = 3f;
        private static ManualLogSource _log;
        private static long _beat; // Stopwatch.GetTimestamp() do último frame
        private static volatile bool _watch;
        private static float _sampleAcc;

        // Estágio atual
        private static bool _stageActive;
        private static string _stageName;
        private static float _stageStart;
        private static float _stageWorstMs;
        private static int _stageKills;
        private static int _stageMaxMonsters;
        private static long _gold0;
        private static double _credit0;
        private static long _dropped0;
        private static int _exc0, _rev0;

        private static readonly List<RateWatch> Watches = new List<RateWatch>();

        public static ManualLogSource Log => _log;

        public static void Install(ManualLogSource log)
        {
            _log = log;
            Interlocked.Exchange(ref _beat, Stopwatch.GetTimestamp());
            Stage.onServerStageBegin += OnStageBegin;
            Stage.onServerStageComplete += OnStageComplete;
            Run.onRunDestroyGlobal += OnRunDestroy;
            GlobalEventManager.onCharacterDeathServer += OnDeath;
            RoR2Application.onUpdate += OnUpdate;
            Application.logMessageReceivedThreaded += OnUnityLog;
            new Thread(WatchLoop) { IsBackground = true, Name = "Ror2Meeeee.Watchdog" }.Start();
        }

        // ---- estágio -------------------------------------------------------------------------------------------

        private static void OnStageBegin(Stage stage)
        {
            try
            {
                if (_stageActive) LogStageSummary("(interrompido)");
                _stageActive = true;
                _stageName = stage && stage.sceneDef ? stage.sceneDef.cachedName : "?";
                _stageStart = Time.time;
                _stageWorstMs = 0f;
                _stageKills = 0;
                _stageMaxMonsters = 0;
                _gold0 = GoldEvents;
                _credit0 = CreditGiven;
                _dropped0 = CreditDropped;
                _exc0 = Exceptions;
                _rev0 = Revives;
                _unknownAtStage = _unknownCount;
                foreach (var w in Watches) w.ResetStage();
                DroppedGrants.Clear();
                Interlocked.Exchange(ref _beat, Stopwatch.GetTimestamp());
                _watch = true;
            }
            catch (Exception e) { _log?.LogWarning($"[Diag] falha no início do estágio: {e.Message}"); }
        }

        private static void OnStageComplete(Stage stage)
        {
            try { LogStageSummary("concluído"); }
            catch (Exception e) { _log?.LogWarning($"[Diag] falha no resumo do estágio: {e.Message}"); }
            _watch = false; // a troca de cena pode bloquear a thread principal legitimamente
        }

        private static void OnRunDestroy(Run run)
        {
            try { if (_stageActive) LogStageSummary("run encerrada"); }
            catch (Exception e) { _log?.LogWarning($"[Diag] falha no resumo do estágio: {e.Message}"); }
            _watch = false;
        }

        private static void LogStageSummary(string how)
        {
            if (!_stageActive) return;
            _stageActive = false;
            int unknown;
            lock (UnityLogLock) unknown = _unknownCount - _unknownAtStage;
            _log.LogInfo($"[Estágio] {_stageName} {how}: {Time.time - _stageStart:F0} s | pior frame {_stageWorstMs:F0} ms | "
                + $"abates {_stageKills} | monstros pico {_stageMaxMonsters} | ouro {GoldEvents - _gold0} eventos, crédito +{CreditGiven - _credit0:F0} "
                + $"(cortados {CreditDropped - _dropped0}) | revives {Revives - _rev0} | exceções do mod {Exceptions - _exc0} | erros novos do jogo {unknown}");
        }

        private static void OnDeath(DamageReport report)
        {
            if (_stageActive && report != null && report.victimTeamIndex == TeamIndex.Monster) _stageKills++;
        }

        // ---- por frame -----------------------------------------------------------------------------------------

        private static void OnUpdate()
        {
            Interlocked.Exchange(ref _beat, Stopwatch.GetTimestamp());
            var dt = Time.unscaledDeltaTime;
            var ms = dt * 1000f;
            if (ms > _stageWorstMs) _stageWorstMs = ms;
            _sampleAcc += dt;
            if (_sampleAcc < 0.5f) return;
            _sampleAcc = 0f;
            var n = Run.instance ? TeamComponent.GetTeamMembers(TeamIndex.Monster).Count : 0;
            Monsters = n;
            MonsterSum += n;
            MonsterSamples++;
            if (n > WindowMaxMonsters) WindowMaxMonsters = n;
            if (n > _stageMaxMonsters) _stageMaxMonsters = n;
        }

        // Tipos de monstro vivos agora, do mais comum ao menos ("BeetleBody=12, WispBody=8"). Chamar só em eventos raros.
        public static string MonsterBreakdown(int top)
        {
            var members = TeamComponent.GetTeamMembers(TeamIndex.Monster);
            if (members == null || members.Count == 0) return "nenhum";
            var counts = new Dictionary<string, int>();
            for (var i = 0; i < members.Count; i++)
            {
                var tc = members[i];
                var name = tc && tc.body ? tc.body.name : "?";
                if (name.EndsWith("(Clone)")) name = name.Substring(0, name.Length - 7);
                counts.TryGetValue(name, out var c);
                counts[name] = c + 1;
            }
            var list = new List<KeyValuePair<string, int>>(counts);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            var sb = new StringBuilder();
            for (var i = 0; i < list.Count && i < top; i++)
                sb.Append(i == 0 ? "" : ", ").Append(list[i].Key).Append('=').Append(list[i].Value);
            if (list.Count > top) sb.Append($" (+{list.Count - top} tipos)");
            return sb.ToString();
        }

        // ---- vigia de travada (thread de fundo: só Stopwatch e campos simples) ---------------------------------

        private static void WatchLoop()
        {
            var nextReport = StallSeconds;
            var stalled = false;
            while (!Plugin.Quitting)
            {
                Thread.Sleep(1000);
                try
                {
                    if (!_watch) { stalled = false; nextReport = StallSeconds; continue; }
                    var gap = (Stopwatch.GetTimestamp() - Interlocked.Read(ref _beat)) / (double)Stopwatch.Frequency;
                    if (gap >= nextReport)
                    {
                        stalled = true;
                        nextReport = (float)gap + 10f;
                        _log.LogWarning($"[Vigia] jogo sem atualizar há {gap:F0} s (monstros ~{Monsters}, eventos de ouro {GoldEvents}, lápides {Tombstones}).");
                    }
                    else if (gap < StallSeconds && stalled)
                    {
                        stalled = false;
                        nextReport = StallSeconds;
                        _log.LogInfo("[Vigia] jogo voltou a responder.");
                    }
                }
                catch (Exception) { /* a vigia nunca derruba nada */ }
            }
        }

        // ---- erros do Unity ------------------------------------------------------------------------------------

        private static void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (string.IsNullOrEmpty(condition) || condition.Contains("Ror2Meeeee")) return;
            lock (UnityLogLock)
            {
                for (var i = 0; i < KnownNoise.Length; i++)
                    if (condition.Contains(KnownNoise[i])) { KnownCounts[i]++; return; }

                _unknownCount++;
                var nl = condition.IndexOf('\n');
                var key = (nl > 0 ? condition.Substring(0, nl) : condition);
                if (key.Length > 140) key = key.Substring(0, 140);
                if (SeenUnknown.Count < 60 && SeenUnknown.Add(key))
                    _log?.LogWarning($"[Diag] erro novo do jogo/outro mod (1ª vez): {key}");
            }
        }

        // ---- relatório (ror2meeeee_status) ---------------------------------------------------------------------

        public static string Report()
        {
            var sb = new StringBuilder("Diagnóstico:\n");
            sb.Append($"  ouro: {GoldEvents} eventos, crédito +{CreditGiven:F0}, cortado em {CreditDropped} eventos\n");
            sb.Append($"  monstros agora ~{Monsters}, pico do estágio {_stageMaxMonsters}, pior frame do estágio {_stageWorstMs:F0} ms\n");
            sb.Append($"  exceções do mod {Exceptions}, revives {Revives}, lápides {Tombstones}, usos do Grimório {GrimoireUses} (crédito cortado {GrimoireTrimmed:F0}), lâminas fantasmas puladas {GhostBladesSkipped}\n");
            sb.Append($"  MoneyHub: {MoneyHub.Calls} chamadas, custo do mod ~{MoneyHub.AverageMicros:F1} µs/chamada (amostrado)\n");
            lock (UnityLogLock)
            {
                sb.Append($"  erros do jogo: {_unknownCount} novos");
                for (var i = 0; i < KnownNoise.Length; i++)
                    if (KnownCounts[i] > 0) sb.Append($", {KnownNoise[i]}={KnownCounts[i]}");
            }
            return sb.ToString();
        }

        // ---- enxurrada -----------------------------------------------------------------------------------------

        // Conta eventos numa janela de 1 s; ao cruzar o limite loga UMA vez por estágio, com a pilha de chamadas de
        // quem disparou (a origem costuma ser outro item ou o jogo, não o mod).
        internal sealed class RateWatch
        {
            private readonly string _name;
            private readonly int _threshold;
            private float _start;
            private int _n;
            private bool _logged;

            public RateWatch(string name, int threshold)
            {
                _name = name;
                _threshold = threshold;
                lock (Watches) Watches.Add(this);
            }

            public void ResetStage() => _logged = false;

            public void Hit(string detail = null)
            {
                var now = Time.time;
                if (now - _start >= 1f) { _start = now; _n = 0; }
                if (++_n != _threshold || _logged) return;
                _logged = true;
                _log?.LogWarning($"[Diag] enxurrada de {_name}: {_n} em <1 s{(detail != null ? " (" + detail + ")" : "")}; monstros ~{Monsters}. Origem:\n{new StackTrace(1, false)}");
            }
        }
    }
}
