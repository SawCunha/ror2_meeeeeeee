using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using RoR2;
using Ror2Meeeee.Core.Helluva;
using UnityEngine;

namespace Ror2Meeeee
{
    // Tela para soltar itens (Drop): grade de ícones grandes + painel do item escolhido. Abre com Drop.MenuKey (padrão F7)
    // ou com o comando ror2meeeee_drop_menu [nome] (já com o item selecionado).
    // Mouse: clique no ícone seleciona; o cursor é liberado enquanto a tela está aberta. Quantidade: campo para digitar e
    // botões −1/+1/+5/+10, sempre limitada ao que o jogador tem e a Drop.MaxPerCommand (a mesma regra do servidor).
    // Teclado: setas movem, +/- mudam a quantidade, Enter solta, só a tecla fecha (Esc não; o Esc é do jogo). Não abre com a tela de status (F8) aberta. Solta pelo comando ror2meeeee_drop (host e clientes).
    internal sealed class DropMenu : MonoBehaviour
    {
        // Uma célula da grade: item do inventário local, quantidade e ícone.
        private struct Row
        {
            public string Name, Internal;
            public int Count;
            public Color Color;
            public Texture Tex;
            public Rect Uv;
        }

        private const float Cell = 104f, Gap = 12f;

        private static ConfigEntry<string> _key;
        private static DropMenu _instance;
        private static string _preselect;

        private readonly List<Row> _rows = new List<Row>();
        private bool _open;
        private string _selectedName; // pelo nome interno: a lista se atualiza e os índices mudam
        private int _qty = 1;
        private string _qtyText = "1"; // texto do campo; pode ficar vazio enquanto a pessoa digita
        private float _refreshAt;
        private string _status = "";
        private Vector2 _scroll;
        private KeyCode _keyCode = KeyCode.F7;
        private int _cols = 1;
        private float _gridH = 400f;
        private bool _savedCursor;
        private bool _iconLogged;
        private CursorLockMode _lockBefore;
        private bool _visibleBefore;
        private GUIStyle _label, _big, _small, _button, _field;

        // Cria o componente (desligado) no objeto do plugin e passa a escutar a tecla de abrir.
        public static void Install(GameObject host, ConfigEntry<string> key)
        {
            if (_instance) return;
            _key = key;
            _instance = host.AddComponent<DropMenu>();
            _instance.enabled = false; // ligado só com a tela aberta: fechada, nem Update nem OnGUI rodam
            _instance.ParseKey();
            RoR2Application.onUpdate += PollKey;
            key.SettingChanged += (_, __) => _instance.ParseKey();
        }

        // Abre a tela (de ror2meeeee_drop_menu); name vazio = sem seleção.
        public static string OpenWith(string name)
        {
            if (!_instance) return "Menu indisponível.";
            if (!Run.instance) return "Entre em uma run primeiro.";
            if (!FeatureOn()) return "Drop desligado (Drop.Enabled=false).";
            if (StatsPanel.IsOpen) return "Feche a tela de status primeiro.";
            _preselect = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
            _instance.SetOpen(true);
            return "Tela de soltar itens aberta.";
        }

        // Lê a tecla configurada em Drop.MenuKey; valor inválido volta para F7.
        private void ParseKey()
        {
            if (!Enum.TryParse(_key.Value, true, out _keyCode)) _keyCode = KeyCode.F7;
        }

        // A DropFeature registrada no plugin (null se o plugin ainda não subiu).
        private static DropFeature DropFeatureInstance()
        {
            var p = Plugin.Instance;
            if (p == null) return null;
            foreach (var f in p.Features) if (f is DropFeature d) return d;
            return null;
        }

        // Drop ligado? (sem ele o comando ror2meeeee_drop não faz nada).
        private static bool FeatureOn()
        {
            var d = DropFeatureInstance();
            return d != null && d.Enabled;
        }

        // Máximo que dá para soltar desta linha em um comando: o que o jogador tem, limitado a Drop.MaxPerCommand.
        private static int MaxQty(Row row)
        {
            var d = DropFeatureInstance();
            return DropRules.ClampCount(int.MaxValue, row.Count, d != null ? d.MaxPerCommand : 0);
        }

