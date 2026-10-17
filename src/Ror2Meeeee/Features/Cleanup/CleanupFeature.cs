using System;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using EntityStates.Totem;
using RoR2;
using UnityEngine;

namespace Ror2Meeeee
{
    // Limpeza de coisas que pesam no FPS, sem thread e sem varredura:
    //  1) Cadáveres: usa o sistema do próprio jogo (Corpse). Padrão do jogo: até 25, apagados só FORA DA TELA
    //     (OutOfSight), 6 s de vida. Aqui: modo Hard (apaga mesmo à vista), máximo e tempo menores.
    //     Aplicado na ligação e no início de cada run (o jogo recarrega os convars salvos depois do Awake);
    //     valores originais restaurados ao desligar. Não grava nos convars do jogador.
    //  2) Totem dos Lynx (bug do jogo 1.5.0): SacrificeSuccess.FixedUpdate lê alliesToRapture[0] a cada tick;
    //     se o aliado já morreu, vira NullReferenceException por tick (600+ no log). Antes do tick, põe um aliado
    //     vivo na posição 0 ou, sem nenhum, uma âncora invisível sem HealthComponent (o sacrifício a ignora).
    //     Roda em toda máquina (clientes também executam o estado); só enquanto o Totem está nesse estado.
    public sealed class CleanupFeature : Feature
    {
        private static readonly FieldInfo MaxCorpsesField = typeof(Corpse).GetField("maxCorpses", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo DisposalField = typeof(Corpse).GetField("disposalMode", BindingFlags.Static | BindingFlags.NonPublic);

        private readonly ConfigEntry<int> _maxCorpses;
        private readonly ConfigEntry<float> _corpseLifetime;

        private bool _saved;
        private int _origMax;
        private Corpse.DisposalMode _origDisposal;
        private float _origLifetime;

        private static GameObject _totemAnchor;

        public CleanupFeature(Cfg cfg, ManualLogSource log) : base("Cleanup", cfg, log,
            "Limpeza para o FPS: cadáveres somem rápido, inclusive à vista (máximo e tempo abaixo), e corrige um erro do jogo no Totem dos Lynx que gerava centenas de exceções por partida.",
            enabledByDefault: true)
        {
            _maxCorpses = cfg.Bind("Cleanup", "MaxCorpses", 10, "Máximo de cadáveres no mapa (jogo: 25). Os mais antigos somem primeiro, mesmo à vista.");
            _corpseLifetime = cfg.Bind("Cleanup", "CorpseLifetime", 2f, "Segundos que um cadáver fica antes de sumir (jogo: 6).");
            _maxCorpses.SettingChanged += (_, __) => { if (Enabled) ApplyCorpses(); };
            _corpseLifetime.SettingChanged += (_, __) => { if (Enabled) ApplyCorpses(); };
        }

        public override void ResetRunState()
        {
            if (Enabled) ApplyCorpses(); // depois dos convars salvos do jogo
        }

        protected override void OnEnable()
        {
            Track("SacrificeSuccess.FixedUpdate (Totem Lynx)", () => On.EntityStates.Totem.SacrificeSuccess.FixedUpdate += OnSacrificeFixedUpdate);
            ApplyCorpses();
        }

        protected override void OnDisable()
        {
            On.EntityStates.Totem.SacrificeSuccess.FixedUpdate -= OnSacrificeFixedUpdate;
            RestoreCorpses();
        }

        // ---- cadáveres ----

        private void ApplyCorpses()
        {
            if (MaxCorpsesField == null || DisposalField == null) { Log.LogWarning("[Cleanup] campos de Corpse não encontrados (versão do jogo mudou?)."); return; }
            if (!_saved)
            {
                _origMax = (int)MaxCorpsesField.GetValue(null);
                _origDisposal = (Corpse.DisposalMode)DisposalField.GetValue(null);
                _origLifetime = Corpse.corpseLifeTime;
                _saved = true;
            }
            var max = Math.Max(0, _maxCorpses.Value);
            var life = Mathf.Max(0.5f, _corpseLifetime.Value);
            MaxCorpsesField.SetValue(null, max);
            DisposalField.SetValue(null, Corpse.DisposalMode.Hard);
            Corpse.corpseLifeTime = life;
            Log.LogInfo($"[Cleanup] cadáveres: máximo {max}, {life:F1} s, modo Hard");
        }

        private void RestoreCorpses()
        {
            if (!_saved || MaxCorpsesField == null || DisposalField == null) return;
            MaxCorpsesField.SetValue(null, _origMax);
            DisposalField.SetValue(null, _origDisposal);
            Corpse.corpseLifeTime = _origLifetime;
            _saved = false;
        }

        // ---- Totem dos Lynx ----

        private void OnSacrificeFixedUpdate(On.EntityStates.Totem.SacrificeSuccess.orig_FixedUpdate orig, SacrificeSuccess self)
        {
            try { FixAllies(self); }
            catch (Exception e) { Fail(e); }
            orig(self);
        }

        private static void FixAllies(SacrificeSuccess s)
        {
            var allies = s.alliesToRapture;
            if (allies != null && allies.Length > 0 && allies[0]) return; // caso normal: nada a fazer

            if (allies != null)
                for (var i = 1; i < allies.Length; i++)
                    if (allies[i])
                    {
                        allies[0] = allies[i]; // o sacrifício ainda o mata (percorre o array todo)
                        allies[i] = null;
                        return;
                    }

            if (allies == null || allies.Length == 0) s.alliesToRapture = allies = new GameObject[1];
            if (!_totemAnchor)
            {
                _totemAnchor = new GameObject("Ror2Meeeee_TotemAnchor");
                UnityEngine.Object.DontDestroyOnLoad(_totemAnchor);
            }
            var esm = s.outer; // a máquina de estados fica no próprio Totem
            if (esm) _totemAnchor.transform.position = esm.transform.position;
            allies[0] = _totemAnchor;
        }
    }
}
