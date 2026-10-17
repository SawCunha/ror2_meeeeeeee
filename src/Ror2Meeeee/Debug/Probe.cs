using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using RoR2;
using RoR2.Projectile;
using Ror2Meeeee.Core.Measurement;
using UnityEngine;

namespace Ror2Meeeee
{
    // Mede frame time, RAM e contadores por fase nomeada. Kill-switch: General.ProbeEnabled.
    // Sem Stopwatch nos hooks: o frame time vem de Time.unscaledDeltaTime em RoR2Application.onUpdate.
    // Buffers pré-alocados em OnEnable; o CSV só é escrito no fim da fase.
    public sealed class Probe : Feature
    {
        private const int Capacity = 72000; // 20 min a 60 fps
        private const string Header = "utc,game_version,mod_version,phase,duration_s,frames,mean_ms,p95_ms,ram_managed_mb,ram_unity_alloc_mb,projectiles,damage_events,damage_total,heal_total,empty_hook,truncated,monsters_mean,monsters_max,gold_events,credit_given,credit_dropped,unity_delta_mb,worst_frame_ms,heal_events,minions,player_items,alert";

        private float[] _frames;
        private int _count;
        private double _duration;
        private bool _truncated;
        private string _phase; // null = nenhuma fase ativa
        private bool _phaseIsAuto;
        private int _autoIndex;
        private bool _warnedCap;
        // Projéteis por prefab (InstanceID: sem alocar string por chamada); nomes só no fim da janela.
        private readonly Dictionary<int, int> _projByPrefab = new Dictionary<int, int>();
        private readonly Dictionary<int, GameObject> _prefabs = new Dictionary<int, GameObject>();
        private int _projectiles;
        private int _damageEvents;
        private double _damageTotal;
        private double _healTotal;
        private int _healEvents;
        // Janela: diferenças dos contadores cumulativos do Diag e linha de base para o alerta de degradação.
        private long _gold0, _dropped0, _monN0;
        private double _credit0, _monSum0;
        private double _lastUnityMb = -1;
        private float _baseMean = float.MaxValue; // menor média de fase completa da run
        private readonly Diag.RateWatch _damageFlood = new Diag.RateWatch("eventos de dano", 800);

        public Probe(Cfg cfg, ManualLogSource log) : base("Probe", cfg, log, cfg.ProbeEnabledEntry) { }

        public string ActivePhase => _phase;

        // Prefixo das janelas automáticas (ex.: "base" -> base-1, base-2...). Troque entre condições sem reiniciar.
        public string Label { get; set; } = "auto";

        public string Begin(string phase)
        {
            if (!Enabled) return "Probe desligado (General.ProbeEnabled=false).";
            if (_phase != null && !_phaseIsAuto) return $"Fase '{_phase}' já está ativa; encerre com ror2meeeee_probe_end.";
            _phaseIsAuto = false; // fase manual substitui a janela automática em andamento
            _count = 0;
            _duration = 0;
            _truncated = false;
            _projectiles = 0;
            _projByPrefab.Clear();
            _prefabs.Clear();
            _damageEvents = 0;
            _damageTotal = 0;
            _healTotal = 0;
            _healEvents = 0;
            _gold0 = Diag.GoldEvents;
            _credit0 = Diag.CreditGiven;
            _dropped0 = Diag.CreditDropped;
            _monSum0 = Diag.MonsterSum;
            _monN0 = Diag.MonsterSamples;
            Diag.WindowMaxMonsters = 0;
            _phase = phase;
            Log.LogInfo($"[Probe] Fase '{phase}' iniciada.");
            return $"Fase '{phase}' iniciada.";
        }

