using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace Ror2Meeeee
{
    // Modelo 3D da pilha de pregos, gerado em código: 3 pregos de revolução num único mesh (ProcModel).
    internal static class NailModel
    {
        // Perfil do prego (raio, altura), da ponta à cabeça.
        private static readonly Vector2[] Profile =
        {
            new Vector2(0f, -0.5f), new Vector2(0.03f, -0.38f), new Vector2(0.03f, 0.44f),
            new Vector2(0.1f, 0.44f), new Vector2(0.1f, 0.5f), new Vector2(0f, 0.5f),
        };
        private static readonly float[] V = { 0f, 0.12f, 0.94f, 0.94f, 1f, 1f }; // altura + 0.5
        private const int Sides = 12;

        // Devolve null se falhar; quem chama mantém o modelo anterior.
        public static GameObject Build(string name, Color32 rust, Color32 spots, Color emission, ManualLogSource log)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            ProcModel.AddLathe(Profile, V, Sides, Matrix4x4.TRS(new Vector3(-0.1f, 0f, 0f), Quaternion.Euler(0f, 0f, 14f), Vector3.one), verts, uvs, tris);
            ProcModel.AddLathe(Profile, V, Sides, Matrix4x4.TRS(new Vector3(0.1f, -0.04f, 0.05f), Quaternion.Euler(12f, 30f, -20f), new Vector3(1f, 0.9f, 1f)), verts, uvs, tris);
            ProcModel.AddLathe(Profile, V, Sides, Matrix4x4.TRS(new Vector3(0f, -0.42f, -0.05f), Quaternion.Euler(0f, -25f, 85f), new Vector3(1f, 0.8f, 1f)), verts, uvs, tris);
            return ProcModel.Build(name, verts, uvs, tris, ProcModel.Bands("Ror2MeeeeeRust", (rust, spots)), 0.15f, emission, log);
        }
    }
}
