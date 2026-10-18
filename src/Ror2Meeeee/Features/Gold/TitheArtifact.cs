using System;
using BepInEx.Logging;
using R2API;
using RoR2;
using UnityEngine;

#pragma warning disable CS0618 // smallIcon*Sprite: campos obsoletos no 1.5.0, mas ainda lidos pelo jogo

namespace Ror2Meeeee
{
    // Artefato do Dízimo: com ele ligado no lobby, todo ouro ganho por jogadores também vira
    // crédito de spawn do diretor. O efeito é aplicado pela GoldFeature (precisa de Gold.Enabled = true).
    public static class TitheArtifact
    {
        public const string Name = "Ror2MeeeeeTithe";
        public const string Token = "ROR2MEEEEE_ARTIFACT_TITHE";

        private static ArtifactDef _def;

        // Chamar no Awake do plugin (antes de o ArtifactCatalog existir).
        public static void Register(ManualLogSource log)
        {
            try
            {
                _def = ScriptableObject.CreateInstance<ArtifactDef>();
                _def.cachedName = Name;
                _def.nameToken = Token + "_NAME";
                _def.descriptionToken = Token + "_DESC";
                _def.smallIconSelectedSprite = LoadIcon("dizimo_selecionado.png", new Color32(230, 180, 30, 255));
                _def.smallIconDeselectedSprite = LoadIcon("dizimo_desmarcado.png", new Color32(90, 90, 90, 255));

                Loc.Add(Token + "_NAME", "Artifact of Tithe", "Artefato do Dízimo");
                Loc.Add(Token + "_DESC", "All gold earned by the players is also added to the Director's spawn credit (10%).", "Todo ouro ganho pelos jogadores também é adicionado ao crédito de spawn do Diretor (10%).");

                if (!ContentAddition.AddArtifactDef(_def))
                {
                    log.LogError("Tithe: ContentAddition recusou o artefato.");
                    _def = null;
                }
                else
                    log.LogInfo($"Tithe: artefato '{Name}' registrado.");
            }
            catch (Exception e)
            {
                log.LogError($"Tithe: falha ao registrar o artefato (o plugin segue sem ele): {e}");
                _def = null;
            }
        }

        // Ligado na run atual? (artifactIndex só existe depois do ArtifactCatalog.)
        public static bool IsEnabled()
        {
            var mgr = RunArtifactManager.instance;
            return _def && mgr && mgr.IsArtifactEnabled(_def);
        }

        // PNG embutido (Assets/artifacts/<nome>.png); se faltar ou não decodificar, cai no ícone gerado.
        internal static Sprite LoadIcon(string resource, Color32 fallback)
        {
            try
            {
                using (var stream = typeof(TitheArtifact).Assembly.GetManifestResourceStream(resource))
                {
                    if (stream != null)
                    {
                        var bytes = new byte[stream.Length];
                        var read = 0;
                        while (read < bytes.Length)
                        {
                            var n = stream.Read(bytes, read, bytes.Length - read);
                            if (n <= 0) break;
                            read += n;
                        }
                        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (tex.LoadImage(bytes, true))
                            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
            }
            catch (Exception) { /* cai no gerado */ }
            return MakeIcon(fallback);
        }

        internal static Sprite MakeIcon(Color32 fill)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var edge = new Color32(40, 40, 40, 255);
            var px = new Color32[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                    px[y * size + x] = (x < 8 || y < 8 || x >= size - 8 || y >= size - 8) ? edge : fill;
            tex.SetPixels32(px);
            tex.Apply(false, true); // sem cópia legível na RAM
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