        // async = true nas janelas automáticas: estatística, CSV e log vão para o ThreadPool (na hora, se o jogo está fechando).
        // Na thread principal só se captura o que exige API do Unity (memória, nomes de prefab, fps).
        public string End(bool async = false)
        {
            if (_phase == null) return "Nenhuma fase ativa.";
            var snap = new Snapshot
            {
                Phase = _phase,
                Duration = _duration,
                Count = _count,
                Frames = new float[_count], // cópia pequena (<= 16 KB por janela de 30 s); o buffer fixo segue na principal
                ManagedMb = GC.GetTotalMemory(false) / 1048576.0,
                // Environment.WorkingSet devolve 0 sob Mono/Proton. Alocada (não reservada): a reservada quase não varia.
                UnityMb = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576.0,
                Projectiles = _projectiles,
                DamageEvents = _damageEvents,
                DamageTotal = _damageTotal,
                HealTotal = _healTotal,
                HealEvents = _healEvents,
                Truncated = _truncated,
                EmptyHook = Cfg.ProbeEmptyHook,
                FpsMax = Application.targetFrameRate,
                VSync = QualitySettings.vSyncCount,
                TopProjectiles = TopProjectilesText(),
                MonstersMean = Diag.MonsterSamples > _monN0 ? (Diag.MonsterSum - _monSum0) / (Diag.MonsterSamples - _monN0) : 0,
                MonstersMax = Diag.WindowMaxMonsters,
                GoldEvents = Diag.GoldEvents - _gold0,
                CreditGiven = Diag.CreditGiven - _credit0,
                CreditDropped = Diag.CreditDropped - _dropped0,
                Tombstones = Diag.Tombstones,
                Breakdown = Diag.MonsterBreakdown(6), // só aqui (1x por janela); impresso apenas se houver alerta
            };
            CountPlayers(out snap.Minions, out snap.PlayerItems);
            snap.UnityDeltaMb = _lastUnityMb < 0 ? 0 : snap.UnityMb - _lastUnityMb;
            _lastUnityMb = snap.UnityMb;
            Array.Copy(_frames, snap.Frames, _count);
            _phase = null;
            _phaseIsAuto = false;

            var capped = snap.FpsMax > 0 || snap.VSync > 0;
            if (capped && !_warnedCap)
            {
                _warnedCap = true;
                Log.LogWarning("[Probe] FPS limitado (fps_max/vsync): o frame time reflete o limite, não o custo do jogo. No console: fps_max -1 e vsync_count 0.");
            }

            var path = System.IO.Path.Combine(Paths.BepInExRootPath, "ror2meeeee-probe.csv");
            if (!async || Plugin.Quitting) return Finish(snap, path);
            Plugin.RunInBackground(() =>
            {
                try { Finish(snap, path); }
                catch (Exception e) { Log.LogError($"Probe: falha ao fechar a janela '{snap.Phase}': {e.Message}"); }
            });
            return $"Fase '{snap.Phase}' encerrada (resultado no log e no CSV).";
        }

        // Uma vez por janela (thread principal): ajudantes dos jogadores (drones, torretas...) e total de cópias de itens.
        private static void CountPlayers(out int minions, out long items)
        {
            minions = 0; items = 0;
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                var m = pcmc ? pcmc.master : null;
                if (!m) continue;
                var group = MinionOwnership.MinionGroup.FindGroup(m.netId);
                if (group != null) minions += group.memberCount;
                var inv = m.inventory;
                if (!inv) continue;
                foreach (var idx in inv.itemAcquisitionOrder) items += inv.GetItemCountPermanent(idx);
            }
        }

        private sealed class Snapshot
        {
            public string Phase;
            public double Duration;
            public int Count;
            public float[] Frames;
            public double ManagedMb, UnityMb, DamageTotal, HealTotal;
            public int Projectiles, DamageEvents, FpsMax, VSync;
            public bool Truncated, EmptyHook;
            public string TopProjectiles;
            public double MonstersMean, CreditGiven, UnityDeltaMb;
            public int MonstersMax, Tombstones, HealEvents, Minions;
            public long PlayerItems;
            public long GoldEvents, CreditDropped;
            public string Breakdown;
        }

        // Sem API do Unity: pode rodar em qualquer thread. O lock mantém a ordem e o cabeçalho do CSV.
        private static readonly object CsvLock = new object();

