using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using RoR2.Projectile;
using Ror2Meeeee.Core.Limits;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Teto de projéteis dos JOGADORES (e dos ajudantes deles), no host. Em runs longas com o multiplicador, itens que
    // disparam ao acertar (Sticky Bomb, Molten Perforator, estacas elétricas) passavam de 11 mil projéteis em 30 s e o
    // jogo caía para ~1 FPS; o lixo gerado também alongava as pausas do coletor de memória (travadas de vários segundos).
    //  - por tipo, para os prefabs da lista (PerTypeMaxPerSecond);
    //  - global, para todo projétil de jogador (GlobalMaxPerSecond).
    // Disparo acima do teto não sai: o dano dele vai para o próximo disparo permitido do mesmo tipo (Core.Limits.ProjectileBudget),
    // então o dano total se mantém. Projéteis de monstros nunca são tocados.
    // Espectros da Acolyte (WightLauncher): cada abate de alvo assombrado solta 1 espectro por acúmulo do DoT Haunt, e cada
    // espectro que acerta aplica mais Haunt. Com o multiplicador isso vira reação em cadeia (1.200 espectros em 30 s, jogo
    // travando por segundos). O lançador é limitado a WightsPerKill espectros, cada um com o dano dos cortados.
    public sealed class ProjectileBudgetFeature : Feature
    {
        private readonly ConfigEntry<string> _list;
        private readonly ConfigEntry<int> _perType;
        private readonly ConfigEntry<int> _global;
        private readonly ConfigEntry<int> _wights;
        private int _perTypeValue, _globalValue, _wightsValue;
        private static readonly FieldInfo WightDamageField = typeof(WightLauncher).GetField("damageCoefficient", BindingFlags.Instance | BindingFlags.NonPublic);
        private long _wightsCut;

        private readonly HashSet<string> _names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, bool> _listed = new Dictionary<int, bool>();        // prefab -> está na lista (cache)
        private readonly Dictionary<int, BudgetWindow> _windows = new Dictionary<int, BudgetWindow>(); // prefab -> janela
        private BudgetWindow _globalWindow;
        private long _skipped;

        public ProjectileBudgetFeature(Cfg cfg, ManualLogSource log) : base("ProjectileBudget", cfg, log,
            "Player projectile cap for FPS: items that fire en masse (Sticky Bomb, Molten Perforator, stakes, Fireworks, missiles) release fewer projectiles, each one stronger, with the same total damage. Host only; monster projectiles are unchanged.", "Teto de projéteis dos jogadores para o FPS: itens que disparam em massa (Sticky Bomb, Molten Perforator, estacas, Fireworks, mísseis) soltam menos projéteis, cada um mais forte, com o mesmo dano total. Só no host; projéteis de monstros não mudam.",
            enabledByDefault: true)
        {
            _list = cfg.Bind("ProjectileBudget", "Prefabs", "StickyBomb,FireMeatBall,LightningStake,DaggerProjectile,EngiGrenadeProjectile,ShurikenProjectile,FireworkProjectile,MissileProjectile",
                "Projectiles (prefab name, as in the Probe log) with a per-type cap. Comma-separated.", "Projéteis (nome do prefab, como no log do Probe) com teto por tipo. Separados por vírgula.");
            _perType = cfg.Bind("ProjectileBudget", "PerTypeMaxPerSecond", 15,
                "Maximum per second of each listed projectile (all players combined). 0 = no per-type cap.", "Máximo por segundo de cada projétil da lista (todos os jogadores somados). 0 = sem teto por tipo.");
            _global = cfg.Bind("ProjectileBudget", "GlobalMaxPerSecond", 40,
                "Maximum per second of projectiles from players and minions, of any type. 0 = no global cap.", "Máximo por segundo de projéteis de jogadores e ajudantes, de qualquer tipo. 0 = sem teto global.");
            _wights = cfg.Bind("ProjectileBudget", "WightsPerKill", 3,
                "Maximum Acolyte wights released per haunted kill; the cut ones add their damage to the rest. 0 = no cap.", "Máximo de espectros da Acolyte soltos por abate assombrado; os cortados somam o dano aos restantes. 0 = sem teto.");
            ReadConfig();
            _wights.SettingChanged += (_, __) => ReadConfig();
            _list.SettingChanged += (_, __) => ReadConfig();
            _perType.SettingChanged += (_, __) => ReadConfig();
            _global.SettingChanged += (_, __) => ReadConfig();
        }

        private void ReadConfig()
        {
            _perTypeValue = _perType.Value;
            _globalValue = _global.Value;
            _wightsValue = _wights.Value;
            _names.Clear();
            foreach (var raw in (_list.Value ?? "").Split(','))
            {
                var n = raw.Trim();
                if (n.Length > 0) _names.Add(n);
            }
            _listed.Clear();
        }

        public override void ResetRunState()
        {
            _windows.Clear();
            _globalWindow = default;
            if (_skipped > 0) Log.LogInfo($"[ProjectileBudget] run: {_skipped} projéteis de jogadores agregados (dano somado aos seguintes).");
            _skipped = 0;
            if (_wightsCut > 0) Log.LogInfo($"[ProjectileBudget] run: {_wightsCut} espectros da Acolyte cortados (dano somado aos restantes).");
            _wightsCut = 0;
        }

        protected override void OnEnable()
        {
            Track("ProjectileManager.FireProjectile", () => On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += OnFireProjectile);
            if (WightDamageField != null) Track("WightLauncher.FixedUpdate", () => On.RoR2.WightLauncher.FixedUpdate += OnWightLauncherFixedUpdate);
            else Log.LogWarning("[ProjectileBudget] WightLauncher.damageCoefficient não encontrado; espectros da Acolyte sem teto.");
        }

        protected override void OnDisable()
        {
            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo -= OnFireProjectile;
            On.RoR2.WightLauncher.FixedUpdate -= OnWightLauncherFixedUpdate;
        }

        // Só age uma vez por lançador: depois do corte, remaining <= teto.
        private void OnWightLauncherFixedUpdate(On.RoR2.WightLauncher.orig_FixedUpdate orig, WightLauncher self)
        {
            try
            {
                var cap = _wightsValue;
                if (NetworkServer.active && cap > 0 && self.remaining > cap)
                {
                    var coef = (float)WightDamageField.GetValue(self);
                    WightDamageField.SetValue(self, coef * self.remaining / cap);
                    _wightsCut += self.remaining - cap;
                    self.remaining = cap;
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(self);
        }

        private bool Listed(GameObject prefab)
        {
            var id = prefab.GetInstanceID();
            if (!_listed.TryGetValue(id, out var yes))
            {
                yes = _names.Contains(prefab.name);
                _listed[id] = yes;
            }
            return yes;
        }

        private static bool IsPlayerSide(GameObject owner)
        {
            if (!owner) return false;
            var tc = owner.GetComponent<TeamComponent>();
            return tc && tc.teamIndex == TeamIndex.Player;
        }

        private void OnFireProjectile(On.RoR2.Projectile.ProjectileManager.orig_FireProjectile_FireProjectileInfo orig, ProjectileManager self, FireProjectileInfo info)
        {
            try
            {
                var prefab = info.projectilePrefab;
                if (NetworkServer.active && prefab && (_perTypeValue > 0 || _globalValue > 0) && IsPlayerSide(info.owner))
                {
                    var now = Time.time;
                    // Janela por prefab para todo projétil de jogador: fora da lista ela não tem teto (0) e só guarda o dano
                    // cortado pelo teto global, somado ao próximo disparo do mesmo tipo.
                    var perType = _perTypeValue > 0 && Listed(prefab) ? _perTypeValue : 0;
                    var key = prefab.GetInstanceID();
                    _windows.TryGetValue(key, out var w);
                    var fire = ProjectileBudget.Decide(ref w, now, perType, info.damage, out var damage);
                    if (fire && _globalValue > 0)
                    {
                        fire = ProjectileBudget.Decide(ref _globalWindow, now, _globalValue, 0f, out _);
                        if (!fire) w.PendingDamage += damage; // estourou o global: o dano volta para o tipo
                    }
                    _windows[key] = w;
                    if (!fire) { _skipped++; return; }
                    info.damage = damage; // struct: cópia local, com o dano agregado
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(self, info);
        }
    }
}
