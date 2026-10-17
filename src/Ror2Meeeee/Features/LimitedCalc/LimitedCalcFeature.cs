using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Items;
using Ror2Meeeee.Core.Limits;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Limita os stacks que o jogo "vê" nos itens da lista. Tudo desligado por padrão (Mode=Off):
    // limites ainda não vieram da linha de base (Story 1.6); os valores padrão são provisórios.
    // Novo item = nova Entry em _entries + seu hook/handler; Feature e Core não mudam (FR-5).
    public sealed class LimitedCalcFeature : Feature
    {
        private sealed class Entry
        {
            public string Name;
            public Func<ItemDef> Item; // resolvido sob demanda: catálogos não existem no Awake
            public ConfigEntry<StackMode> Mode;
            public ConfigEntry<int> Limit;
            public volatile IStackPolicy Policy = StackPolicy.Off; // trocada só em SettingChanged
            public Action Register;
            public Action Unregister;
            public bool Hooked; // hook só existe com Mode != Off: Off não paga o hook no FixedUpdate
        }

        private readonly Entry _fungus;
        private readonly Entry _fireworks;
        private readonly Entry _tome;
        private float _tomeFactor = 1f; // fator da última rolagem do Tome; lido no spawn do pacote da mesma morte
        private readonly Entry[] _entries;

        public LimitedCalcFeature(Cfg cfg, ManualLogSource log) : base("LimitedCalc", cfg, log,
            "Cálculo limitado: reduz o peso no FPS de itens com muitas cópias (Bustling Fungus, Bundle of Fireworks e Ghor's Tome) sem tirar o efeito. Escolha o modo de cada item nas seções LimitedCalc.*.",
            enabledByDefault: true)
        {
            _fungus = NewEntry("BustlingFungus", StackMode.Off, 10, () => RoR2Content.Items.Mushroom,
                () => On.RoR2.Items.MushroomBodyBehavior.FixedUpdate += OnMushroomFixedUpdate,
                () => On.RoR2.Items.MushroomBodyBehavior.FixedUpdate -= OnMushroomFixedUpdate);
            _fireworks = NewEntry("BundleOfFireworks", StackMode.Aggregate, 20, () => RoR2Content.Items.Firework,
                () => On.RoR2.FireworkLauncher.FixedUpdate += OnFireworkFixedUpdate,
                () => On.RoR2.FireworkLauncher.FixedUpdate -= OnFireworkFixedUpdate);
            _tome = NewEntry("GhorsTome", StackMode.Aggregate, 5, () => RoR2Content.Items.BonusGoldPackOnKill,
                () => IL.RoR2.GlobalEventManager.OnCharacterDeath += ILTomeOnCharacterDeath,
                () => IL.RoR2.GlobalEventManager.OnCharacterDeath -= ILTomeOnCharacterDeath);
            _entries = new[] { _fungus, _fireworks, _tome };
        }

        private Entry NewEntry(string name, StackMode defaultMode, int defaultLimit, Func<ItemDef> item, Action register, Action unregister)
        {
            var section = "LimitedCalc." + name;
            var e = new Entry
            {
                Name = name,
                Item = item,
                Register = register,
                Unregister = unregister,
                Mode = Cfg.Bind(section, "Mode", defaultMode, "Off = como no jogo. Clamp = o item funciona como se tivesse no máximo Limit cópias. Aggregate = Fireworks: menos mísseis, cada um mais forte, mesmo dano total; Ghor's Tome: menos pacotes de ouro, cada um vale mais, mesmo ouro esperado; no Fungus é igual a Clamp."),
                Limit = Cfg.Bind(section, "Limit", defaultLimit, "Máximo de cópias consideradas nos modos Clamp e Aggregate (mínimo 1)."),
            };
            e.Mode.SettingChanged += (_, __) => Refresh(e);
            e.Limit.SettingChanged += (_, __) => Refresh(e);
            Refresh(e);
            return e;
        }

        private void Refresh(Entry e)
        {
            e.Policy = StackPolicy.Create(e.Mode.Value, e.Limit.Value);
            Log.LogInfo($"[LimitedCalc] {e.Name}: Mode={e.Mode.Value}, Limit={e.Limit.Value}");
            if (!Enabled) return;
            try { SyncHook(e); }
            catch (Exception ex) { Fail(ex); }
        }

        // Registra/remove o hook da entrada conforme o Mode. Fora de OnEnable: entre partidas ou via reload.
        private void SyncHook(Entry e)
        {
            var want = e.Mode.Value != StackMode.Off;
            if (want == e.Hooked) return;
            if (want) e.Register(); else e.Unregister();
            e.Hooked = want;
        }

        // true se o item está sob cálculo limitado (Mode != Off); o teto de stacks da multiplicação não vale para ele.
        public bool Covers(ItemDef item)
        {
            if (!Enabled || !item) return false;
            foreach (var e in _entries)
                if (e.Mode.Value != StackMode.Off && e.Item() == item) return true;
            return false;
        }

        public override void ResetRunState() { } // sem cache por run

        protected override void OnEnable()
        {
            foreach (var e in _entries)
            {
                if (e.Mode.Value == StackMode.Off) continue; // registrado depois, se o Mode mudar
                Track(e.Name, e.Register);
                e.Hooked = true;
            }
        }

        protected override void OnDisable()
        {
            foreach (var e in _entries)
            {
                e.Unregister(); // idempotente: -= sem hook não faz nada
                e.Hooked = false;
            }
        }

        // Fungus: o jogo lê `stack` do behavior no FixedUpdate; enxerga o valor limitado só durante a chamada.
        private void OnMushroomFixedUpdate(On.RoR2.Items.MushroomBodyBehavior.orig_FixedUpdate orig, MushroomBodyBehavior self)
        {
            var real = self.stack;
            try
            {
                if (NetworkServer.active) self.stack = _fungus.Policy.Apply(real);
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
            try { orig(self); }
            finally { self.stack = real; }
        }

        // Fireworks: remaining = 4 + 4*stack no disparo; limita ao teto equivalente ao Limit.
        // Clamp: corta os mísseis (e o dano total). Aggregate: mesmo teto, mas cada míssil leva
        // damageCoefficient * remaining/cap, então o dano total do disparo é preservado.
        // Idempotente por tick: depois do ajuste remaining <= cap.
        private void OnFireworkFixedUpdate(On.RoR2.FireworkLauncher.orig_FixedUpdate orig, FireworkLauncher self)
        {
            try
            {
                var policy = _fireworks.Policy;
                if (NetworkServer.active && policy.Limit != int.MaxValue)
                {
                    var cap = 4 + 4 * policy.Limit;
                    if (self.remaining > cap)
                    {
                        var before = self.remaining;
                        var coefBefore = self.damageCoefficient;
                        if (_fireworks.Mode.Value == StackMode.Aggregate)
                            self.damageCoefficient *= (float)before / cap;
                        self.remaining = cap;
                        // Uma linha por interação (não por tick): só quando o teto foi aplicado.
                        Log.LogInfo($"[LimitedCalc] BundleOfFireworks {_fireworks.Mode.Value}: mísseis {before} -> {cap}, dano por míssil x{self.damageCoefficient / coefBefore:F2}");
                    }
                }
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
            orig(self);
        }

        // Ghor's Tome: no OnCharacterDeath o jogo rola 4% * stack e solta 1 BonusMoneyPack por morte.
        // IL: limita o stack lido para a rolagem e, no Aggregate, multiplica o ouro do pacote criado
        // pela razão chance real / chance limitada (mesmo ouro esperado, menos objetos no chão).
        // Corpo mudou (patch do jogo/outro mod) = loga e não altera nada; o resto da feature segue.
        private void ILTomeOnCharacterDeath(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After,
                    x => x.MatchLdsfld(typeof(RoR2Content.Items), nameof(RoR2Content.Items.BonusGoldPackOnKill)),
                    x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective)))
                || !c.Clone().TryGotoNext(x => x.MatchLdstr("Prefabs/NetworkedObjects/BonusMoneyPack")))
            {
                Log.LogError("[LimitedCalc] GhorsTome: trecho do OnCharacterDeath não encontrado; IL hook não aplicado.");
                return;
            }
            c.EmitDelegate<Func<int, int>>(CapTomeStack);
            c.GotoNext(x => x.MatchLdstr("Prefabs/NetworkedObjects/BonusMoneyPack"));
            if (!c.TryGotoNext(MoveType.After, x => x.MatchCall(out var m) && m.Name == "Instantiate"))
            {
                Log.LogError("[LimitedCalc] GhorsTome: Instantiate do pacote não encontrado; só a chance foi limitada.");
                return;
            }
            c.EmitDelegate<Func<GameObject, GameObject>>(ScaleTomePack);
        }

        private int CapTomeStack(int stack)
        {
            _tomeFactor = 1f;
            try
            {
                if (!NetworkServer.active || stack <= 0) return stack;
                var capped = _tome.Policy.Apply(stack);
                if (_tome.Mode.Value == StackMode.Aggregate)
                    _tomeFactor = ChanceAggregate.Factor(stack, capped, 4f);
                return capped;
            }
            catch (Exception ex)
            {
                Fail(ex);
                return stack;
            }
        }

        // Roda antes do Start do MoneyPickup, que calcula o ouro a partir de baseGoldReward.
        private GameObject ScaleTomePack(GameObject pack)
        {
            try
            {
                if (_tomeFactor > 1f && pack)
                {
                    var money = pack.GetComponentInChildren<MoneyPickup>();
                    if (money)
                    {
                        var before = money.baseGoldReward;
                        money.baseGoldReward = (int)Math.Round(before * _tomeFactor);
                        Log.LogInfo($"[LimitedCalc] GhorsTome Aggregate: pacote {before} -> {money.baseGoldReward} (x{_tomeFactor:F2})");
                    }
                }
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
            _tomeFactor = 1f;
            return pack;
        }
    }
}