        private string Finish(Snapshot s, string path)
        {
            FrameStats.Compute(s.Frames, s.Count, new float[s.Count], out var mean, out var p95);
            var inv = CultureInfo.InvariantCulture;
            var worst = 0f;
            for (var i = 0; i < s.Count; i++) if (s.Frames[i] > worst) worst = s.Frames[i];
            var alert = Evaluate(s, mean, worst);
            var row = string.Join(",", new[]
            {
                DateTime.UtcNow.ToString("o", inv),
                Plugin.GameVersion,
                ModInfo.Version,
                s.Phase.Replace(',', ';'),
                s.Duration.ToString("F1", inv),
                s.Count.ToString(inv),
                mean.ToString("F3", inv),
                p95.ToString("F3", inv),
                s.ManagedMb.ToString("F1", inv),
                s.UnityMb.ToString("F1", inv),
                s.Projectiles.ToString(inv),
                s.DamageEvents.ToString(inv),
                s.DamageTotal.ToString("F0", inv),
                s.HealTotal.ToString("F0", inv),
                s.EmptyHook ? "true" : "false",
                s.Truncated ? "true" : "false",
                s.MonstersMean.ToString("F1", inv),
                s.MonstersMax.ToString(inv),
                s.GoldEvents.ToString(inv),
                s.CreditGiven.ToString("F0", inv),
                s.CreditDropped.ToString(inv),
                s.UnityDeltaMb.ToString("F0", inv),
                worst.ToString("F0", inv),
                s.HealEvents.ToString(inv),
                s.Minions.ToString(inv),
                s.PlayerItems.ToString(inv),
                alert == null ? "" : alert.Replace(',', ';'),
            });
            lock (CsvLock)
            {
                RotateIfOldHeader(path);
                var text = File.Exists(path) ? row + "\n" : Header + "\n" + row + "\n";
                File.AppendAllText(path, text);
            }

            // Tudo no LogOutput.log para validar sem abrir o CSV.
            var summary = $"Fase '{s.Phase}': {s.Duration:F1}s, {s.Count} frames, média {mean:F2} ms, p95 {p95:F2} ms | "
                + $"RAM gerenciada {s.ManagedMb:F0} MB, Unity alocada {s.UnityMb:F0} MB | projéteis {s.Projectiles}, "
                + $"eventos de dano {s.DamageEvents}, dano total {s.DamageTotal:F0}, cura total {s.HealTotal:F0} | "
                + $"hook vazio {(s.EmptyHook ? "sim" : "não")}{(s.Truncated ? " | TRUNCADA (buffer cheio)" : "")}"
                + $" | fps_max {s.FpsMax}, vsync {s.VSync}"
                + $" | monstros média {s.MonstersMean:F0} pico {s.MonstersMax}, ouro {s.GoldEvents} eventos (+{s.CreditGiven:F0} crédito, cortados {s.CreditDropped}), "
                + $"Unity {s.UnityDeltaMb:+0;-0;0} MB, pior frame {worst:F0} ms, lápides {s.Tombstones}"
                + $" | eventos de cura {s.HealEvents}, ajudantes {s.Minions}, itens dos jogadores {s.PlayerItems}";
            Log.LogInfo("[Probe] " + summary);
            if (alert != null)
            {
                Log.LogWarning($"[Probe] ALERTA na fase '{s.Phase}': {alert}");
                Log.LogWarning($"[Probe] monstros vivos ao fim da janela: {s.Breakdown}");
            }
            if (s.TopProjectiles != null) Log.LogInfo(s.TopProjectiles);
            return $"{summary} -> {path}";
        }

        // Degradação: média > 3x a melhor fase completa da run (e acima de 25 ms), salto de memória nativa ou frame travado.
        // Sem API do Unity; roda no ThreadPool, sob o mesmo lock do CSV.
        private string Evaluate(Snapshot s, float mean, float worst)
        {
            lock (CsvLock)
            {
                string alert = null;
                void Add(string t) => alert = alert == null ? t : alert + " | " + t;
                if (s.Count > 0 && _baseMean < float.MaxValue && mean > Math.Max(_baseMean * 3f, 25f))
                    Add($"frame médio {mean:F1} ms (linha de base {_baseMean:F1} ms)");
                if (s.UnityDeltaMb > 300) Add($"memória Unity +{s.UnityDeltaMb:F0} MB na janela");
                if (worst > 1000f) Add($"frame de {worst:F0} ms (travada)");
                if (s.Duration >= 20 && s.Count > 0 && mean < _baseMean) _baseMean = mean;
                return alert;
            }
        }

        // CSV de versão anterior (outra coluna de RAM): guardado à parte, para não misturar significados.
        private static void RotateIfOldHeader(string path)
        {
            if (!File.Exists(path)) return;
            string first;
            using (var r = new StreamReader(path)) first = r.ReadLine();
            if (first == Header) return;
            var old = System.IO.Path.ChangeExtension(path, null) + ".antigo-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + ".csv";
            File.Move(path, old);
        }

