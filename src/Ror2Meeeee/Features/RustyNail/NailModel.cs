using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Ror2Meeeee
{
    // Modelo 3D da pilha de pregos, gerado em código (sem AssetBundle): 3 pregos de revolução num único mesh,
    // porque o PickupDisplay escala o modelo pelo bounds do primeiro renderer. Sem collider (não atrapalha o pickup).
    internal static class NailModel
    {
        // Perfil do prego (raio, altura), da ponta à cabeça; cada par vira uma faixa com aresta viva.
        private static readonly Vector2[] Profile =
        {
            new Vector2(0f, -0.5f), new Vector2(0.03f, -0.38f), new Vector2(0.03f, 0.44f),
            new Vector2(0.1f, 0.44f), new Vector2(0.1f, 0.5f), new Vector2(0f, 0.5f),
        };
        private const int Sides = 12;

        private static GameObject _holder; // inativo: guarda os "prefabs" sem que apareçam na cena

        // Devolve null se falhar; quem chama mantém o modelo anterior.
        public static GameObject Build(string name, Color32 rust, Color32 spots, Color emission, ManualLogSource log)
        {
            try
            {
                var shader = Addressables.LoadAssetAsync<Shader>("RoR2/Base/Shaders/HGStandard.shader").WaitForCompletion();
                if (!shader) { log.LogWarning($"{name}: shader HGStandard indisponível; mantendo o modelo do jogo."); return null; }

                var verts = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();
                AddNail(Matrix4x4.TRS(new Vector3(-0.1f, 0f, 0f), Quaternion.Euler(0f, 0f, 14f), Vector3.one), verts, uvs, tris);
                AddNail(Matrix4x4.TRS(new Vector3(0.1f, -0.04f, 0.05f), Quaternion.Euler(12f, 30f, -20f), new Vector3(1f, 0.9f, 1f)), verts, uvs, tris);
                AddNail(Matrix4x4.TRS(new Vector3(0f, -0.42f, -0.05f), Quaternion.Euler(0f, -25f, 85f), new Vector3(1f, 0.8f, 1f)), verts, uvs, tris);

                // Centraliza: o PickupDisplay posiciona pela origem do modelo.
                var bounds = new Bounds(verts[0], Vector3.zero);
                foreach (var v in verts) bounds.Encapsulate(v);
                for (var i = 0; i < verts.Count; i++) verts[i] -= bounds.center;

                var mesh = new Mesh { name = name + "Mesh" };
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                var mat = new Material(shader) { name = name + "Mat" };
                mat.SetColor("_Color", Color.white);
                mat.SetTexture("_MainTex", RustTexture(rust, spots));
                mat.SetFloat("_Smoothness", 0.15f);
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

        private static void AddNail(Matrix4x4 m, List<Vector3> verts, List<Vector2> uvs, List<int> tris)
        {
            for (var p = 0; p < Profile.Length - 1; p++)
            {
                var a = Profile[p];
                var b = Profile[p + 1];
                var start = verts.Count;
                for (var i = 0; i <= Sides; i++)
                {
                    var ang = i * 2f * Mathf.PI / Sides;
                    var c = Mathf.Cos(ang);
                    var s = Mathf.Sin(ang);
                    verts.Add(m.MultiplyPoint3x4(new Vector3(a.x * c, a.y, a.x * s)));
                    verts.Add(m.MultiplyPoint3x4(new Vector3(b.x * c, b.y, b.x * s)));
                    uvs.Add(new Vector2((float)i / Sides, a.y + 0.5f));
                    uvs.Add(new Vector2((float)i / Sides, b.y + 0.5f));
                }
                for (var i = 0; i < Sides; i++)
                {
                    int lo = start + i * 2, hi = lo + 1, lo2 = lo + 2, hi2 = lo + 3;
                    if (a.x > 0f) { tris.Add(lo); tris.Add(hi); tris.Add(lo2); } // raio 0: triângulo degenerado
                    if (b.x > 0f) { tris.Add(hi); tris.Add(hi2); tris.Add(lo2); }
                }
            }
        }

        // Ferrugem manchada: ruído fixo (mesma aparência em toda run).
        private static Texture2D RustTexture(Color32 rust, Color32 spots)
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Ror2MeeeeeRust", wrapMode = TextureWrapMode.Repeat };
            var rng = new System.Random(1);
            var px = new Color32[size * size];
            for (var i = 0; i < px.Length; i++) px[i] = Color32.Lerp(rust, spots, (float)rng.NextDouble());
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