        // A tecla é lida aqui (não no Update) porque o componente fica desligado com a tela fechada.
        private static void PollKey()
        {
            try
            {
                if (_instance && Input.GetKeyDown(_instance._keyCode))
                {
                    if (_instance._open) _instance.SetOpen(false); else OpenWith(null);
                }
            }
            catch (Exception e)
            {
                Diag.Log?.LogWarning($"[DropMenu] falha na tecla: {e.Message}");
            }
        }

        // Tela aberta? (a tela de status F8 consulta para não abrir por cima.)
        public static bool IsOpen => _instance && _instance._open;

        // Abre/fecha: liga o componente e guarda/restaura o estado do cursor do jogo.
        private void SetOpen(bool open)
        {
            if (open == _open) return;
            _open = open;
            enabled = open;
            if (open)
            {
                _status = "";
                _refreshAt = 0f;
                _qty = 1;
                _qtyText = "1";
                _lockBefore = Cursor.lockState;
                _visibleBefore = Cursor.visible;
                _savedCursor = true;
            }
            else if (_savedCursor)
            {
                Cursor.lockState = _lockBefore;
                Cursor.visible = _visibleBefore;
                _savedCursor = false;
            }
        }

        // Índice da linha selecionada (-1 se nenhuma ou se o item saiu do inventário).
        private int SelectedIndex()
        {
            for (var i = 0; i < _rows.Count; i++) if (_rows[i].Internal == _selectedName) return i;
            return -1;
        }

        // Seleciona a linha; trocar de item volta a quantidade para 1.
        private void Select(int index)
        {
            if (index < 0 || index >= _rows.Count) return;
            var changed = _rows[index].Internal != _selectedName;
            _selectedName = _rows[index].Internal;
            if (changed) SetQty(1);
            EnsureVisible(index);
        }

        // Rola a grade para a linha ficar visível (navegação pelas setas).
        private void EnsureVisible(int index)
        {
            var top = (index / _cols) * (Cell + Gap);
            if (top < _scroll.y) _scroll.y = top;
            else if (top + Cell > _scroll.y + _gridH) _scroll.y = top + Cell - _gridH;
        }

