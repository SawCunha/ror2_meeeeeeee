using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ror2Meeeee
{
    // Barra dos seus atributos (1º toque do F8), como em docs/status_jogador.png. É uGUI filha do mesmo container dos
    // ícones de habilidade do HUD, não IMGUI: assim herda a inclinação 3D, a escala e a visibilidade do HUD. Fica logo
    // acima das habilidades e do equipamento, com a largura deles. As medidas são em pixels de 1080p, convertidas pela
    // altura do ícone de habilidade (IconDesign).
    internal sealed class StatsBar
    {
        private const float H = 104f, MinW = 560f, IconDesign = 58f, Gap = 34f, ValueX = 36f;
        // Fração da largura de cada coluna: a vida ("999,9 mil/999,9 mil") precisa de mais espaço que as outras.
        private static readonly float[] ColShare = { 0.34f, 0.22f, 0.22f, 0.22f };
        private readonly float[] _valueX = new float[7];
        private static readonly Color Divider = new Color(1f, 1f, 1f, 0.12f);

        private readonly GameObject _root;
        private readonly RectTransform _rt;
        private readonly RectTransform[] _cols = new RectTransform[4], _dividers = new RectTransform[3];
        private readonly TextMeshProUGUI[] _values = new TextMeshProUGUI[7];
        private readonly HUD _hud;
        private readonly Vector3[] _corners = new Vector3[4];
        private float _w = -1f;

        public bool IsFor(HUD hud) => _root && _hud == hud;

        public StatsBar(HUD hud, Sprite panel, Texture[] icons, string levelLabel)
        {
            _hud = hud;
            _root = new GameObject("Ror2MeeeeeStatsBar", typeof(RectTransform));
            _rt = (RectTransform)_root.transform;
            _rt.SetParent(hud.skillIcons[0].transform.parent, false);
            _root.layer = _rt.parent.gameObject.layer;
            _root.AddComponent<LayoutElement>().ignoreLayout = true; // o container pode ter layout group
            _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0f);
            var bg = _root.AddComponent<Image>();
            bg.sprite = panel;
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            for (var i = 0; i < 4; i++) _cols[i] = Rect(_rt, 0f, 0f, 0f, H);
            for (var i = 0; i < 3; i++)
            {
                _dividers[i] = Rect(_rt, 0f, 10f, 1f, H - 20f);
                var d = _dividers[i].gameObject.AddComponent<RawImage>();
                d.color = Divider;
                d.raycastTarget = false;
            }
            // Colunas: vida/movimento, dano/regeneração, armadura/nível, velocidade de ataque.
            for (var i = 0; i < 7; i++)
            {
                var y = i % 2 == 0 ? H / 2f - 36f : H / 2f + 2f;
                var col = _cols[i / 2];
                var icon = Rect(col, 0f, y + 4f, 26f, 26f).gameObject.AddComponent<RawImage>();
                icon.texture = icons[i];
                icon.raycastTarget = false;
                var x = ValueX;
                if (i == 5)
                {
                    Text(col, x, y, 40f, levelLabel, false);
                    x += 40f;
                }
                _valueX[i] = x;
                _values[i] = Text(col, x, y, 150f, "", true);
                // Se o valor não couber na coluna, a fonte encolhe (até 14) em vez de invadir a vizinha. Atualiza a 4 Hz.
                _values[i].enableAutoSizing = true;
                _values[i].fontSizeMin = 14f;
                _values[i].fontSizeMax = 20f;
            }
        }

        private static RectTransform Rect(RectTransform parent, float x, float y, float w, float h)
        {
            var rt = (RectTransform)new GameObject("e", typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.gameObject.layer = parent.gameObject.layer;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        private static TextMeshProUGUI Text(RectTransform parent, float x, float y, float w, string text, bool bold)
        {
            var t = Rect(parent, x, y, w, 34f).gameObject.AddComponent<HGTextMeshProUGUI>(); // fonte do jogo
            t.fontSize = 20f;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.color = bold ? Color.white : new Color(0.85f, 0.88f, 0.92f);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        public void SetActive(bool on)
        {
            if (_root && _root.activeSelf != on) _root.SetActive(on);
        }

        public void Destroy()
        {
            if (_root) Object.Destroy(_root);
        }

        // Valores na ordem das colunas; e reposiciona sobre os ícones (o equipamento pode aparecer ou sumir).
        public void Set(string health, string speed, string damage, string regen, string armor, string level, string attackSpeed)
        {
            Place();
            _values[0].text = health; _values[1].text = speed; _values[2].text = damage; _values[3].text = regen;
            _values[4].text = armor; _values[5].text = level; _values[6].text = attackSpeed;
        }

        private void Place()
        {
            var parent = (RectTransform)_rt.parent;
            float xMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue, iconH = 0f, equipH = 0f;
            foreach (var icon in _hud.skillIcons) Grow(parent, icon, true, ref xMin, ref xMax, ref yMax, ref iconH);
            // Equipamento (Q à direita): o quadro aparece mesmo vazio, com o componente desligado; é maior, então só
            // entra na largura, não na escala. A barra termina na borda direita do Q.
            if (_hud.equipmentIcons != null)
                foreach (var icon in _hud.equipmentIcons) Grow(parent, icon, false, ref xMin, ref xMax, ref yMax, ref equipH);
            if (iconH <= 0f) return;
            var k = iconH / IconDesign; // unidades locais por pixel de desenho
            var w = Mathf.Max(MinW, (xMax - xMin) / k);
            _rt.localScale = new Vector3(k, k, 1f);
            _rt.localPosition = new Vector3(xMax - w * k / 2f, yMax + Gap * k, 0f); // alinhada à direita da barra
            if (Mathf.Approximately(w, _w)) return;
            _w = w;
            _rt.sizeDelta = new Vector2(w, H);
            var x = 30f;
            for (var i = 0; i < 4; i++)
            {
                var col = (w - 40f) * ColShare[i];
                _cols[i].anchoredPosition = new Vector2(x, 0f);
                x += col;
                if (i < 3) _dividers[i].anchoredPosition = new Vector2(x - 14f, -10f);
            }
            // Cada valor termina antes do divisor da sua coluna, para o auto-ajuste da fonte saber o limite.
            for (var i = 0; i < 7; i++)
                _values[i].rectTransform.sizeDelta = new Vector2((w - 40f) * ColShare[i / 2] - _valueX[i] - 22f, 34f);
        }

        // Extremos do ícone no espaço local do container (o mesmo plano inclinado).
        private void Grow(RectTransform parent, Component icon, bool activeOnly, ref float xMin, ref float xMax, ref float yMax, ref float h)
        {
            if (!icon || (activeOnly && !icon.gameObject.activeInHierarchy)) return;
            ((RectTransform)icon.transform).GetWorldCorners(_corners);
            float lo = float.MaxValue, hi = float.MinValue;
            for (var i = 0; i < 4; i++)
            {
                var p = parent.InverseTransformPoint(_corners[i]);
                xMin = Mathf.Min(xMin, p.x); xMax = Mathf.Max(xMax, p.x);
                lo = Mathf.Min(lo, p.y); hi = Mathf.Max(hi, p.y);
            }
            yMax = Mathf.Max(yMax, hi);
            h = Mathf.Max(h, hi - lo);
        }
    }
}
