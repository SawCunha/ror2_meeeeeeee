using System;
using BepInEx.Logging;
using R2API;
using RoR2;
using UnityEngine;

#pragma warning disable CS0618 // smallIcon*Sprite: campos obsoletos no 1.5.0, mas ainda lidos pelo jogo

namespace Ror2Meeeee
{
    // Artefato Taxade: com ele ligado no lobby, toda compra paga com ouro custa vida ao jogador (Tax.Percent da vida máxima).
    // O efeito é aplicado pela TaxFeature (precisa de Tax.Enabled = true).
    public static class TaxArtifact
    {
        public const string Name = "Ror2MeeeeeTax";
        public const string Token = "ROR2MEEEEE_ARTIFACT_TAX";

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
                _def.smallIconSelectedSprite = TitheArtifact.LoadIcon("taxade_selecionado.png", new Color32(190, 40, 40, 255));
                _def.smallIconDeselectedSprite = TitheArtifact.LoadIcon("taxade_desmarcado.png", new Color32(90, 90, 90, 255));

                LanguageAPI.Add(Token + "_NAME", "Artefato Taxade");
                LanguageAPI.Add(Token + "_DESC", "Toda compra paga com ouro (baús, santuários, drones...) custa vida ao jogador (30% da vida máxima por padrão).");

                if (!ContentAddition.AddArtifactDef(_def))
                {
                    log.LogError("Tax: ContentAddition recusou o artefato.");
                    _def = null;
                }
                else
                    log.LogInfo($"Tax: artefato '{Name}' registrado.");
            }
            catch (Exception e)
            {
                log.LogError($"Tax: falha ao registrar o artefato (o plugin segue sem ele): {e}");
                _def = null;
            }
        }

        public static bool IsEnabled()
        {
            var mgr = RunArtifactManager.instance;
            return _def && mgr && mgr.IsArtifactEnabled(_def);
        }
    }
}
