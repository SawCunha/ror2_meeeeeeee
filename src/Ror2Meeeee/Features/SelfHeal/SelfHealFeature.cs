using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Stats;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Itens "Fungo Egoísta" (verde) e "Fungo Egoísta do Vazio" (void verde, corrompe o primeiro): regeneração fixa por
    // segundo (cresce com o nível) + % da vida máxima por cópia extra (Core.Stats.SelfHeal). Curam SEMPRE, a cada
    // 0,25 s, só o dono e os minions dele (drones, torretas...), onde estiverem. Sem zona de cura. Só no host.
    // Não cura quem já está com vida cheia (evita eventos de cura à toa, que alimentam itens como as Lâminas Fantasmas).
    // Visual: Vazio = buff visual MushroomVoidActive do jogo (aura roxa do Weeping Fungus, sincronizada pelo jogo);
    // Egoísta = brilho do Medkit a cada 3 s. Ambos só enquanto está curando de fato.
    // Custo: o tick (onFixedUpdate) só existe enquanto alguém tem um dos itens; a lista é mantida por eventos.
    public sealed class SelfHealFeature : Feature
    {
        public const string ItemName = "Ror2MeeeeeSelfishFungus";
        public const string Token = "ROR2MEEEEE_ITEM_SELFISHFUNGUS";
        public const string VoidName = "Ror2MeeeeeSelfishFungusVoid";
        public const string VoidToken = "ROR2MEEEEE_ITEM_SELFISHFUNGUSVOID";
        private const float Interval = 0.25f;
        private const int GlowEveryTicks = 12; // 3 s

        private static ItemDef _def, _voidDef;
        private static ItemIndex _index = ItemIndex.None, _voidIndex = ItemIndex.None;

        private static ItemIndex Index
        {
            get
            {
                if (_index == ItemIndex.None && _def) _index = _def.itemIndex;
                return _index;
            }
        }

        private static ItemIndex VoidIndex
        {
            get
            {
                if (_voidIndex == ItemIndex.None && _voidDef) _voidIndex = _voidDef.itemIndex;
                return _voidIndex;
            }
        }

        private readonly List<CharacterBody> _holders = new List<CharacterBody>();
        private readonly HashSet<CharacterBody> _voidAura = new HashSet<CharacterBody>(); // buff visual posto pelo mod
        private bool _ticking;
        private float _acc;
        private int _tick;
        private GameObject _glow;

        public static void Register(ManualLogSource log)
        {
            var pt = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
            string P(float f) => (f * 100f).ToString("0.##", pt) + "%";
            string N(float f) => f.ToString("0.#", pt);
            var lvl = P(SelfHeal.LevelScaling);
            string Desc(float flat, float perStack) =>
                $"Regenera <style=cIsHealing>{N(flat)} de vida</style> por segundo <style=cStack>(+{lvl} por nível)</style> " +
                $"e mais <style=cIsHealing>{P(perStack)} da vida máxima</style> por segundo <style=cStack>por acúmulo extra</style>, " +
                "<style=cIsUtility>sempre, andando ou parado</style>. Cura também <style=cIsUtility>seus drones, torretas e outros ajudantes</style>, " +
                "onde estiverem. <style=cDeath>Não cura outros jogadores.</style>";

            _def = ModItem.Register(ItemName, Token, ItemTier.Tier2, new[] { ItemTag.Healing, ItemTag.AIBlacklist },
                "fungo_egoista.png", new Color32(120, 200, 90, 255),
                "Fungo Egoísta",
                $"Regenera {N(SelfHeal.SelfishFlat)} de vida/s, sempre, em você e nos seus drones e torretas. Só vocês.",
                Desc(SelfHeal.SelfishFlat, SelfHeal.SelfishPerStack),
                "Dividir é para os fracos.",
                log, "RoR2/Base/Mushroom/PickupMushroom.prefab");
            _voidDef = ModItem.Register(VoidName, VoidToken, ItemTier.VoidTier2, new[] { ItemTag.Healing, ItemTag.AIBlacklist },
                "fungo_egoista_vazio.png", new Color32(170, 70, 200, 255),
                "Fungo Egoísta do Vazio",
                $"Regenera {N(SelfHeal.VoidFlat)} de vida/s, sempre, em você e nos seus drones e torretas. <style=cIsVoid>Corrompe todos os Fungos Egoístas.</style>",
                Desc(SelfHeal.VoidFlat, SelfHeal.VoidPerStack) + " <style=cIsVoid>Corrompe todos os Fungos Egoístas.</style>",
                "Tudo o que é seu continua seu. Só que agora é do Vazio também.",
                log, "RoR2/DLC1/MushroomVoid/PickupMushroomVoid.prefab");
            ModItem.Corrupt(_def, _voidDef, log);
        }

        public SelfHealFeature(Cfg cfg, ManualLogSource log) : base("SelfishFungus", cfg, log,
            "Itens Fungo Egoísta (verde, 5 de vida/s) e Fungo Egoísta do Vazio (void, 15/s), mais % da vida por cópia extra: curam sempre, só você e seus minions. Desligado: os itens existem mas não fazem nada.",
            enabledByDefault: true)
        {
        }

        public override void ResetRunState() => ClearAll();

        protected override void OnEnable()
        {
            Track("CharacterBody.onBodyInventoryChangedGlobal", () => CharacterBody.onBodyInventoryChangedGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyStartGlobal", () => CharacterBody.onBodyStartGlobal += OnHolderMaybeChanged);
        }

        protected override void OnDisable()
        {
            CharacterBody.onBodyInventoryChangedGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyStartGlobal -= OnHolderMaybeChanged;
            ClearAll();
        }

        private void ClearAll()
        {
            foreach (var b in _voidAura) if (b && NetworkServer.active) b.RemoveBuff(DLC1Content.Buffs.MushroomVoidActive);
            _voidAura.Clear();
            _holders.Clear();
            SetTicking(false);
        }

        private void SetTicking(bool on)
        {
            if (on == _ticking) return;
            if (on) RoR2Application.onFixedUpdate += OnFixedUpdate;
            else RoR2Application.onFixedUpdate -= OnFixedUpdate;
            _ticking = on;
            _acc = 0f;
        }

        private static int Count(Inventory inv, ItemIndex idx) => inv && idx != ItemIndex.None ? inv.GetItemCountEffective(idx) : 0;

        private void OnHolderMaybeChanged(CharacterBody body)
        {
            try
            {
                if (!NetworkServer.active || !body) return;
                var inv = body.inventory;
                var has = Count(inv, Index) > 0 || Count(inv, VoidIndex) > 0;
                var at = _holders.IndexOf(body);
                if (has && at < 0) _holders.Add(body);
                else if (!has && at >= 0)
                {
                    _holders.RemoveAt(at);
                    SetVoidAura(body, false);
                }
                SetTicking(_holders.Count > 0);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnFixedUpdate()
        {
            try
            {
                _acc += Time.fixedDeltaTime;
                if (_acc < Interval) return;
                _acc = 0f;
                if (!NetworkServer.active) { ClearAll(); return; }
                var glow = ++_tick % GlowEveryTicks == 0;
                for (var i = _holders.Count - 1; i >= 0; i--)
                {
                    var body = _holders[i];
                    if (!body) { _holders.RemoveAt(i); continue; } // corpo destruído (morte, troca de estágio)
                    var inv = body.inventory;
                    var selfish = Count(inv, Index);
                    var voidStacks = Count(inv, VoidIndex);
                    var healing = Heal(body, selfish, voidStacks);
                    healing |= HealMinions(body.master, selfish, voidStacks);
                    SetVoidAura(body, healing && voidStacks > 0);
                    if (glow && healing && selfish > 0) Glow(body);
                }
                _voidAura.RemoveWhere(b => !b); // corpos destruídos levam o buff junto
                if (_holders.Count == 0) SetTicking(false);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // true se curou (estava vivo e abaixo da vida cheia). Cópias do dono; vida máxima e nível de quem é curado.
        private static bool Heal(CharacterBody body, int selfish, int voidStacks)
        {
            var hc = body ? body.healthComponent : null;
            if (!hc || !hc.alive || hc.health >= hc.fullHealth) return false;
            var amount = SelfHeal.AmountPerTick(selfish, voidStacks, hc.fullHealth, body.level, Interval);
            if (amount <= 0f) return false;
            hc.Heal(amount, default(ProcChainMask));
            return true;
        }

        private static bool HealMinions(CharacterMaster owner, int selfish, int voidStacks)
        {
            if (!owner) return false;
            var group = MinionOwnership.MinionGroup.FindGroup(owner.netId);
            if (group == null) return false;
            var any = false;
            var members = group.members;
            for (var k = 0; k < group.memberCount; k++)
            {
                var m = members[k] ? members[k].GetComponent<CharacterMaster>() : null;
                any |= Heal(m ? m.GetBody() : null, selfish, voidStacks);
            }
            return any;
        }

        private void SetVoidAura(CharacterBody body, bool on)
        {
            if (on == _voidAura.Contains(body)) return;
            if (on)
            {
                body.AddBuff(DLC1Content.Buffs.MushroomVoidActive);
                _voidAura.Add(body);
            }
            else
            {
                if (body) body.RemoveBuff(DLC1Content.Buffs.MushroomVoidActive);
                _voidAura.Remove(body);
            }
        }

        private void Glow(CharacterBody body)
        {
            if (!_glow) _glow = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MedkitHealEffect");
            if (!_glow) return;
            var data = new EffectData { origin = body.transform.position };
            data.SetNetworkedObjectReference(body.gameObject);
            EffectManager.SpawnEffect(_glow, data, transmit: true);
        }
    }
}
