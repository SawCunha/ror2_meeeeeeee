using System;
using System.Collections.Generic;
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
    public sealed class ProjectileBudgetFeature : Feature
    {
        private readonly ConfigEntry<string> _list;
        private readonly ConfigEntry<int> _perType;
        private readonly ConfigEntry<int> _global;
        private int _perTypeValue, _globalValue;

        private readonly HashSet<string> _names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, bool> _listed = new Dictionary<int, bool>();        // prefab -> está na lista (cache)
        private readonly Dictionary<int, BudgetWindow> _windows = new Dictionary<int, BudgetWindow>(); // prefab -> janela
        private BudgetWindow _globalWindow;
        private long _skipped;

        public ProjectileBudgetFeature(Cfg cfg, ManualLogSource log) : base("ProjectileBudget", cfg, log,
            "Teto de projéteis dos jogadores para o FPS: itens que disparam em massa (Sticky Bomb, Molten Perforator, estacas) soltam menos projéteis, cada um mais forte, com o mesmo dano total. Só no host; projéteis de monstros não mudam.",
            enabledByDefault: true)
        {
            _list = cfg.Bind("ProjectileBudget", "Prefabs", "StickyBomb,FireMeatBall,LightningStake,DaggerProjectile,EngiGrenadeProjectile,ShurikenProjectile",
                "Projéteis (nome do prefab, como no log do Probe) com teto por tipo. Separados por vírgula.");
            _perType = cfg.Bind("ProjectileBudget", "PerTypeMaxPerSecond", 15,
                "Máximo por segundo de cada projétil da lista (todos os jogadores somados). 0 = sem teto por tipo.");
            _global = cfg.Bind("ProjectileBudget", "GlobalMaxPerSecond", 80,
                "Máximo por segundo de projéteis de jogadores e ajudantes, de qualquer tipo. 0 = sem teto global.");
            ReadConfig();
            _list.SettingChanged += (_, __) => ReadConfig();
            _perType.SettingChanged += (_, __) => ReadConfig();
            _global.SettingChanged += (_, __) => ReadConfig();
        }

        private void ReadConfig()
        {
            _perTypeValue = _perType.Value;
            _globalValue = _global.Value;
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
        }

        protected override void OnEnable()
        {
            Track("ProjectileManager.FireProjectile", () => On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += OnFireProjectile);
        }

        protected override void OnDisable()
        {
            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo -= OnFireProjectile;
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
