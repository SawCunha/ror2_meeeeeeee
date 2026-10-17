using System.Collections.Generic;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Stats;
using UnityEngine;

namespace Ror2Meeeee
{
    // Item void verde "zzz cervo <3": toda a velocidade de movimento extra vira dano.
    // Não corrompe nenhum item; sai em Void Cradles/Potentials. O efeito é aplicado pela CervoFeature.
    public static class CervoItem
    {
        public const string Name = "Ror2MeeeeeCervo";
        public const string Token = "ROR2MEEEEE_ITEM_CERVO";

        private static ItemDef _def;
        private static ItemIndex _index = ItemIndex.None;

        // ItemIndex só existe depois do ItemCatalog; resolvido uma vez e guardado.
        public static ItemIndex Index
        {
            get
            {
                if (_index == ItemIndex.None && _def) _index = _def.itemIndex;
                return _index;
            }
        }

        public static void Register(ManualLogSource log)
        {
            var extra = (int)(SpeedToDamage.ExtraStackRatio * 100f);
            _def = ModItem.Register(Name, Token, ItemTier.VoidTier2, new[] { ItemTag.Damage, ItemTag.AIBlacklist },
                "cervo.png", new Color32(150, 60, 170, 255),
                "zzz cervo <3",
                "A pressa é inimiga da perfeição.",
                "Converte toda a <style=cIsUtility>velocidade de movimento adicional</style> em <style=cIsDamage>dano</style>: " +
                "cada +1% de velocidade vira <style=cIsDamage>+1% de dano</style> " +
                $"<style=cStack>(+{extra}% de conversão por acúmulo)</style>. Você fica na velocidade normal; lentidões continuam valendo.",
                "A pressa é inimiga da perfeição.",
                log, "RoR2/Base/Hoof/PickupHoof.prefab");
            if (_def) _def.pickupModelPrefab = Model(log) ?? _def.pickupModelPrefab;
        }

        // Cervo de pé, como o ícone: corpo, pescoço e cabeça marrons, olhos esbugalhados brancos com pupila preta,
        // boca aberta preta, orelhas em cone e quatro patas finas com casco preto. Peças de revolução num mesh só.
        private const float Brown = 0.17f, White = 0.5f, Black = 0.83f; // V da faixa na textura
        private static readonly Vector2[] SphereProfile = Sphere(8);
        private static readonly Vector2[] Cylinder = { new Vector2(0f, -0.5f), new Vector2(1f, -0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f) };
        private static readonly Vector2[] Cone = { new Vector2(0f, -0.5f), new Vector2(1f, -0.5f), new Vector2(0f, 0.5f) };

        private static GameObject Model(ManualLogSource log)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            void Part(Vector2[] profile, float band, Vector3 pos, Vector3 euler, Vector3 scale) =>
                ProcModel.AddLathe(profile, Fill(profile.Length, band), 12, Matrix4x4.TRS(pos, Quaternion.Euler(euler), scale), verts, uvs, tris);

            Part(SphereProfile, Brown, new Vector3(0f, 0.05f, 0f), new Vector3(-15f, 0f, 0f), new Vector3(0.15f, 0.24f, 0.15f));  // corpo
            Part(Cylinder, Brown, new Vector3(0f, 0.32f, 0.04f), new Vector3(10f, 0f, 0f), new Vector3(0.05f, 0.25f, 0.05f));     // pescoço
            Part(SphereProfile, Brown, new Vector3(0f, 0.48f, 0.06f), Vector3.zero, new Vector3(0.12f, 0.11f, 0.13f));            // cabeça
            Part(SphereProfile, Black, new Vector3(0f, 0.43f, 0.17f), Vector3.zero, new Vector3(0.05f, 0.065f, 0.025f));          // boca aberta
            for (var side = -1; side <= 1; side += 2)
            {
                Part(SphereProfile, White, new Vector3(0.07f * side, 0.56f, 0.13f), Vector3.zero, new Vector3(0.065f, 0.065f, 0.065f)); // olho
                Part(SphereProfile, Black, new Vector3(0.07f * side, 0.56f, 0.19f), Vector3.zero, new Vector3(0.03f, 0.03f, 0.015f));   // pupila
                Part(Cone, Brown, new Vector3(0.11f * side, 0.62f, 0.02f), new Vector3(0f, 0f, -40f * side), new Vector3(0.03f, 0.15f, 0.03f)); // orelha
                for (var front = -1; front <= 1; front += 2)
                {
                    var x = 0.08f * side;
                    var z = 0.06f * front;
                    var splay = new Vector3(-6f * front, 0f, -5f * side);
                    Part(Cylinder, Brown, new Vector3(x, -0.36f, z), splay, new Vector3(0.025f, 0.45f, 0.025f));                     // pata
                    Part(Cone, Black, new Vector3(x * 1.2f, -0.6f, z * 1.2f), new Vector3(180f, 0f, 0f), new Vector3(0.035f, 0.06f, 0.035f)); // casco
                }
            }

            var tex = ProcModel.Bands(Name + "Tex",
                (new Color32(80, 45, 25, 255), new Color32(105, 60, 35, 255)),
                (new Color32(235, 235, 235, 255), new Color32(255, 255, 255, 255)),
                (new Color32(10, 8, 8, 255), new Color32(30, 20, 20, 255)));
            return ProcModel.Build(Name, verts, uvs, tris, tex, 0.2f, Color.black, log);
        }

        // Semicírculo de raio 1, de baixo para cima (face externa à direita, como o ProcModel espera).
        private static Vector2[] Sphere(int segments)
        {
            var p = new Vector2[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                var t = Mathf.PI * i / segments;
                p[i] = new Vector2(Mathf.Sin(t), -Mathf.Cos(t));
            }
            p[0].x = 0f;
            p[segments].x = 0f;
            return p;
        }

        private static float[] Fill(int n, float v)
        {
            var a = new float[n];
            for (var i = 0; i < n; i++) a[i] = v;
            return a;
        }
    }
}
