using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using Ror2Meeeee.Core.Stats;
using UnityEngine;

namespace Ror2Meeeee
{
    // Efeito do item "Cano Extra": repete o disparo da arma principal em leque, 1 extra por cópia.
    // "Arma principal" = ataque marcado pelo jogo com DamageSource.Primary (o mesmo critério dos itens oficiais).
    // Três pontos de disparo: BulletAttack.Fire (balas), ProjectileManager.FireProjectile (projéteis) e
    // OrbManager.AddOrb (flechas da Huntress). Os extras chamam `orig` direto: não reentram neste hook.
    // EXCEÇÃO à regra "só no host": o tiro do jogador é calculado no computador dele (balas e projéteis);
    // as flechas da Huntress nascem no host. Cada hook roda onde o jogo dispara.
    // Custo sem ninguém com o item: zero por disparo. Os 3 hooks de disparo só existem enquanto a lista de portadores
    // (mantida por eventos de inventário/corpo) não está vazia; registrar/remover custa alguns ms, mas só na mudança.
    // Proteções de FPS: teto por jogador (MaxExtraShots), teto global por frame e distância do corpo
    // (projéteis-filhos que nascem longe, no impacto, não são multiplicados).
    public sealed class CanoFeature : Feature
    {
        private const int MaxExtraPerFrame = 64;
        private const float MaxOriginDistanceSqr = 8f * 8f;

        // MemberwiseClone como delegate (resolvido uma vez): sem reflexão nem object[] por orb extra.
        private static readonly Func<object, object> Clone = MakeClone();

        private static Func<object, object> MakeClone()
        {
            var m = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return m == null ? null : (Func<object, object>)Delegate.CreateDelegate(typeof(Func<object, object>), m);
        }

        private readonly ConfigEntry<int> _maxExtra;
        private readonly ConfigEntry<float> _fanDegrees;
        private int _maxExtraValue;
        private float _fanDegreesValue;
        private Quaternion[] _fan; // rotação de cada tiro extra, recalculada só quando a config muda

        private int _frame = -1;
        private int _usedThisFrame;
        // Quem tem o item, por GameObject do corpo. Mantido pelos eventos de inventário/corpo do jogo (não por tiro):
        // vazio = cada disparo do jogo custa só `Count == 0`. Pequeno (jogadores com o item), nunca cresce com monstros.
        private readonly Dictionary<GameObject, CharacterBody> _holders = new Dictionary<GameObject, CharacterBody>();
        // "É primário" por prefab de projétil (limitado ao número de prefabs); limpo por run.
        private readonly Dictionary<int, bool> _primaryPrefab = new Dictionary<int, bool>();
        private bool _fireHooked;

        public CanoFeature(Cfg cfg, ManualLogSource log) : base("CanoExtra", cfg, log,
            "Extra Barrel item (red): each stack makes the primary weapon fire +1 shot in a spread (bullets, projectiles and arrows; not melee). Off: the item exists but does nothing.", "Item Cano Extra (vermelho): cada cópia faz a arma principal disparar +1 tiro em leque (balas, projéteis e flechas; corpo a corpo não). Desligado: o item existe mas não faz nada.",
            enabledByDefault: true)
        {
            _maxExtra = cfg.Bind("CanoExtra", "MaxExtraShots", 5,
                "Maximum extra shots per firing, even with more stacks (protects FPS). 0 = the item fires no extra shots.", "Máximo de tiros extras por disparo, mesmo com mais cópias (protege o FPS). 0 = o item não dispara extras.");
            _fanDegrees = cfg.Bind("CanoExtra", "FanDegrees", 5f,
                "Spread angle in degrees between one shot and the next (5 = shots at ±5°, ±10°...).", "Abertura do leque em graus entre um tiro e o próximo (5 = tiros a ±5°, ±10°...).");
            ReadConfig();
            _maxExtra.SettingChanged += (_, __) => ReadConfig();
            _fanDegrees.SettingChanged += (_, __) => ReadConfig();
        }

        private void ReadConfig()
        {
            _maxExtraValue = _maxExtra.Value;
            _fanDegreesValue = _fanDegrees.Value;
            var n = Mathf.Clamp(_maxExtraValue, 0, MaxExtraPerFrame); // nunca há mais extras que isso por disparo
            var fan = new Quaternion[n];
            for (var i = 0; i < n; i++)
                fan[i] = Quaternion.AngleAxis(MultiShot.FanAngle(i, _fanDegreesValue), Vector3.up);
            _fan = fan;
        }

        public override void ResetRunState()
        {
            _holders.Clear();
            _primaryPrefab.Clear();
            SyncFireHooks();
        }

        protected override void OnEnable()
        {
            Track("CharacterBody.onBodyInventoryChangedGlobal", () => CharacterBody.onBodyInventoryChangedGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyStartGlobal", () => CharacterBody.onBodyStartGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyDestroyGlobal", () => CharacterBody.onBodyDestroyGlobal += OnBodyDestroy);
            // BulletAttack.Fire, FireProjectile e AddOrb: registrados por SyncFireHooks quando surge um portador.
        }

        protected override void OnDisable()
        {
            SetFireHooks(false);
            CharacterBody.onBodyInventoryChangedGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyStartGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyDestroyGlobal -= OnBodyDestroy;
            _holders.Clear();
            _primaryPrefab.Clear();
        }

