using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Gold;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Com o Artefato do Dízimo ligado, todo ouro ganho por jogadores também vira crédito de spawn
    // (CombatDirector.monsterCredit) dos diretores de monstros que acumulam ondas de crédito. Só no host.
    // O diretor agradece no chat: o crédito é acumulado e falado no máximo 1 vez por Gold.ThankIntervalSeconds,
    // checado no próprio evento de ouro (sem Update/timer); o resto sai no fim do estágio.
    public sealed class GoldFeature : Feature
    {
        private readonly ConfigEntry<float> _ratio;
        private readonly ConfigEntry<float> _thankInterval;
        private readonly ConfigEntry<float> _maxCredit;
        private readonly ConfigEntry<int> _maxMonsters;
        private long _logged;
        private float _ratioValue;
        private float _thankIntervalValue;
        private float _maxCreditValue;
        private int _maxMonstersValue;
        private float _winStart, _winCredit; // janela de 1 s do teto de crédito
        private readonly Diag.RateWatch _flood = new Diag.RateWatch("eventos de ouro", 40);
        private double _pendingCredit;  // crédito ainda não agradecido
        private ulong _pendingGold;
        private float _lastThank;       // Time.time do último agradecimento
        private int _thankIndex;        // rodízio das frases
        // Elegibilidade por diretor, resolvida uma vez (reflexão só na primeira vez); limpo por run.
        private readonly Dictionary<int, bool> _eligible = new Dictionary<int, bool>();

        public GoldFeature(Cfg cfg, ManualLogSource log) : base("Gold", cfg, log,
            "Artefato do Dízimo: com o artefato ativado no lobby, parte de todo ouro que os jogadores ganham vira crédito para o diretor gerar mais inimigos, e ele agradece no chat. Sem o artefato, nada muda.",
            enabledByDefault: true)
        {
            _ratio = cfg.Bind("Gold", "Ratio", 0.1f,
                "Quanto do ouro ganho vira crédito do diretor (0.1 = 10%). 0 desliga.");
            _ratioValue = _ratio.Value;
            _ratio.SettingChanged += (_, __) => _ratioValue = _ratio.Value;
            _thankInterval = cfg.Bind("Gold", "ThankIntervalSeconds", 60f,
                "O diretor agradece no chat o ouro recebido do Dízimo, juntando tudo e falando no máximo 1 vez a cada X segundos (e no fim do estágio). 0 = sem mensagens.");
            _thankIntervalValue = _thankInterval.Value;
            _thankInterval.SettingChanged += (_, __) => _thankIntervalValue = _thankInterval.Value;
            _maxCredit = cfg.Bind("Gold", "MaxCreditPerSecond", 150f,
                "Teto de crédito que o Dízimo pode dar por segundo. Evita que uma enxurrada de ouro (centenas de eventos/s) encha o diretor e trave o jogo. 0 = sem teto.");
            _maxCreditValue = _maxCredit.Value;
            _maxCredit.SettingChanged += (_, __) => _maxCreditValue = _maxCredit.Value;
            _maxMonsters = cfg.Bind("Gold", "MaxMonsters", 80,
                "Com tantos monstros vivos (ou mais), o Dízimo para de dar crédito até o número cair. Evita a bola de neve ouro -> crédito -> mais inimigos -> lag. 0 = sem trava.");
            _maxMonstersValue = _maxMonsters.Value;
            _maxMonsters.SettingChanged += (_, __) => _maxMonstersValue = _maxMonsters.Value;
        }

        protected override void OnEnable()
        {
            Track("CharacterMaster.GiveMoney", () => MoneyHub.SetAfter(OnGiveMoney));
            Track("Stage.onServerStageComplete", () => Stage.onServerStageComplete += OnStageComplete);
        }

        protected override void OnDisable()
        {
            MoneyHub.SetAfter(null);
            Stage.onServerStageComplete -= OnStageComplete;
            _eligible.Clear();
        }

        private void OnGiveMoney(CharacterMaster self, uint amount)
        {
            try
            {
                if (!NetworkServer.active || !self || !self.playerCharacterMasterController || !TitheArtifact.IsEnabled()) return;
                var credit = GoldCredit.Compute(amount, 1, _ratioValue);
                if (credit <= 0f) return;

                Diag.GoldEvents++;
                _flood.Hit($"+{amount} de {self.name}");
                if (_maxMonstersValue > 0 && TeamComponent.GetTeamMembers(TeamIndex.Monster).Count >= _maxMonstersValue)
                {
                    Diag.CreditDropped++;
                    return;
                }

                // Janela de 1 s: teto de crédito e diagnóstico de enxurrada (o ouro vem do jogo/outro item, não do mod).
                var t = UnityEngine.Time.time;
                if (t - _winStart >= 1f) { _winStart = t; _winCredit = 0f; }
                if (_maxCreditValue > 0f)
                {
                    var room = _maxCreditValue - _winCredit;
                    if (room <= 0f) { Diag.CreditDropped++; return; }
                    if (credit > room) { credit = room; Diag.CreditDropped++; }
                    _winCredit += credit;
                }

                var n = 0;
                var list = CombatDirector.instancesList;
                for (var i = 0; i < list.Count; i++) // for, não foreach: sem enumerador
                {
                    var d = list[i];
                    if (!d || !d.enabled || !IsMainMonsterDirector(d)) continue;
                    d.monsterCredit += credit;
                    n++;
                }
                if (n == 0) return;
                // Ouro cai o tempo todo: loga só as 3 primeiras ocorrências e depois 1 a cada 200.
                if (++_logged <= 3 || _logged % 200 == 0)
                    Log.LogInfo($"[Gold] +{amount} ouro de {self.name} => +{credit:F1} crédito em {n} diretor(es) (#{_logged})");

                // Só soma: a mensagem (alocação, rede) sai no máximo 1 vez por intervalo.
                _pendingCredit += (double)credit * n;
                Diag.CreditGiven += (double)credit * n;
                _pendingGold += amount;
                var now = UnityEngine.Time.time;
                if (TitheThanks.Due(now, _lastThank, _thankIntervalValue, _pendingCredit)) Thank(now);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        // Fim do estágio: agradece o que sobrou, sem esperar o intervalo.
        private void OnStageComplete(Stage stage)
        {
            try
            {
                if (NetworkServer.active && _thankIntervalValue > 0f && _pendingCredit >= 1) Thank(UnityEngine.Time.time);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void Thank(float now)
        {
            var text = TitheThanks.Format(_thankIndex++, _pendingCredit, _pendingGold);
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = text });
            Log.LogInfo($"[Gold] agradecimento no chat: {_pendingGold} de ouro => {_pendingCredit:F0} de crédito"); // 1 linha por intervalo
            _pendingCredit = 0;
            _pendingGold = 0;
            _lastThank = now;
        }

        // Só o diretor principal soma ondas de crédito; os de boss/evento não devem receber ouro extra.
        private bool IsMainMonsterDirector(CombatDirector d)
        {
            var id = d.GetInstanceID();
            if (_eligible.TryGetValue(id, out var ok)) return ok;
            ok = Directors.IsMainMonster(d);
            _eligible[id] = ok;
            return ok;
        }

        public override void ResetRunState()
        {
            _logged = 0;
            _winCredit = 0f;
            _eligible.Clear();
            _pendingCredit = 0;
            _pendingGold = 0;
            _lastThank = UnityEngine.Time.time; // 1º agradecimento só 1 intervalo depois do início da run
            _thankIndex = 0;
        }
    }
}
