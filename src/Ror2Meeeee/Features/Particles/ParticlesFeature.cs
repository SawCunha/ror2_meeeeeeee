using System;
using System.Text;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Limits;
using UnityEngine;

namespace Ror2Meeeee
{
    // Teto por segundo, por tipo de efeito, para TODOS os efeitos visuais, menos as exceções (nomes de prefab na config).
    // Efeitos da lista Raised usam um teto próprio, maior (ex.: explosões: com isenção total passavam de 190 mil por run).
    // Ponto: EffectManager.SpawnEffect(EffectIndex, EffectData, transmit). Efeito é só visual/som: pular orig não
    // toca no gameplay. transmit=true = a máquina que originou o efeito: acima do teto não mostra NEM envia, então
    // quem tem o mod manda menos efeitos para os outros (host e clientes). transmit=false = efeito recebido pela rede
    // (no host, os dele voltam por aqui via conexão local): teto próprio, para não contar o mesmo efeito duas vezes.
    // O host só retransmite efeitos de clientes (HandleEffectServer -> TransmitEffect, sem passar aqui): esses já
    // vêm limitados do cliente de origem, que também tem o mod.
    public sealed class ParticlesFeature : Feature
    {
        private readonly ConfigEntry<string> _list;
        private readonly ConfigEntry<int> _max;
        private readonly ConfigEntry<string> _raisedList;
        private readonly ConfigEntry<int> _raisedMax;
        private readonly ConfigEntry<bool> _count;

        private int[] _cap;        // por EffectIndex: teto por segundo; 0 = exceção (sem teto); null = não resolvido
        private BudgetWindow[] _sentWin;  // por EffectIndex: efeitos originados aqui (transmit=true)
        private BudgetWindow[] _shownWin; // por EffectIndex: efeitos recebidos pela rede (transmit=false)
        private int _maxPerSecond;
        private int _cut;          // cortados pelo teto desde o último relatório de fase
        private int[] _counts;     // por EffectIndex; só com CountSpawns
        private bool _resolved;
        private bool _countOn;
        private bool _hooked; // SpawnEffect é muito chamado: sem teto nem contagem, não há hook

        public ParticlesFeature(Cfg cfg, ManualLogSource log) : base("Particles", cfg, log,
            "Particle cap: limits how many times per second each type of effect appears (all of them, except those in the Exempt list). Applies to what you show and to what you send to others (host and clients with the mod). Does not change damage.", "Teto de partículas: limita quantas vezes por segundo cada tipo de efeito aparece (todos, menos os da lista Exempt). Vale para o que você mostra e para o que você envia aos outros (host e clientes com o mod). Não muda dano.",
            enabledByDefault: true)
        {
            _list = cfg.Bind("Particles", "Exempt", "BladeComboSwingEffect",
                "Effects that are NEVER limited, comma-separated (default: Mercenary strikes). All others follow MaxPerSecond (or RaisedMaxPerSecond, if they are in Raised). To find the names, turn on CountSpawns and use the ror2meeeee_effects command.", "Efeitos que NUNCA são limitados, separados por vírgula (padrão: golpes do Mercenary). Todos os outros seguem MaxPerSecond (ou RaisedMaxPerSecond, se estiverem em Raised). Para descobrir os nomes, ligue CountSpawns e use o comando ror2meeeee_effects.");
            _max = cfg.Bind("Particles", "MaxPerSecond", 10,
                "Maximum times per second each type of effect appears/is sent (each type has its own count); the excess is neither shown nor sent. 0 = no cap.", "Máximo de vezes por segundo que cada tipo de efeito aparece/é enviado (cada tipo tem a sua conta); o excesso não é mostrado nem enviado. 0 = sem teto.");
            _raisedList = cfg.Bind("Particles", "Raised", "OmniExplosionVFXQuick",
                "Effects with their own higher cap (RaisedMaxPerSecond), comma-separated (default: explosions). If the same name is in Exempt, Exempt wins.", "Efeitos com teto próprio, maior (RaisedMaxPerSecond), separados por vírgula (padrão: explosões). Se o mesmo nome estiver em Exempt, vale Exempt.");
            _raisedMax = cfg.Bind("Particles", "RaisedMaxPerSecond", 60,
                "Maximum per second for each effect in the Raised list. 0 = no cap for them.", "Máximo por segundo para cada efeito da lista Raised. 0 = sem teto para eles.");
            _count = cfg.Bind("Particles", "CountSpawns", false,
                "Counts how many times each effect appears, to find which ones weigh the most (ror2meeeee_effects command; the list is also printed to the console/log on each new stage). Small cost; turn it off after choosing.", "Conta quantas vezes cada efeito aparece, para descobrir quais mais pesam (comando ror2meeeee_effects; a lista também sai no console/log a cada fase nova). Custo pequeno; desligue depois de escolher.");
            _list.SettingChanged += (_, __) => { _resolved = false; SyncHook(); };
            _raisedList.SettingChanged += (_, __) => _resolved = false;
            _raisedMax.SettingChanged += (_, __) => _resolved = false;
            _count.SettingChanged += (_, __) => { _countOn = _count.Value; _counts = null; SyncHook(); };
            _max.SettingChanged += (_, __) => { _maxPerSecond = _max.Value; _resolved = false; SyncHook(); };
            _countOn = _count.Value;
            _maxPerSecond = _max.Value;
        }

        public override void ResetRunState() { _counts = null; _cut = 0; }

