using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using RoR2;
using RoR2.UI;
using Ror2Meeeee.Core.Stats;
using UnityEngine;

namespace Ror2Meeeee
{
    // Tela de status (Stats.MenuKey, padrão F8), em ciclo: 1º toque mostra os seus atributos numa barra no próprio
    // HUD, em cima das habilidades e inclinada como elas (StatsBar); 2º toque troca pela lista de todos os jogadores
    // no centro da tela (retrato, nome e atributos; a Tormenta no rodapé, só no host); 3º toque fecha. Os atributos são
    // do CharacterBody, que o jogo recalcula em todo cliente: nada de rede.
    // Só local; o componente fica desligado com a tela fechada (a tecla é lida no RoR2Application.onUpdate) e os textos
    // são refeitos 4x por segundo, não por frame.
    internal sealed class StatsPanel : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f;
        private const int Closed = 0, Mine = 1, All = 2;
        private const float CardH = 132f, Portrait = 104f;

        // Um jogador: retrato e os 7 valores já formatados.
        private struct Entry
        {
            public string Name, Health, Speed, Damage, Regen, Armor, Level, AttackSpeed;
            public Texture Portrait;
            public bool Me;
        }

        private static ConfigEntry<string> _key;
        private static StatsPanel _instance;
        private static Texture _health, _speed, _damage, _regen, _armor, _level, _attackSpeed, _players;

        private KeyCode _keyCode = KeyCode.F8;
        private int _mode;
        private float _refreshAt;
        private string _tempest = "", _hint = "";
        private Entry _mine;
        private readonly List<Entry> _all = new List<Entry>();
        private GUIStyle _value, _label, _name, _head, _small, _panel, _card, _cardMe, _slot;
        private readonly StringBuilder _sb = new StringBuilder(256);
        private readonly GUIContent _measure = new GUIContent();
        private StatsBar _bar; // 1º toque: barra no HUD (StatsBar)
        private static Sprite _panelSprite; // moldura em 9 partes (borda 30)

