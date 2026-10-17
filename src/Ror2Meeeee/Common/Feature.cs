using System;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace Ror2Meeeee
{
    // Uma mecânica do mod, com kill-switch e falha isolada.
    // Fail(Exception) é o único ponto de contagem e desligamento.
    public abstract class Feature
    {
        private readonly ConfigEntry<bool> _enabledEntry;
        private int _failureCount;
        private bool _shutdownByFailures;

        private readonly System.Collections.Generic.List<string> _hookResults = new System.Collections.Generic.List<string>();

        // `summary`: o que a feature faz, para a descrição do <Nome>.Enabled na tela de mods.
        protected Feature(string name, Cfg cfg, ManualLogSource log, string summary, bool enabledByDefault = false)
            : this(name, cfg, log, cfg.BindEnabled(name, summary, enabledByDefault)) { }

        // Para features cujo kill-switch vive em outra chave de config (ex.: General.ProbeEnabled).
        // null = sem opção de config: a feature fica sempre ligada (ex.: Sharing, controlada só pelo artefato).
        protected Feature(string name, Cfg cfg, ManualLogSource log, ConfigEntry<bool> enabledEntry)
        {
            Name = name;
            Cfg = cfg;
            Log = log;
            _enabledEntry = enabledEntry;
        }

        public string Name { get; }
        public bool Enabled { get; private set; }
        public bool ConfigEnabled => _enabledEntry == null || _enabledEntry.Value;
        internal ConfigEntry<bool> EnabledEntry => _enabledEntry; // pode ser null (ver construtor)
        public int FailureCount => _failureCount;
        public bool ShutdownByFailures => _shutdownByFailures;
        // "nome: ok" ou "nome: FALHA ..." por hook registrado via Track (para ror2meeeee_status).
        public System.Collections.Generic.IReadOnlyList<string> HookResults => _hookResults;

        protected Cfg Cfg { get; }
        protected ManualLogSource Log { get; }

        public void Enable()
        {
            if (Enabled || _shutdownByFailures) return;
            _hookResults.Clear();
            try
            {
                OnEnable();
                Enabled = true;
                Log.LogInfo($"{Name}: hooks registrados");
            }
            catch (Exception e)
            {
                Log.LogError($"{Name}: falha ao registrar hooks, feature desligada: {e}");
                RemoveHooksQuietly(); // pode haver hooks registrados antes da exceção
            }
        }

        public void Disable()
        {
            if (!Enabled) return;
            Enabled = false;
            RemoveHooksQuietly();
        }

        // Chamado em um único ponto (Plugin) no início e no fim da run.
        public virtual void ResetRunState() { }

        internal void ResetRunStateSafe()
        {
            try { ResetRunState(); }
            catch (Exception e)
            {
                // Feature desligada não acumula falhas: só registra.
                if (Enabled) Fail(e);
                else Log.LogError($"{Name}: falha em ResetRunState (feature desligada, não contada): {e}");
            }
        }

        protected void Fail(Exception e)
        {
            if (_shutdownByFailures) return; // já desligada: sem recontar nem relogar
            _failureCount++;
            Diag.Exceptions++;
            Log.LogError($"{Name}: falha {_failureCount}/{Cfg.MaxFailures}: {e}");

            if (_failureCount >= Cfg.MaxFailures && !_shutdownByFailures)
            {
                _shutdownByFailures = true;
                Disable();
                Log.LogWarning($"{Name}: desligada após {_failureCount} falhas.");
            }
        }

        // Registra um hook e anota o resultado; a exceção segue para Enable() (feature fica desligada).
        protected void Track(string hookName, Action register)
        {
            try
            {
                register();
                _hookResults.Add($"{hookName}: ok");
            }
            catch (Exception e)
            {
                _hookResults.Add($"{hookName}: FALHA ({e.GetType().Name}: {e.Message})");
                throw;
            }
        }

        protected abstract void OnEnable();

        // Remove TODOS os hooks registrados por OnEnable; precisa ser idempotente.
        protected abstract void OnDisable();

        private void RemoveHooksQuietly()
        {
            try { OnDisable(); }
            catch (Exception e) { Log.LogError($"{Name}: falha ao remover hooks: {e}"); }
        }
    }
}