        protected override void OnEnable()
        {
            _resolved = false;
            Track("Stage.onStageStartGlobal", () => Stage.onStageStartGlobal += OnStageStart);
            if (!HookNeeded()) return; // registrado depois, se a lista ou CountSpawns mudar
            Track("EffectManager.SpawnEffect", () => On.RoR2.EffectManager.SpawnEffect_EffectIndex_EffectData_bool += OnSpawnEffect);
            _hooked = true;
        }

        protected override void OnDisable()
        {
            Stage.onStageStartGlobal -= OnStageStart;
            On.RoR2.EffectManager.SpawnEffect_EffectIndex_EffectData_bool -= OnSpawnEffect;
            _hooked = false;
        }

        private bool HookNeeded() => _countOn || _maxPerSecond > 0;

        private void SyncHook()
        {
            if (!Enabled) return;
            try
            {
                var want = HookNeeded();
                if (want == _hooked) return;
                if (want) On.RoR2.EffectManager.SpawnEffect_EffectIndex_EffectData_bool += OnSpawnEffect;
                else On.RoR2.EffectManager.SpawnEffect_EffectIndex_EffectData_bool -= OnSpawnEffect;
                _hooked = want;
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnSpawnEffect(On.RoR2.EffectManager.orig_SpawnEffect_EffectIndex_EffectData_bool orig,
            EffectIndex effectIndex, EffectData effectData, bool transmit)
        {
            try
            {
                if (!_resolved) Resolve();
                var i = (int)effectIndex;
                if (_countOn)
                {
                    if (_counts == null && EffectCatalog.availability.available) _counts = new int[EffectCatalog.effectCount];
                    if (_counts != null && (uint)i < (uint)_counts.Length) _counts[i]++;
                }
                // EffectIndex.Invalid (-1, som puro) fica fora pelo teste de faixa.
                if (_maxPerSecond > 0 && _cap != null && (uint)i < (uint)_cap.Length && _cap[i] > 0
                    && !ProjectileBudget.Decide(ref (transmit ? _sentWin : _shownWin)[i], Time.unscaledTime, _cap[i], 0f, out _))
                {
                    _cut++;
                    return; // só visual/som: sem dano; com transmit=true também não vai para os outros
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(effectIndex, effectData, transmit);
        }

        // Nome -> índice, uma vez (catálogo não existe no Awake). Nunca por string a cada efeito.
        private void Resolve()
        {
            if (!EffectCatalog.availability.available) return;
            _resolved = true;
            var n = EffectCatalog.effectCount;
            var cap = new int[n];
            for (var i = 0; i < n; i++) cap[i] = _maxPerSecond;
            // Raised antes de Exempt: um nome nas duas listas fica sem teto.
            var raised = Apply(cap, _raisedList.Value, Math.Max(0, _raisedMax.Value));
            var exempt = Apply(cap, _list.Value, 0);
            // Janelas só no primeiro Resolve: re-resolver (config mudou) não zera as contagens em andamento.
            if (_sentWin == null || _sentWin.Length != n) { _sentWin = new BudgetWindow[n]; _shownWin = new BudgetWindow[n]; }
            _cap = cap;
            Log.LogInfo($"[Particles] teto de {_maxPerSecond}/s por tipo de efeito; {raised} com teto de {_raisedMax.Value}/s; {exempt} exceção(ões).");
        }

        // Aplica `value` aos efeitos da lista (nomes de prefab separados por vírgula); devolve quantos achou.
        private int Apply(int[] cap, string list, int value)
        {
            var found = 0;
            foreach (var raw in (list ?? "").Split(','))
            {
                var name = raw.Trim();
                if (name.Length == 0) continue;
                var idx = EffectCatalog.FindEffectIndex(name);
                if (idx == EffectIndex.Invalid) { Log.LogWarning($"[Particles] efeito '{name}' não encontrado; ignorado."); continue; }
                cap[(int)idx] = value;
                found++;
            }
            return found;
        }

        // A cada fase nova, a mesma saída do ror2meeeee_effects (acumulado da run), só com CountSpawns e dados.
        private void OnStageStart(Stage stage)
        {
            try
            {
                var stages = Run.instance ? Run.instance.stageClearCount : 0;
                if (_cut > 0) Log.LogInfo($"[Particles] teto de {_maxPerSecond}/s: {_cut} efeito(s) não mostrados/enviados na fase anterior.");
                _cut = 0;
                if (!_countOn || _counts == null) return;
                Debug.Log($"[Particles] efeitos mais frequentes após {stages} fase(s):\n{DumpCounts(20)}");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Para ror2meeeee_effects: os mais frequentes desde o início da run (com CountSpawns).
        internal string DumpCounts(int top)
        {
            if (!_countOn || _counts == null) return "Particles.CountSpawns desligado ou sem dados (ligue, reinicie a run).";
            var sb = new StringBuilder();
            var used = new bool[_counts.Length];
            for (var k = 0; k < top; k++)
            {
                var best = -1;
                for (var i = 0; i < _counts.Length; i++)
                    if (!used[i] && _counts[i] > 0 && (best < 0 || _counts[i] > _counts[best])) best = i;
                if (best < 0) break;
                used[best] = true;
                var def = EffectCatalog.GetEffectDef((EffectIndex)best);
                sb.Append(_counts[best]).Append("  ").Append(def != null ? def.prefabName : "#" + best).Append('\n');
            }
            return sb.Length == 0 ? "nenhum efeito contado ainda." : sb.ToString().TrimEnd('\n');
        }
    }
}
