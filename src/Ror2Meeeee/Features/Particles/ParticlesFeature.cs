using System;
using System.Text;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;

namespace Ror2Meeeee
{
    // Corta efeitos visuais por nome de prefab (lista na config), só localmente (por cliente).
    // Ponto: EffectManager.SpawnEffect(EffectIndex, EffectData, transmit). Efeitos vindos da rede (e os do host,
    // que também chegam pelo cliente local) entram com transmit=false; cortar aí não toca na rede nem no
    // gameplay (SpawnEffect só instancia o visual e o som local). Chamadas com transmit=true NÃO são cortadas:
    // pular orig impediria o envio aos outros jogadores. Limitação: efeitos que o próprio cliente origina.
    // Só ação Disable; "Shorten" fica adiado até a medição (story 5.1) mostrar ganho.
    public sealed class ParticlesFeature : Feature
    {
        private readonly ConfigEntry<string> _list;
        private readonly ConfigEntry<bool> _count;

        private bool[] _blocked;   // por EffectIndex; null = lista vazia / não resolvida
        private int[] _counts;     // por EffectIndex; só com CountSpawns
        private bool _resolved;
        private bool _countOn;
        private bool _hasBlocked;
        private bool _hooked; // SpawnEffect é muito chamado: sem lista nem contagem, não há hook

        public ParticlesFeature(Cfg cfg, ManualLogSource log) : base("Particles", cfg, log,
            "Corte de partículas: esconde efeitos visuais escolhidos (lista em Disable) só na sua tela, para ganhar FPS. Não muda dano nem o que os outros jogadores veem.",
            enabledByDefault: true)
        {
            _list = cfg.Bind("Particles", "Disable", "",
                "Nomes dos efeitos a esconder, separados por vírgula. Para descobrir os nomes, ligue CountSpawns e use o comando ror2meeeee_effects. Vazio = nada é escondido.");
            _count = cfg.Bind("Particles", "CountSpawns", false,
                "Conta quantas vezes cada efeito aparece, para descobrir quais mais pesam (comando ror2meeeee_effects). Custo pequeno; desligue depois de escolher.");
            _list.SettingChanged += (_, __) => { _resolved = false; SyncHook(); };
            _count.SettingChanged += (_, __) => { _countOn = _count.Value; _counts = null; SyncHook(); };
            _countOn = _count.Value;
        }

        public override void ResetRunState() { _counts = null; }

        protected override void OnEnable()
        {
            _resolved = false;
            if (!HookNeeded()) return; // registrado depois, se a lista ou CountSpawns mudar
            Track("EffectManager.SpawnEffect", () => On.RoR2.EffectManager.SpawnEffect_EffectIndex_EffectData_bool += OnSpawnEffect);
            _hooked = true;
        }

        protected override void OnDisable()
        {
            On.RoR2.EffectManager.SpawnEffect_EffectIndex_EffectData_bool -= OnSpawnEffect;
            _hooked = false;
        }

        private bool HookNeeded() => _countOn || (_list.Value ?? "").Trim(' ', ',', '\t').Length > 0;

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
                if (_hasBlocked && !transmit && _blocked != null && (uint)i < (uint)_blocked.Length && _blocked[i])
                    return; // visual local: sem rede, sem dano
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
            _blocked = new bool[n];
            _hasBlocked = false;
            foreach (var raw in (_list.Value ?? "").Split(','))
            {
                var name = raw.Trim();
                if (name.Length == 0) continue;
                var idx = EffectCatalog.FindEffectIndex(name);
                if (idx == EffectIndex.Invalid) { Log.LogWarning($"[Particles] efeito '{name}' não encontrado; ignorado."); continue; }
                _blocked[(int)idx] = true;
                _hasBlocked = true;
            }
            Log.LogInfo($"[Particles] lista resolvida: {(_hasBlocked ? "cortando efeitos configurados" : "vazia (jogo base)")}");
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
