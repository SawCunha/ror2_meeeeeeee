using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Stats;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Item verde "Bateria do Portal": as zonas de carga (teleportador, células do Void Fields, pilares da Lua, Void Seeds)
    // carregam +10% mais rápido por cópia somada do time (Core.Stats.PortalBattery). Usa o ponto oficial do jogo,
    // HoldoutZoneController.calcChargeRate (o mesmo do Focused Convergence). Como os outros itens, só com portador: a
    // lista é mantida por eventos de inventário/corpo (host); com alguém nela, inscreve nas zonas ativas e liga os hooks de
    // zona ligada/desligada; sem ninguém, desfaz tudo. A carga é calculada no host.
    public sealed class PortalBatteryFeature : Feature
    {
        public const string ItemName = "Ror2MeeeeePortalBattery";
        public const string Token = "ROR2MEEEEE_ITEM_PORTALBATTERY";

        private static ItemDef _def;
        private static ItemIndex _index = ItemIndex.None;

        private static ItemIndex Index
        {
            get
            {
                if (_index == ItemIndex.None && _def) _index = _def.itemIndex;
                return _index;
            }
        }

        private readonly HoldoutZoneController.CalcChargeRateDelegate _applyRate;
        private readonly HashSet<GameObject> _holders = new HashSet<GameObject>();
        private bool _hooked;

        public static void Register(ManualLogSource log)
        {
            var pt = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
            var p = (PortalBattery.ChargePerStack * 100f).ToString("0.##", pt) + "%";
            _def = ModItem.Register(ItemName, Token, ItemTier.Tier2, new[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.HoldoutZoneRelated },
                "bateria_portal.png", new Color32(240, 200, 60, 255),
                "Bateria do Portal",
                "O teleportador carrega mais rápido.",
                $"O <style=cIsUtility>teleportador</style> e outras zonas de carga carregam <style=cIsUtility>{p} mais rápido</style> " +
                $"<style=cStack>(+{p} por acúmulo)</style>. As cópias de todo o time somam.",
                "Energia emprestada de algum lugar que ainda não percebeu.",
                log, "RoR2/Base/EquipmentMagazine/PickupBattery.prefab");
        }

        public PortalBatteryFeature(Cfg cfg, ManualLogSource log) : base("PortalBattery", cfg, log,
            "Item Bateria do Portal (verde): zonas de carga (teleportador etc.) carregam +10% mais rápido por cópia do time. Desligado: o item existe mas não faz nada.",
            enabledByDefault: true)
        {
            _applyRate = ApplyRate;
        }

        protected override void OnEnable()
        {
            Track("CharacterBody.onBodyInventoryChangedGlobal", () => CharacterBody.onBodyInventoryChangedGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyStartGlobal", () => CharacterBody.onBodyStartGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyDestroyGlobal", () => CharacterBody.onBodyDestroyGlobal += OnBodyDestroy);
            // HoldoutZoneController OnEnable/OnDisable e calcChargeRate: ligados por SetHook quando surge um portador.
        }

        protected override void OnDisable()
        {
            CharacterBody.onBodyInventoryChangedGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyStartGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyDestroyGlobal -= OnBodyDestroy;
            _holders.Clear();
            SetHook(false);
        }

        public override void ResetRunState()
        {
            _holders.Clear();
            SetHook(false);
        }

        private void SetHook(bool on)
        {
            if (on == _hooked) return;
            if (on)
            {
                On.RoR2.HoldoutZoneController.OnEnable += OnZoneEnable;
                On.RoR2.HoldoutZoneController.OnDisable += OnZoneDisable;
            }
            else
            {
                On.RoR2.HoldoutZoneController.OnEnable -= OnZoneEnable;
                On.RoR2.HoldoutZoneController.OnDisable -= OnZoneDisable;
            }
            // Zonas já ativas (ex.: alguém pega o item com o teleportador carregando).
            foreach (var zone in InstanceTracker.GetInstancesList<HoldoutZoneController>())
            {
                if (!zone) continue;
                zone.calcChargeRate -= _applyRate;
                if (on) zone.calcChargeRate += _applyRate;
            }
            _hooked = on;
            Log.LogInfo($"[PortalBattery] hooks de carga {(on ? "ligados (há portador)" : "desligados (ninguém com o item)")}");
        }

        private void OnHolderMaybeChanged(CharacterBody body)
        {
            try
            {
                var idx = Index;
                if (idx == ItemIndex.None || !NetworkServer.active || !body) return;
                var inv = body.inventory;
                if (inv && inv.GetItemCountEffective(idx) > 0) { if (_holders.Add(body.gameObject)) SetHook(true); }
                else if (_holders.Remove(body.gameObject)) SetHook(_holders.Count > 0);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnBodyDestroy(CharacterBody body)
        {
            if (_holders.Count > 0 && body && _holders.Remove(body.gameObject)) SetHook(_holders.Count > 0);
        }

        private void OnZoneEnable(On.RoR2.HoldoutZoneController.orig_OnEnable orig, HoldoutZoneController self)
        {
            orig(self);
            if (self) { self.calcChargeRate -= _applyRate; self.calcChargeRate += _applyRate; }
        }

        private void OnZoneDisable(On.RoR2.HoldoutZoneController.orig_OnDisable orig, HoldoutZoneController self)
        {
            if (self) self.calcChargeRate -= _applyRate;
            orig(self);
        }

        // Chamado pelo jogo a cada tick enquanto a zona carrega.
        private void ApplyRate(ref float rate)
        {
            try
            {
                var idx = Index;
                if (!NetworkServer.active || idx == ItemIndex.None) return;
                var stacks = Util.GetItemCountForTeam(TeamIndex.Player, idx, requiresAlive: true, requiresConnected: true);
                if (stacks > 0) rate *= PortalBattery.ChargeRateMultiplier(stacks);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }
}
