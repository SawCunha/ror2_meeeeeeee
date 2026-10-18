using System;
using System.Text;
using BepInEx.Configuration;
using RoR2;
using RoR2.Stats;
using Ror2Meeeee.Core.Stats;
using UnityEngine;
using UnityEngine.Networking;

namespace Ror2Meeeee
{
    // Tela de status (Stats.MenuKey, padrão F8): seus atributos atuais e, para todos os jogadores, vida, dano e os totais da
    // run (dano causado, cura, dano recebido...). Totais: StatSheet do próprio jogo (o da tela de fim de run); o jogo só manda
    // ao cliente os dele, então os dos outros chegam do host por StatsSyncMessage. Só local; o componente fica desligado com a tela fechada
    // (a tecla é lida no RoR2Application.onUpdate) e o texto é refeito 4x por segundo, não por frame.
    internal sealed class StatsPanel : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f;

        private static ConfigEntry<string> _key;
        private static StatsPanel _instance;

        private KeyCode _keyCode = KeyCode.F8;
        private bool _open;
        private float _refreshAt;
        private string _now = "", _run = "", _title = "";
        private GUIStyle _label, _big, _head;
        private readonly StringBuilder _sb = new StringBuilder(512);

        public static void Install(GameObject host, ConfigEntry<string> key)
        {
            if (_instance) return;
            _key = key;
            _instance = host.AddComponent<StatsPanel>();
            _instance.enabled = false;
            _instance.ParseKey();
            key.SettingChanged += (_, __) => _instance.ParseKey();
            RoR2Application.onUpdate += PollKey;
            StatsSyncMessage.Install();
        }

        private void ParseKey()
        {
            if (!Enum.TryParse(_key.Value, true, out _keyCode)) _keyCode = KeyCode.F8;
        }

        private static void PollKey()
        {
            try
            {
                if (!_instance || !Input.GetKeyDown(_instance._keyCode)) return;
                if (!_instance._open && DropMenu.IsOpen) return; // uma tela por vez: feche a do F7 primeiro
                _instance.SetOpen(!_instance._open && Run.instance);
            }
            catch (Exception e)
            {
                Diag.Log?.LogWarning($"[Stats] falha na tecla: {e.Message}");
            }
        }

        public static bool IsOpen => _instance && _instance._open;

        private void SetOpen(bool open)
        {
            _open = open;
            enabled = open;
            _refreshAt = 0f;
        }

        private void Update()
        {
            if (!_open) return;
            if (!Run.instance) { SetOpen(false); return; } // fecha só pela tecla (ou ao sair da run)
            if (Time.unscaledTime < _refreshAt) return;
            _refreshAt = Time.unscaledTime + RefreshSeconds;
            try { Refresh(); }
            catch (Exception e)
            {
                SetOpen(false);
                Diag.Log?.LogWarning($"[Stats] falha: {e.Message}");
            }
        }

        private static string N(double v) => ShortNumber.Format(v);

        // Totais da run: o próprio jogo no host e para o próprio jogador; para os outros, no cliente, o que o host enviou.
        private static bool TryTotals(CharacterMaster master, bool own, out RunTotals totals)
        {
            totals = default;
            if (!master) return false;
            if (NetworkServer.active || own)
            {
                var sheet = PlayerStatsComponent.FindMasterStatSheet(master);
                if (sheet == null) return false;
                totals = RunTotals.From(sheet);
                return true;
            }
            return StatsSyncMessage.Remote.TryGetValue(master.netId.Value, out totals);
        }

