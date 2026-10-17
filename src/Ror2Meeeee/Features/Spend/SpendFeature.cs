using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Spend;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Quanto cada jogador gasta em compras (baús, cápsulas, impressoras...) na run, só no host.
    // Ponto: PurchaseInteraction.onPurchaseGlobalServer. Acumula na principal (barato); no fim da run
    // o CSV é gravado em ror2meeeee-spend.csv por um worker, com uma cópia dos dados.
    public sealed class SpendFeature : Feature
    {
        private static readonly object CsvLock = new object();

        private readonly SpendLedger _ledger = new SpendLedger();
        private readonly ConfigEntry<bool> _logEach;
        private bool _logEachValue;

        public SpendFeature(Cfg cfg, ManualLogSource log) : base("Spend", cfg, log,
            "Gasto por jogador: conta compras e ouro gasto de cada jogador na run (comando ror2meeeee_spend) e grava BepInEx/ror2meeeee-spend.csv no fim. Não muda o jogo.",
            enabledByDefault: true)
        {
            _logEach = cfg.Bind("Spend", "LogEachPurchase", false, "Escreve no log uma linha por compra (jogador, objeto, custo). Gera muito texto; deixe desligado.");
            _logEach.SettingChanged += (_, __) => _logEachValue = _logEach.Value;
            _logEachValue = _logEach.Value;
        }

        public string Report() => SpendLedger.Format(_ledger.Snapshot());

        // Início e fim da run: grava o que houver (worker) e zera.
        public override void ResetRunState()
        {
            if (_ledger.Players > 0)
            {
                var rows = _ledger.Snapshot();
                var utc = DateTime.UtcNow;
                var path = System.IO.Path.Combine(Paths.BepInExRootPath, "ror2meeeee-spend.csv");
                var log = Log;
                Plugin.RunInBackground(() =>
                {
                    try
                    {
                        var sb = new System.Text.StringBuilder();
                        var inv = System.Globalization.CultureInfo.InvariantCulture;
                        lock (CsvLock)
                        {
                            if (!File.Exists(path)) sb.Append("utc,player,purchases,gold_spent\n");
                            foreach (var r in rows)
                                sb.Append(utc.ToString("o", inv)).Append(',').Append(r.Name.Replace(',', ';')).Append(',')
                                  .Append(r.Purchases.ToString(inv)).Append(',').Append(r.Gold.ToString(inv)).Append('\n');
                            File.AppendAllText(path, sb.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        log.LogError($"Spend: falha ao gravar o CSV: {e.Message}");
                    }
                });
            }
            _ledger.Clear();
        }

        protected override void OnEnable()
        {
            Track("PurchaseInteraction.onPurchaseGlobalServer", () => PurchaseHub.SetSpend(OnPurchase)); // passo 1 (antes do Taxade)
        }

        protected override void OnDisable()
        {
            PurchaseHub.SetSpend(null);
        }

        private static Func<string> NameOf(CharacterMaster master) => () => Util.GetBestMasterName(master);

        // O contexto vem de um pool do jogo: nada é guardado depois do callback.
        private void OnPurchase(CostTypeDef.PayCostContext ctx, CostTypeDef.PayCostResults results)
        {
            try
            {
                if (!NetworkServer.active) return;
                var master = ctx.activatorMaster;
                if (!master || !master.playerCharacterMasterController) return;

                var pi = ctx.purchaseInteraction;
                var gold = pi && pi.costType == CostTypeIndex.Money ? Math.Max(0, ctx.cost) : 0;
                var id = master.GetInstanceID();
                _ledger.Add(id, _ledger.Knows(id) ? null : NameOf(master), gold); // closure só na 1ª compra do jogador

                if (_logEachValue)
                    Log.LogInfo($"[Spend] {Util.GetBestMasterName(master)}: {(ctx.purchasedObject ? ctx.purchasedObject.name : "?")} por {gold} de ouro");
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }
}
