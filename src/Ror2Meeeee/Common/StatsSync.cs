using System.Collections.Generic;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Stats;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Totais da run de um jogador (os mesmos números da tela de fim de run).
    internal struct RunTotals
    {
        public double Dealt, Minion, Highest, Healed, Taken, Kills, Deaths, Items;

        public static RunTotals From(StatSheet s) => new RunTotals
        {
            Dealt = s.GetStatValueAsDouble(StatDef.totalDamageDealt),
            Minion = s.GetStatValueAsDouble(StatDef.totalMinionDamageDealt),
            Highest = s.GetStatValueAsDouble(StatDef.highestDamageDealt),
            Healed = s.GetStatValueAsDouble(StatDef.totalHealthHealed),
            Taken = s.GetStatValueAsDouble(StatDef.totalDamageTaken),
            Kills = s.GetStatValueAsDouble(StatDef.totalKills),
            Deaths = s.GetStatValueAsDouble(StatDef.totalDeaths),
            Items = s.GetStatValueAsDouble(StatDef.totalItemsCollected),
        };
    }

    // O jogo só manda a cada cliente as estatísticas DELE; o host tem as de todos. A cada 2 s, se houver cliente conectado
    // e uma run, o host envia os totais de todos os jogadores (R2API Networking). Cada cliente guarda em Remote.
    internal sealed class StatsSyncMessage : INetMessage
    {
        private const float IntervalSeconds = 2f;
        internal static readonly Dictionary<uint, RunTotals> Remote = new Dictionary<uint, RunTotals>(); // netId do master -> totais
        private static float _nextSend;

        private readonly List<KeyValuePair<uint, RunTotals>> _entries = new List<KeyValuePair<uint, RunTotals>>();

        public static void Install()
        {
            NetworkingAPI.RegisterMessageType<StatsSyncMessage>();
            RoR2Application.onUpdate += HostTick;
            Run.onRunStartGlobal += _ => Remote.Clear();
        }

        // Host: barato quando não há cliente (2 checagens por frame); com cliente, 1 mensagem pequena a cada 2 s.
        private static void HostTick()
        {
            if (!NetworkServer.active || !Run.instance || Time.unscaledTime < _nextSend) return;
            _nextSend = Time.unscaledTime + IntervalSeconds;
            if (NetworkServer.connections.Count <= 1) return; // só o host local
            try
            {
                var msg = new StatsSyncMessage();
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    var master = pcmc ? pcmc.master : null;
                    var sheet = master ? PlayerStatsComponent.FindMasterStatSheet(master) : null;
                    if (sheet != null) msg._entries.Add(new KeyValuePair<uint, RunTotals>(master.netId.Value, RunTotals.From(sheet)));
                }
                if (msg._entries.Count > 0) msg.Send(NetworkDestination.Clients);
            }
            catch (System.Exception e)
            {
                Diag.Log?.LogWarning($"[Stats] falha ao enviar os totais: {e.Message}");
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write((byte)_entries.Count);
            foreach (var e in _entries)
            {
                var t = e.Value;
                writer.Write(e.Key);
                writer.Write(t.Dealt); writer.Write(t.Minion); writer.Write(t.Highest); writer.Write(t.Healed);
                writer.Write(t.Taken); writer.Write(t.Kills); writer.Write(t.Deaths); writer.Write(t.Items);
            }
        }

        public void Deserialize(NetworkReader reader)
        {
            _entries.Clear();
            var n = reader.ReadByte();
            for (var i = 0; i < n; i++)
            {
                var id = reader.ReadUInt32();
                var t = new RunTotals
                {
                    Dealt = reader.ReadDouble(), Minion = reader.ReadDouble(), Highest = reader.ReadDouble(), Healed = reader.ReadDouble(),
                    Taken = reader.ReadDouble(), Kills = reader.ReadDouble(), Deaths = reader.ReadDouble(), Items = reader.ReadDouble(),
                };
                _entries.Add(new KeyValuePair<uint, RunTotals>(id, t));
            }
        }

        public void OnReceived()
        {
            if (NetworkServer.active) return; // o host lê direto do jogo
            foreach (var e in _entries) Remote[e.Key] = e.Value;
        }
    }
}