        private void Refresh()
        {
            var user = LocalUserManager.GetFirstLocalUser();
            var me = user != null ? user.cachedMaster : null;
            var body = user != null ? user.cachedBody : null;
            _title = me ? $"STATUS — {Util.GetBestMasterName(me)}" : "STATUS";

            // Seus atributos, detalhados.
            var pt = Loc.IsPt;
            _sb.Clear();
            if (body)
            {
                var hc = body.healthComponent;
                _sb.Append(pt ? "Vida " : "Health ").Append(N(hc ? hc.health : 0f)).Append('/').Append(N(body.maxHealth));
                if (body.maxShield > 0f) _sb.Append(pt ? "   Escudo " : "   Shield ").Append(N(hc ? hc.shield : 0f)).Append('/').Append(N(body.maxShield));
                if (hc && hc.barrier > 0f) _sb.Append(pt ? "   Barreira " : "   Barrier ").Append(N(hc.barrier));
                _sb.Append(pt ? "   Regeneração " : "   Regen ").Append(N(body.regen)).Append("/s\n");
                _sb.Append(pt ? "Dano " : "Damage ").Append(N(body.damage))
                   .Append(pt ? "   Vel. de ataque " : "   Attack speed ").Append(body.attackSpeed.ToString("0.##")).Append('x')
                   .Append(pt ? "   Crítico " : "   Crit ").Append(body.crit.ToString("0.#")).Append('%')
                   .Append(pt ? "   Armadura " : "   Armor ").Append(body.armor.ToString("0"))
                   .Append(pt ? "   Velocidade " : "   Speed ").Append(body.moveSpeed.ToString("0.#"))
                   .Append(pt ? "   Nível " : "   Level ").Append(body.level.ToString("0"));
            }
            else _sb.Append(pt ? "(sem personagem: morto ou espectando)" : "(no character: dead or spectating)");
            _now = _sb.ToString();

            // Todos os jogadores: vida, dano e os totais da run.
            _sb.Clear();
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                var m = pcmc ? pcmc.master : null;
                if (!m) continue;
                var b = m.GetBody();
                var hc = b ? b.healthComponent : null;
                _sb.Append(m == me ? "> " : "  ").Append(Util.GetBestMasterName(m)).Append('\n');
                _sb.Append(pt ? "    vida " : "    health ").Append(b ? N(hc ? hc.health : 0f) + "/" + N(b.maxHealth) : pt ? "morto" : "dead")
                   .Append(pt ? "   dano " : "   damage ").Append(b ? N(b.damage) : "—").Append('\n');
                if (TryTotals(m, m == me, out var t))
                    _sb.Append(pt ? "    causado " : "    dealt ").Append(N(t.Dealt)).Append(pt ? " (ajudantes " : " (minions ").Append(N(t.Minion))
                       .Append(pt ? ")   maior golpe " : ")   biggest hit ").Append(N(t.Highest))
                       .Append(pt ? "\n    cura " : "\n    healed ").Append(N(t.Healed)).Append(pt ? "   recebido " : "   taken ").Append(N(t.Taken))
                       .Append(pt ? "   abates " : "   kills ").Append(N(t.Kills)).Append(pt ? "   mortes " : "   deaths ").Append(N(t.Deaths))
                       .Append(pt ? "   itens " : "   items ").Append(N(t.Items)).Append('\n');
                else _sb.Append(pt ? "    totais da run: aguardando o host\n" : "    run totals: waiting for the host\n");
            }
            _run = _sb.ToString();
        }

        private void OnGUI()
        {
            if (!_open) return;
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true, richText = false };
                _head = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
                _big = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
            }
            // Coordenadas virtuais ~1080p, escaladas pela resolução real (como a tela do F7).
            var scale = Mathf.Max(0.8f, Screen.height / 1080f);
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var w = 900f;
            var players = Mathf.Max(1, PlayerCharacterMasterController.instances.Count);
            var h = 200f + players * 105f;
            var x = Screen.width / scale - w - 30f; var y = 100f;
            var bg = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = bg;
            GUI.Label(new Rect(x + 20, y + 10, w - 40, 36), _title, _big);
            GUI.Label(new Rect(x + 20, y + 50, w - 40, 26), Loc.T("YOU NOW", "VOCÊ AGORA"), _head);
            GUI.Label(new Rect(x + 20, y + 76, w - 40, 56), _now, _label);
            GUI.Label(new Rect(x + 20, y + 136, w - 40, 26), Loc.T("PLAYERS IN THE RUN", "JOGADORES NA RUN"), _head);
            GUI.Label(new Rect(x + 20, y + 162, w - 40, h - 196), _run, _label);
            GUI.Label(new Rect(x + 20, y + h - 30, w - 40, 26), Loc.T($"{_key.Value} closes", $"{_key.Value} fecha"), _label);
            GUI.matrix = old;
        }
    }
}