        public static void Install(GameObject host, ConfigEntry<string> key)
        {
            if (_instance) return;
            _key = key;
            _instance = host.AddComponent<StatsPanel>();
            _instance.enabled = false;
            _instance.ParseKey();
            key.SettingChanged += (_, __) => _instance.ParseKey();
            RoR2Application.onUpdate += PollKey;
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
                var next = Run.instance ? (_instance._mode + 1) % 3 : Closed;
                if (next == All && DropMenu.IsOpen) return; // a lista não abre por cima da tela do F7; a barra pode
                _instance.SetMode(next);
            }
            catch (Exception e)
            {
                Diag.Log?.LogWarning($"[Stats] falha na tecla: {e.Message}");
            }
        }

        // Só a lista conta como "tela aberta" para o F7; a barra pequena fica junto do HUD.
        public static bool IsOpen => _instance && _instance._mode == All;

        private void SetMode(int mode)
        {
            _mode = mode;
            enabled = mode != Closed;
            _refreshAt = 0f;
            if (mode != Mine) _bar?.SetActive(false);
            if (mode != Closed && !_health)
            {
                _health = Icon("health"); _speed = Icon("speed"); _damage = Icon("damage"); _regen = Icon("regen");
                _armor = Icon("armor"); _level = Icon("level"); _attackSpeed = Icon("attackspeed");
            }
        }

        private static Texture Icon(string name) => TitheArtifact.LoadIcon($"stat_{name}.png", new Color32(200, 200, 200, 255)).texture;

        private void Update()
        {
            if (_mode == Closed) return;
            if (!Run.instance) { SetMode(Closed); return; } // fecha pela tecla (ou ao sair da run)
            if (Time.unscaledTime < _refreshAt) return;
            _refreshAt = Time.unscaledTime + RefreshSeconds;
            try { Refresh(); }
            catch (Exception e)
            {
                SetMode(Closed);
                Diag.Log?.LogWarning($"[Stats] falha: {e.Message}");
            }
        }

        private static string N(double v) => ShortNumber.Format(v, !Loc.IsPt);

        private static Entry Read(CharacterMaster m, bool me)
        {
            var b = m.GetBody();
            var e = new Entry { Name = Util.GetBestMasterName(m), Me = me };
            var prefab = m.bodyPrefab ? m.bodyPrefab.GetComponent<CharacterBody>() : null;
            e.Portrait = b ? b.portraitIcon : prefab ? prefab.portraitIcon : null;
            if (!b)
            {
                e.Health = Loc.T("dead", "morto");
                e.Speed = e.Damage = e.Regen = e.Armor = e.Level = e.AttackSpeed = "—";
                return e;
            }
            var hc = b.healthComponent;
            e.Health = N(hc ? hc.health : 0f) + "/" + N(b.maxHealth);
            e.Speed = b.moveSpeed.ToString("0.#", Loc.IsPt ? Loc.PtCulture : Loc.EnCulture);
            e.Damage = N(b.damage);
            e.Regen = N(b.regen) + "/s";
            e.Armor = b.armor.ToString("0");
            e.Level = b.level.ToString("0");
            e.AttackSpeed = b.attackSpeed.ToString("0.##", Loc.IsPt ? Loc.PtCulture : Loc.EnCulture) + "x";
            return e;
        }

        private void Refresh()
        {
            var user = LocalUserManager.GetFirstLocalUser();
            var me = user != null ? user.cachedMaster : null;
            if (_mode == Mine)
            {
                _mine = me ? Read(me, true) : default;
                UpdateBar(user);
                return;
            }

            _all.Clear();
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                var m = pcmc ? pcmc.master : null;
                if (m) _all.Add(Read(m, m == me));
            }

            // Tormenta (só no host): estado do diretor e multiplicadores atuais.
            _sb.Clear();
            _tempest = TempestFeature.AppendSummary(_sb, Loc.IsPt) ? _sb.ToString() : "";
            _hint = Loc.T($"{_key.Value} closes", $"{_key.Value} fecha"); // fora do OnGUI: sem alocar por evento
        }

        // Barra uGUI no HUD deste jogador; refeita se o HUD mudar (novo estágio, novo câmera rig).
        private void UpdateBar(LocalUser user)
        {
            HUD hud = null;
            foreach (var h in HUD.readOnlyInstanceList)
                if (h && h.localUserViewer == user && h.skillIcons != null && h.skillIcons.Length > 0 && h.skillIcons[0]) { hud = h; break; }
            if (!hud || _mine.Name == null) { _bar?.SetActive(false); return; }
            if (_bar == null || !_bar.IsFor(hud))
            {
                _bar?.Destroy();
                if (!_panelSprite)
                {
                    var tex = (Texture2D)Icon("panel");
                    _panelSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(30, 30, 30, 30));
                }
                _bar = new StatsBar(hud, _panelSprite, new[] { _health, _speed, _damage, _regen, _armor, _level, _attackSpeed }, Loc.T("Lvl", "Nvl"));
            }
            _bar.SetActive(true);
            _bar.Set(_mine.Health, _mine.Speed, _mine.Damage, _mine.Regen, _mine.Armor, _mine.Level, _mine.AttackSpeed);
        }

        // Identidade visual das telas do mod (F7 e F8): título azul, texto claro, dica apagada.
        internal static readonly Color HeadColor = new Color(0.45f, 0.78f, 0.95f), TextColor = new Color(0.85f, 0.88f, 0.92f), HintColor = new Color(0.55f, 0.6f, 0.68f);

        // Moldura em 9 partes (stat_<nome>.png): os cantos (arredondados, cantoneiras) não esticam.
        internal static GUIStyle Frame(string name, int border) => new GUIStyle
        {
            normal = { background = (Texture2D)Icon(name) },
            border = new RectOffset(border, border, border, border),
        };

        private void Stat(float x, float y, float width, Texture icon, string text, string prefix = null)
        {
            var end = x + width - 12f; // o valor vai até o fim da coluna, não invade a vizinha
            if (icon) GUI.DrawTexture(new Rect(x, y + 4, 26, 26), icon, ScaleMode.ScaleToFit);
            x += 36f;
            if (prefix != null)
            {
                _measure.text = prefix;
                var pw = _label.CalcSize(_measure).x + 6f;
                GUI.Label(new Rect(x, y, pw, 34), prefix, _label);
                x += pw;
            }
            GUI.Label(new Rect(x, y, Mathf.Max(40f, end - x), 34), text, _value);
        }

        // Os 7 valores em 4 colunas de 2 linhas, como no desenho: vida/movimento, dano/regeneração, armadura/nível,
        // velocidade de ataque, cada um com o nome do atributo. Colunas com larguras diferentes: a vida
        // ("999,9 mil/999,9 mil") e "Regeneração:"/"Vel. de ataque:" precisam de mais espaço que a armadura.
        private static readonly float[] ColShare = { 0.29f, 0.25f, 0.2f, 0.26f };

        private void Stats(Rect r, in Entry e)
        {
            float c0 = r.width * ColShare[0], c1 = r.width * ColShare[1], c2 = r.width * ColShare[2], c3 = r.width * ColShare[3];
            float x1 = r.x + c0, x2 = x1 + c1, x3 = x2 + c2;
            var y1 = r.y + r.height / 2f - 36f;
            var y2 = r.y + r.height / 2f + 2f;
            Stat(r.x, y1, c0, _health, e.Health, Loc.T("Health:", "Vida:"));
            Stat(r.x, y2, c0, _speed, e.Speed, Loc.T("Speed:", "Velocidade:"));
            Stat(x1, y1, c1, _damage, e.Damage, Loc.T("Damage:", "Dano:"));
            Stat(x1, y2, c1, _regen, e.Regen, Loc.T("Regen:", "Regeneração:"));
            Stat(x2, y1, c2, _armor, e.Armor, Loc.T("Armor:", "Armadura:"));
            Stat(x2, y2, c2, _level, e.Level, Loc.T("Level:", "Nível:"));
            Stat(x3, y1, c3, _attackSpeed, e.AttackSpeed, Loc.T("Attack speed:", "Vel. de ataque:"));
        }

        private void OnGUI()
        {
            if (_mode != All) return; // a barra (Mine) é uGUI, no HUD
            if (_value == null)
            {
                _value = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = false };
                _value.normal.textColor = Color.white;
                _label = new GUIStyle(_value) { fontStyle = FontStyle.Normal };
                _label.normal.textColor = TextColor;
                _name = new GUIStyle(_value) { fontSize = 21 };
                _head = new GUIStyle(_value) { fontSize = 26 };
                _head.normal.textColor = HeadColor;
                _small = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
                _small.normal.textColor = HintColor;
                _panel = Frame("panel", 30);
                _card = Frame("card", 16);
                _cardMe = Frame("card_me", 16);
                _slot = Frame("slot", 12);
                _players = Icon("players");
            }
            // Coordenadas virtuais ~1080p, escaladas pela resolução real (como a tela do F7).
            var scale = Mathf.Max(0.8f, Screen.height / 1080f);
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var W = Screen.width / scale;
            var H = Screen.height / scale;

            var w = 1240f; // largura para o nome de cada atributo e a vida em milhares
            var extra = _tempest.Length > 0 ? 74f : 0f;
            var h = 76f + _all.Count * (CardH + 10f) + extra + 40f;
            var p = new Rect((W - w) / 2f, Mathf.Max(20f, (H - h) / 2f), w, h);
            GUI.Box(p, GUIContent.none, _panel);
            if (_players) GUI.DrawTexture(new Rect(p.x + 22, p.y + 18, 40, 40), _players, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(p.x + 74, p.y + 20, w - 100, 36), Loc.T("PLAYERS IN THE RUN", "JOGADORES NA RUN"), _head);
            var y = p.y + 72f;
            for (var i = 0; i < _all.Count; i++)
            {
                var e = _all[i];
                // Cartão: retrato do personagem (1) à esquerda; nome do jogador (2) em cima dos valores.
                var c = new Rect(p.x + 14, y, w - 28, CardH);
                GUI.Box(c, GUIContent.none, e.Me ? _cardMe : _card);
                var pr = new Rect(c.x + 18, c.y + (CardH - Portrait) / 2f, Portrait, Portrait);
                GUI.Box(pr, GUIContent.none, _slot);
                if (e.Portrait) GUI.DrawTexture(new Rect(pr.x + 4, pr.y + 4, pr.width - 8, pr.height - 8), e.Portrait, ScaleMode.ScaleToFit);
                var sx = pr.xMax + 34f;
                GUI.Label(new Rect(sx, c.y + 8, c.xMax - sx - 14, 30), e.Name, _name);
                Stats(new Rect(sx, c.y + 36, c.xMax - sx - 14, CardH - 40), e);
                y += CardH + 10f;
            }
            if (extra > 0f)
            {
                GUI.Label(new Rect(p.x + 24, y, w - 48, 26), Loc.T("TEMPEST", "TORMENTA"), _name);
                GUI.Label(new Rect(p.x + 24, y + 26, w - 48, extra - 26), _tempest, _small);
            }
            GUI.Label(new Rect(p.x + 24, p.yMax - 36, w - 48, 28), _hint, _small);
            GUI.matrix = old;
        }
    }
}
