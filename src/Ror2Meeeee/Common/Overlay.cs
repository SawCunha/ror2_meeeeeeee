using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace Ror2Meeeee
{
    // Aviso e marcadores na tela (IMGUI, só local: o chat vai para todos, isto é para quem roda o mod).
    // Sem nada ativo, OnGUI sai na primeira linha. Marcador fora da tela vira seta presa à borda.
    // (0.7.0 desligava o componente quando ocioso; na 0.7.1 voltou a ficar sempre ligado: marcadores sumiram em jogo.)
    internal sealed class Overlay : MonoBehaviour
    {
        private sealed class Marker
        {
            public object Key;
            public Vector3 Position;
            public Transform Follow; // se definido, segue o objeto; some quando ele é destruído
            public string Label;
            public Color Color;
            public int Size;
        }

        private static Overlay _instance;
        private static readonly List<Marker> Markers = new List<Marker>();
        private static string _banner;
        private static float _bannerUntil;
        private static GUIStyle _style;

        public static void Install(GameObject host)
        {
            if (!_instance) _instance = host.AddComponent<Overlay>();
        }

        public static void Banner(string text, float seconds = 8f)
        {
            _banner = text;
            _bannerUntil = Time.unscaledTime + seconds;
        }

        public static void Mark(object key, Vector3 position, string label, Color color, int size = 18) => Add(key, position, null, label, color, size);

        public static void Mark(object key, Transform follow, string label, Color color, int size = 18) => Add(key, Vector3.zero, follow, label, color, size);

        public static void Unmark(object key) => Markers.RemoveAll(m => m.Key == key);

        public static void Clear() => Markers.Clear();

        private static void Add(object key, Vector3 position, Transform follow, string label, Color color, int size)
        {
            Unmark(key);
            Markers.Add(new Marker { Key = key, Position = position, Follow = follow, Label = label, Color = color, Size = size });
        }

        private static Camera SceneCamera()
        {
            var list = CameraRigController.readOnlyInstancesList;
            for (var i = 0; i < list.Count; i++)
                if (list[i] && list[i].sceneCam && list[i].sceneCam.enabled) return list[i].sceneCam;
            return Camera.main;
        }

        private void OnGUI()
        {
            var showBanner = _banner != null && Time.unscaledTime < _bannerUntil;
            if (!showBanner && Markers.Count == 0) return;

            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = false };

            if (showBanner) DrawText(new Rect(0, Screen.height * 0.12f, Screen.width, 40), _banner, new Color(1f, 0.9f, 0.4f), 18);

            if (Markers.Count == 0) return;
            var cam = SceneCamera();
            if (!cam) return;

            for (var i = Markers.Count - 1; i >= 0; i--)
            {
                var m = Markers[i];
                if (m.Follow != null && !m.Follow) { Markers.RemoveAt(i); continue; }
                var world = m.Follow ? m.Follow.position + Vector3.up * 2f : m.Position + Vector3.up * 2f;
                var sp = cam.WorldToScreenPoint(world);
                var behind = sp.z < 0f;
                if (behind) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; } // espelha: aponta para o lado certo
                const float pad = 60f;
                var onScreen = !behind && sp.x > pad && sp.x < Screen.width - pad && sp.y > pad && sp.y < Screen.height - pad;
                var x = Mathf.Clamp(sp.x, pad, Screen.width - pad);
                var y = Mathf.Clamp(sp.y, pad, Screen.height - pad);
                var dist = Vector3.Distance(cam.transform.position, world);
                var text = (onScreen ? "▼ " : "◆ ") + m.Label + $" ({dist:F0} m)";
                var h = m.Size * 2f;
                DrawText(new Rect(x - 300, Screen.height - y - h / 2f, 600, h), text, m.Color, m.Size);
            }
        }

        private static void DrawText(Rect r, string text, Color color, int size)
        {
            _style.fontSize = size;
            var old = GUI.color;
            GUI.color = Color.black;
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), text, _style);
            GUI.color = color;
            GUI.Label(r, text, _style);
            GUI.color = old;
        }
    }
}