        // Atualiza a lista a cada 0,5 s e trata o teclado (setas, +/-, Enter).
        private void Update()
        {
            try
            {
                if (!_open) return;
                if (!Run.instance) { SetOpen(false); return; }

                if (Time.unscaledTime >= _refreshAt) { Refresh(); _refreshAt = Time.unscaledTime + 0.5f; }

                var i = SelectedIndex();
                if (Input.GetKeyDown(KeyCode.RightArrow)) Select(i < 0 ? 0 : Mathf.Min(i + 1, _rows.Count - 1));
                if (Input.GetKeyDown(KeyCode.LeftArrow)) Select(i < 0 ? 0 : Mathf.Max(i - 1, 0));
                if (Input.GetKeyDown(KeyCode.DownArrow)) Select(i < 0 ? 0 : Mathf.Min(i + _cols, _rows.Count - 1));
                if (Input.GetKeyDown(KeyCode.UpArrow)) Select(i < 0 ? 0 : Mathf.Max(i - _cols, 0));
                if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.PageUp)) ChangeQty(+1);
                if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.PageDown)) ChangeQty(-1);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) DropSelected();
            }
            catch (Exception e)
            {
                SetOpen(false);
                Diag.Log?.LogWarning($"[DropMenu] falha: {e.Message}");
            }
        }

        // O jogo reasserta o cursor travado a cada frame; reabrimos depois dele.
        private void LateUpdate()
        {
            if (!_open) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Soma delta à quantidade (botões e teclas +/-).
        private void ChangeQty(int delta) => SetQty(_qty + delta);

        // Define a quantidade limitada a [1, MaxQty] e reescreve o campo de texto.
        private void SetQty(int value)
        {
            var i = SelectedIndex();
            _qty = i < 0 ? 1 : Mathf.Clamp(value, 1, Mathf.Max(1, MaxQty(_rows[i])));
            _qtyText = _qty.ToString();
        }

        // Texto digitado no campo: só dígitos; vazio é aceito (a pessoa está apagando) e mantém a quantidade anterior.
        private void OnQtyTyped(string text)
        {
            var digits = new System.Text.StringBuilder();
            foreach (var ch in text) if (ch >= '0' && ch <= '9') digits.Append(ch);
            _qtyText = digits.ToString();
            if (_qtyText.Length > 0 && int.TryParse(_qtyText, out var v) && v != _qty) SetQty(v);
            else if (_qtyText.Length > 0 && !int.TryParse(_qtyText, out _)) SetQty(int.MaxValue); // número grande demais
        }

        // Relê o inventário do jogador local e monta as linhas da grade (ícone, cor do tier, quantidade).
        private void Refresh()
        {
            _rows.Clear();
            var user = LocalUserManager.GetFirstLocalUser();
            var inv = user != null && user.cachedMaster ? user.cachedMaster.inventory : null;
            if (!inv) return;
            Dictionary<ItemIndex, Texture> hud = null;
            foreach (var idx in inv.itemAcquisitionOrder)
            {
                if (hud == null) hud = HudIcons();
                var def = ItemCatalog.GetItemDef(idx);
                var count = inv.GetItemCountPermanent(idx);
                if (!DropFeature.Droppable(def) || count <= 0) continue;
                var color = Color.white;
                var tier = ItemTierCatalog.GetItemTierDef(def.tier);
                if (tier) color = ColorCatalog.GetColor(tier.colorIndex);
                ResolveIcon(def, idx, out var tex, out var uv);
                if (!tex && hud != null) hud.TryGetValue(idx, out tex); // o HUD já desenhou este ícone
                _rows.Add(new Row { Name = Language.GetString(def.nameToken), Internal = def.name, Count = count, Color = color, Tex = tex, Uv = uv });
            }

            if (!_iconLogged && _rows.Count > 0)
            {
                _iconLogged = true;
                var missing = _rows.FindAll(r => !r.Tex).Count;
                Diag.Log?.LogInfo($"[DropMenu] ícones: {_rows.Count - missing} de {_rows.Count} itens com imagem{(missing > 0 ? " (o resto mostra o nome)" : "")}");
                if (missing > 0) LogIconFields(ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(_rows.Find(x => !x.Tex).Internal)));
            }

            if (_preselect != null)
            {
                var hit = _rows.FindIndex(r => string.Equals(r.Internal, _preselect, StringComparison.OrdinalIgnoreCase)
                                               || string.Equals(r.Name, _preselect, StringComparison.OrdinalIgnoreCase));
                if (hit < 0) hit = _rows.FindIndex(r => r.Internal.IndexOf(_preselect, StringComparison.OrdinalIgnoreCase) >= 0
                                                        || r.Name.IndexOf(_preselect, StringComparison.OrdinalIgnoreCase) >= 0);
                if (hit >= 0) Select(hit); else _status = $"Nenhum item seu combina com '{_preselect}'.";
                _preselect = null;
            }

            var sel = SelectedIndex();
            if (sel < 0 && _rows.Count > 0) { _selectedName = _rows[0].Internal; sel = 0; }
            // Só reescreve o campo se o limite mudou: não apaga o que a pessoa está digitando.
            if (sel >= 0 && Mathf.Clamp(_qty, 1, Mathf.Max(1, MaxQty(_rows[sel]))) != _qty) SetQty(_qty);
        }

        // Envia ror2meeeee_drop com a quantidade que o servidor vai de fato soltar e já desconta da tela
        // (a releitura do inventário corrige se o servidor recusar).
        private void DropSelected()
        {
            var i = SelectedIndex();
            if (i < 0) return;
            var row = _rows[i];
            var count = Mathf.Min(_qty, MaxQty(row));
            if (count <= 0) { _status = "Nada para soltar (Drop.MaxPerCommand = 0?)."; return; }
            var user = LocalUserManager.GetFirstLocalUser();
            if (user == null || !user.currentNetworkUser) { _status = "Sem usuário local."; return; }
            RoR2.Console.instance.SubmitCmd(user.currentNetworkUser, $"ror2meeeee_drop {row.Internal} {count}");
            _status = $"Soltando {count}x {row.Name}...";

            row.Count -= count;
            if (row.Count > 0) { _rows[i] = row; SetQty(_qty); }
            else _rows.RemoveAt(i); // acabou: a próxima releitura escolhe outro item
            _refreshAt = Time.unscaledTime + 1f; // dá tempo do servidor remover e do inventário sincronizar
        }

        // Estilos do IMGUI (criados uma vez, dentro do OnGUI, onde GUI.skin existe).
        private void MakeStyles()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
            _big = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, wordWrap = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 30, fontStyle = FontStyle.Bold };
            _field = new GUIStyle(GUI.skin.textField) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }

        // Desenha uma moldura de espessura t em volta do retângulo.
        private static void Border(Rect r, Color c, float t)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        // 1.5: ItemDef.pickupIconSprite está obsoleto (só para modders) e vem nulo; a imagem real é pickupIconTexture
        // (carregada pela referência). Ordem: textura do item, textura/sprite do pickup, sprite antigo do item.
        private static void ResolveIcon(ItemDef def, ItemIndex idx, out Texture tex, out Rect uv)
        {
            tex = null;
            uv = new Rect(0f, 0f, 1f, 1f);
            try { tex = def.pickupIconTexture; } catch (Exception) { /* tenta o próximo */ }
            if (tex) return;
            Sprite sprite = null;
            try
            {
                var pd = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(idx));
                if (pd != null)
                {
                    tex = pd.iconTexture;
                    if (tex) return;
                    sprite = pd.iconSprite;
                }
            }
            catch (Exception) { /* tenta o próximo */ }
            if (!sprite) sprite = def.pickupIconSprite;
            if (sprite && sprite.texture)
            {
                var tr = sprite.textureRect;
                var t = sprite.texture;
                tex = t;
                uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);
            }
        }

        // Ícones que a própria UI do jogo já está mostrando (inventário do HUD): itemIndex -> imagem.
        private static Dictionary<ItemIndex, Texture> HudIcons()
        {
            var map = new Dictionary<ItemIndex, Texture>();
            try
            {
                foreach (var icon in UnityEngine.Object.FindObjectsOfType<RoR2.UI.ItemIcon>())
                {
                    if (!icon || !icon.image || !icon.image.texture || icon.itemIndex == ItemIndex.None) continue;
                    if (!map.ContainsKey(icon.itemIndex)) map[icon.itemIndex] = icon.image.texture;
                }
            }
            catch (Exception e)
            {
                Diag.Log?.LogWarning($"[DropMenu] não consegui ler os ícones do HUD: {e.Message}");
            }
            return map;
        }

        // Diagnóstico único: quais campos de ícone o ItemDef desta versão tem e se vêm preenchidos.
        private static void LogIconFields(ItemDef def)
        {
            try
            {
                var sb = new System.Text.StringBuilder($"[DropMenu] campos de ícone de ItemDef ({def.name}):");
                const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                foreach (var f in typeof(ItemDef).GetFields(F))
                    if (f.Name.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0)
                        sb.Append(' ').Append(f.Name).Append(':').Append(f.FieldType.Name).Append(f.GetValue(def) == null ? "=nulo" : "=ok").Append(';');
                foreach (var pr in typeof(ItemDef).GetProperties(F))
                    if (pr.Name.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0 && pr.GetIndexParameters().Length == 0)
                    {
                        object v = null;
                        try { v = pr.GetValue(def, null); } catch (Exception) { /* getter falhou */ }
                        sb.Append(' ').Append(pr.Name).Append(':').Append(pr.PropertyType.Name).Append(v == null ? "=nulo" : "=ok").Append(';');
                    }
                Diag.Log?.LogInfo(sb.ToString());
            }
            catch (Exception e)
            {
                Diag.Log?.LogWarning($"[DropMenu] diagnóstico de ícones falhou: {e.Message}");
            }
        }

        // Desenha o ícone do item ou, sem imagem, um quadrado escuro na cor do tier.
        private static void DrawIcon(Rect r, Texture tex, Rect uv, Color fallback)
        {
            if (tex)
            {
                GUI.DrawTextureWithTexCoords(r, tex, uv);
            }
            else
            {
                var old = GUI.color;
                GUI.color = new Color(fallback.r * 0.35f, fallback.g * 0.35f, fallback.b * 0.35f, 1f); // escuro: o nome vai por cima
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = old;
            }
        }

        // Desenha a tela: grade de itens à esquerda, painel do item escolhido à direita e rodapé de ajuda.
        private void OnGUI()
        {
            if (!_open) return;
            MakeStyles();

            // Coordenadas virtuais ~1080p, escaladas pela resolução real (a tela antiga ficava minúscula em 1440p/4K).
            var scale = Mathf.Max(0.8f, Screen.height / 1080f) * 1.15f;
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var W = Screen.width / scale;
            var H = Screen.height / scale;

            var pw = Mathf.Min(1180f, W - 60f);
            var ph = Mathf.Min(720f, H - 60f);
            var panel = new Rect((W - pw) / 2f, (H - ph) / 2f, pw, ph);
            var bg = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.92f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = bg;
            Border(panel, new Color(0.9f, 0.8f, 0.4f), 3f);

            GUI.Label(new Rect(panel.x + 24, panel.y + 12, pw - 48, 44), "SOLTAR ITENS", _big);

            var gridW = pw * 0.58f;
            var grid = new Rect(panel.x + 24, panel.y + 70, gridW, ph - 150f);
            _gridH = grid.height;
            _cols = Mathf.Max(1, (int)((grid.width - 24f) / (Cell + Gap)));
            var rowsN = Mathf.Max(1, Mathf.CeilToInt(_rows.Count / (float)_cols));
            var view = new Rect(0, 0, _cols * (Cell + Gap), rowsN * (Cell + Gap));
            var sel = SelectedIndex();

            if (_rows.Count == 0) GUI.Label(grid, "Você não tem itens que possam ser soltos.", _label);
            _scroll = GUI.BeginScrollView(grid, _scroll, view);
            for (var i = 0; i < _rows.Count; i++)
            {
                var r = new Rect((i % _cols) * (Cell + Gap), (i / _cols) * (Cell + Gap), Cell, Cell);
                var c = GUI.color;
                GUI.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = c;
                DrawIcon(new Rect(r.x + 6, r.y + 6, Cell - 12, Cell - 12), _rows[i].Tex, _rows[i].Uv, _rows[i].Color);
                if (!_rows[i].Tex) GUI.Label(new Rect(r.x + 4, r.y + 4, Cell - 8, Cell - 34), _rows[i].Name, _small);
                Border(r, i == sel ? new Color(1f, 0.9f, 0.3f) : _rows[i].Color * 0.7f, i == sel ? 5f : 2f);
                GUI.Label(new Rect(r.x, r.yMax - 30, r.width - 6, 28), "x" + _rows[i].Count, new GUIStyle(_small) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold });
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) Select(i);
            }
            GUI.EndScrollView();

            // Painel do item escolhido
            var dx = panel.x + 24 + gridW + 24;
            var dw = panel.xMax - dx - 24;
            if (sel >= 0)
            {
                var row = _rows[sel];
                DrawIcon(new Rect(dx, panel.y + 74, 150, 150), row.Tex, row.Uv, row.Color);
                var nameStyle = new GUIStyle(_big) { fontSize = 28 };
                nameStyle.normal.textColor = row.Color;
                GUI.Label(new Rect(dx + 164, panel.y + 74, dw - 164, 100), row.Name, nameStyle);
                GUI.Label(new Rect(dx + 164, panel.y + 170, dw - 164, 50), $"Você tem {row.Count}", _label);

                var max = MaxQty(row);
                GUI.Label(new Rect(dx, panel.y + 240, dw, 40), $"Quantidade a soltar (máx. {max} por vez)", _label);
                var typed = GUI.TextField(new Rect(dx, panel.y + 284, dw, 72), _qtyText, 6, _field);
                if (typed != _qtyText) OnQtyTyped(typed);
                var bw = (dw - 3 * 8f) / 4f;
                var by = panel.y + 366;
                if (GUI.Button(new Rect(dx, by, bw, 64), "−1", _button)) ChangeQty(-1);
                if (GUI.Button(new Rect(dx + (bw + 8f), by, bw, 64), "+1", _button)) ChangeQty(+1);
                if (GUI.Button(new Rect(dx + 2 * (bw + 8f), by, bw, 64), "+5", _button)) ChangeQty(+5);
                if (GUI.Button(new Rect(dx + 3 * (bw + 8f), by, bw, 64), "+10", _button)) ChangeQty(+10);
                if (GUI.Button(new Rect(dx, by + 80, dw, 90), $"SOLTAR {_qty}x", _button)) DropSelected();
            }
            else
            {
                GUI.Label(new Rect(dx, panel.y + 80, dw, 120), "Clique em um item para escolher.", _label);
            }

            var foot = new Rect(panel.x + 24, panel.yMax - 66, pw - 48, 56);
            GUI.Label(foot, (_status.Length > 0 ? _status + "\n" : "") + $"Clique no item · setas movem · digite ou +/- quantidade · Enter solta · {_key.Value} fecha", _small);

            GUI.matrix = oldMatrix;
        }
    }
}
