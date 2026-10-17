using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Ror2Meeeee
{
    // Modelos 3D gerados em código (sem AssetBundle): sólidos de revolução num único mesh, porque o
    // PickupDisplay escala o modelo pelo bounds do primeiro renderer. Sem collider (não atrapalha o pickup).
    internal static class ProcModel
    {
        private static GameObject _holder; // inativo: guarda os "prefabs" sem que apareçam na cena

        // Gira o perfil (raio, altura) em torno de Y; cada par vira uma faixa com aresta viva.
        // v[i] = coordenada V da textura no ponto i do perfil. Perfil percorrido com a face externa à direita
        // (de baixo para cima no lado de fora), senão as normais ficam para dentro.
        public static void AddLathe(Vector2[] profile, float[] v, int sides, Matrix4x4 m, List<Vector3> verts, List<Vector2> uvs, List<int> tris)
        {
            for (var p = 0; p < profile.Length - 1; p++)
            {
                var a = profile[p];
                var b = profile[p + 1];
                var start = verts.Count;
                for (var i = 0; i <= sides; i++)
                {
                    var ang = i * 2f * Mathf.PI / sides;
                    var c = Mathf.Cos(ang);
                    var s = Mathf.Sin(ang);
                    verts.Add(m.MultiplyPoint3x4(new Vector3(a.x * c, a.y, a.x * s)));
                    verts.Add(m.MultiplyPoint3x4(new Vector3(b.x * c, b.y, b.x * s)));
                    uvs.Add(new Vector2((float)i / sides, v[p]));
                    uvs.Add(new Vector2((float)i / sides, v[p + 1]));
                }
                for (var i = 0; i < sides; i++)
                {
                    int lo = start + i * 2, hi = lo + 1, lo2 = lo + 2, hi2 = lo + 3;
                    if (a.x > 0f) { tris.Add(lo); tris.Add(hi); tris.Add(lo2); } // raio 0: triângulo degenerado
                    if (b.x > 0f) { tris.Add(hi); tris.Add(hi2); tris.Add(lo2); }
                }
            }
        }

        // Monta o "prefab" (centralizado, shader HGStandard do jogo). Devolve null se falhar; quem chama mantém o modelo anterior.
        public static GameObject Build(string name, List<Vector3> verts, List<Vector2> uvs, List<int> tris,
            Texture2D tex, float smoothness, Color emission, ManualLogSource log)
        {
            try
            {
                var shader = Addressables.LoadAssetAsync<Shader>("RoR2/Base/Shaders/HGStandard.shader").WaitForCompletion();
                if (!shader) { log.LogWarning($"{name}: shader HGStandard indisponível; mantendo o modelo do jogo."); return null; }

                // Centraliza: o PickupDisplay posiciona pela origem do modelo.
                var bounds = new Bounds(verts[0], Vector3.zero);
                foreach (var p in verts) bounds.Encapsulate(p);
                for (var i = 0; i < verts.Count; i++) verts[i] -= bounds.center;

                var mesh = new Mesh { name = name + "Mesh" };
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                var mat = new Material(shader) { name = name + "Mat" };
                mat.SetColor("_Color", Color.white);
                mat.SetTexture("_MainTex", tex);
                mat.SetFloat("_Smoothness", smoothness);
                if (emission.maxColorComponent > 0f)
                {
                    mat.SetColor("_EmColor", emission);
                    mat.SetFloat("_EmPower", 1f);
                }

                if (!_holder)
                {
                    _holder = new GameObject("Ror2MeeeeeModels");
                    _holder.SetActive(false);
                    UnityEngine.Object.DontDestroyOnLoad(_holder);
                }
                var go = new GameObject(name);
                go.transform.SetParent(_holder.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                return go;
            }
            catch (Exception e)
            {
                log.LogWarning($"{name}: falha ao gerar o modelo ({e.GetType().Name}); mantendo o modelo do jogo.");
                return null;
            }
        }

        // Textura manchada entre duas cores por faixas de V: rows linhas por faixa, (a, b) por faixa.
        // Ruído fixo (mesma aparência em toda run). Com mais de uma faixa, point filter: a fronteira fica nítida.
        public static Texture2D Bands(string name, params (Color32 a, Color32 b)[] bands)
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = bands.Length > 1 ? FilterMode.Point : FilterMode.Bilinear };
            var rng = new System.Random(1);
            var px = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                var band = bands[Math.Min(bands.Length - 1, y * bands.Length / size)];
                for (var x = 0; x < size; x++) px[y * size + x] = Color32.Lerp(band.a, band.b, (float)rng.NextDouble());
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