        // Hooks de disparo só com portador: sem ninguém com o item, os disparos do jogo não passam pelo mod.
        private void SyncFireHooks()
        {
            if (!Enabled && _holders.Count > 0) return; // ResetRunState/eventos com a feature desligada
            try { SetFireHooks(_holders.Count > 0); }
            catch (Exception e) { Fail(e); }
        }

        private void SetFireHooks(bool on)
        {
            if (on == _fireHooked) return;
            if (on)
            {
                On.RoR2.BulletAttack.Fire_FireArgs += OnBulletFire;
                On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += OnFireProjectile;
                On.RoR2.Orbs.OrbManager.AddOrb += OnAddOrb;
            }
            else
            {
                On.RoR2.BulletAttack.Fire_FireArgs -= OnBulletFire;
                On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo -= OnFireProjectile;
                On.RoR2.Orbs.OrbManager.AddOrb -= OnAddOrb;
            }
            _fireHooked = on;
            Log.LogInfo($"[CanoExtra] hooks de disparo {(on ? "ligados (há portador)" : "desligados (ninguém com o item)")}");
        }

        // Roda quando um inventário muda ou um corpo nasce (raro), em toda máquina: o inventário é sincronizado.
        private void OnHolderMaybeChanged(CharacterBody body)
        {
            try
            {
                var idx = CanoItem.Index;
                if (idx == ItemIndex.None || !body) return;
                var inv = body.inventory;
                if (inv && inv.GetItemCountEffective(idx) > 0) _holders[body.gameObject] = body;
                else if (_holders.Count > 0) _holders.Remove(body.gameObject);
                SyncFireHooks();
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnBodyDestroy(CharacterBody body)
        {
            if (_holders.Count > 0 && body && _holders.Remove(body.gameObject)) SyncFireHooks();
        }

        // ---- filtros (do mais barato ao mais caro) ----

        private static bool IsPrimary(DamageTypeCombo damageType) => (damageType.damageSource & DamageSource.Primary) != 0;

        // Tiros extras que este dono pode disparar agora (0 = nenhum).
        private int ExtrasFor(GameObject owner, Vector3 origin)
        {
            if (!owner || !_holders.TryGetValue(owner, out var body) || !body) return 0;
            var inv = body.inventory;
            if (!inv) return 0;
            var wanted = MultiShot.ExtraShots(inv.GetItemCountEffective(CanoItem.Index), _maxExtraValue);
            if (wanted == 0 || (origin - body.corePosition).sqrMagnitude > MaxOriginDistanceSqr) return 0;

            var frame = Time.frameCount;
            if (frame != _frame) { _frame = frame; _usedThisFrame = 0; }
            var allowed = MultiShot.Budget(wanted, _usedThisFrame, MaxExtraPerFrame);
            _usedThisFrame += allowed;
            return allowed;
        }

        private bool PrefabIsPrimary(GameObject prefab)
        {
            if (!prefab) return false;
            var id = prefab.GetInstanceID();
            if (_primaryPrefab.TryGetValue(id, out var ok)) return ok;
            var pd = prefab.GetComponent<ProjectileDamage>();
            ok = pd && IsPrimary(pd.damageType);
            _primaryPrefab[id] = ok;
            return ok;
        }

        private Quaternion Fan(int i)
        {
            var fan = _fan;
            return (uint)i < (uint)fan.Length ? fan[i] : Quaternion.AngleAxis(MultiShot.FanAngle(i, _fanDegreesValue), Vector3.up);
        }

        // ---- balas ----

        private void OnBulletFire(On.RoR2.BulletAttack.orig_Fire_FireArgs orig, BulletAttack self, BulletAttack.FireArgs args)
        {
            orig(self, args);
            if (_holders.Count == 0) return; // ninguém tem o item: custo zero por tiro
            try
            {
                // Raios pré-calculados (ex.: Railgunner) não aceitam nova mira: sem extras.
                if (args.precollectedRaycasts != null || !IsPrimary(self.damageType)) return;
                var n = ExtrasFor(self.owner, self.origin);
                if (n == 0) return;
                var aim = self.aimVector;
                for (var i = 0; i < n; i++)
                {
                    self.aimVector = Fan(i) * aim;
                    orig(self, args);
                }
                self.aimVector = aim;
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // ---- projéteis ----

        private void OnFireProjectile(On.RoR2.Projectile.ProjectileManager.orig_FireProjectile_FireProjectileInfo orig, ProjectileManager self, FireProjectileInfo info)
        {
            orig(self, info);
            if (_holders.Count == 0) return;
            try
            {
                var primary = info.damageTypeOverride.HasValue ? IsPrimary(info.damageTypeOverride.Value) : PrefabIsPrimary(info.projectilePrefab);
                if (!primary) return;
                var n = ExtrasFor(info.owner, info.position);
                if (n == 0) return;
                var rotation = info.rotation;
                for (var i = 0; i < n; i++)
                {
                    info.rotation = Fan(i) * rotation; // struct: cópia local
                    orig(self, info);
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // ---- flechas da Huntress (orbs teleguiados, criados no host) ----

        private void OnAddOrb(On.RoR2.Orbs.OrbManager.orig_AddOrb orig, OrbManager self, Orb orb)
        {
            orig(self, orb);
            if (_holders.Count == 0) return;
            try
            {
                if (!(orb is GenericDamageOrb g) || !IsPrimary(g.damageType) || Clone == null) return;
                var n = ExtrasFor(g.attacker, g.origin);
                // Teleguiadas: os extras seguem o mesmo alvo (leque não se aplica).
                for (var i = 0; i < n; i++)
                    orig(self, (Orb)Clone(orb));
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }
}