        private string TopProjectilesText()
        {
            if (_projByPrefab.Count == 0) return null;
            var list = new List<KeyValuePair<int, int>>(_projByPrefab);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            var sb = new System.Text.StringBuilder("[Probe] projéteis por tipo:");
            for (var i = 0; i < list.Count && i < 5; i++)
            {
                _prefabs.TryGetValue(list[i].Key, out var go);
                sb.Append(i == 0 ? " " : ", ").Append(go ? go.name : "?").Append('=').Append(list[i].Value);
            }
            return sb.ToString();
        }

        public override void ResetRunState()
        {
            _lastUnityMb = -1; // nova run: o salto de memória entre runs não é alerta
            lock (CsvLock) _baseMean = float.MaxValue;
            if (_phase == null) return;
            if (_phaseIsAuto && _count > 0)
            {
                End(async: true); // fecha a janela parcial: vai para o log e o CSV
                return;
            }
            Log.LogWarning($"Probe: fase '{_phase}' descartada na troca de run.");
            _phase = null;
            _phaseIsAuto = false;
        }

        protected override void OnEnable()
        {
            _frames ??= new float[Capacity]; // reaproveitado ao religar
            Track("ProjectileManager.FireProjectile(FireProjectileInfo) [contador]",
                () => On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += OnFireProjectile);
            if (Cfg.ProbeEmptyHook)
                Track("ProjectileManager.FireProjectile(FireProjectileInfo) [vazio]",
                    () => On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += OnFireProjectileEmpty);
            Track("GlobalEventManager.onServerDamageDealt", () => GlobalEventManager.onServerDamageDealt += OnDamageDealt);
            Track("HealthComponent.onCharacterHealServer", () => HealthComponent.onCharacterHealServer += OnHealed);
            Track("RoR2Application.onUpdate", () => RoR2Application.onUpdate += OnUpdate);
        }

        protected override void OnDisable()
        {
            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo -= OnFireProjectile;
            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo -= OnFireProjectileEmpty;
            GlobalEventManager.onServerDamageDealt -= OnDamageDealt;
            HealthComponent.onCharacterHealServer -= OnHealed;
            RoR2Application.onUpdate -= OnUpdate;
            _phase = null;
        }

        private void OnFireProjectile(On.RoR2.Projectile.ProjectileManager.orig_FireProjectile_FireProjectileInfo orig, ProjectileManager self, FireProjectileInfo info)
        {
            if (_phase != null)
            {
                _projectiles++;
                var prefab = info.projectilePrefab;
                if (prefab)
                {
                    var id = prefab.GetInstanceID();
                    _projByPrefab.TryGetValue(id, out var n);
                    _projByPrefab[id] = n + 1;
                    if (n == 0) _prefabs[id] = prefab;
                }
            }
            orig(self, info);
        }

        // Só repassa: a diferença de frame time com/sem este hook é o custo de um hook (A6).
        private void OnFireProjectileEmpty(On.RoR2.Projectile.ProjectileManager.orig_FireProjectile_FireProjectileInfo orig, ProjectileManager self, FireProjectileInfo info)
        {
            orig(self, info);
        }

        private void OnDamageDealt(DamageReport report)
        {
            _damageFlood.Hit();
            if (_phase == null) return;
            _damageEvents++;
            _damageTotal += report.damageDealt; // todos os times: dano causado por qualquer fonte
        }

        // Cura total de qualquer fonte (inclui regeneração de outros itens); comparar fases iguais.
        private void OnHealed(HealthComponent target, float amount, ProcChainMask mask)
        {
            if (_phase != null) { _healTotal += amount; _healEvents++; }
        }

        private void OnUpdate()
        {
            try
            {
                if (_phase == null)
                {
                    // Janela automática: só dentro de uma run, para o log e o CSV saírem sem comandos.
                    if (Cfg.ProbeAutoWindowSeconds > 0 && Run.instance)
                    {
                        Begin(Label + "-" + (++_autoIndex).ToString(CultureInfo.InvariantCulture));
                        _phaseIsAuto = true;
                    }
                    return;
                }
                var dt = Time.unscaledDeltaTime;
                _duration += dt;
                if (_count < Capacity) _frames[_count++] = dt * 1000f;
                else _truncated = true;
                if (_phaseIsAuto && _duration >= Cfg.ProbeAutoWindowSeconds) End(async: true); // 1x por janela; só a captura fica na principal
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }
}
