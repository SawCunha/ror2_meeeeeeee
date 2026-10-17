using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RoR2;
using Ror2Meeeee.Core.Stats;
using UnityEngine;

namespace Ror2Meeeee
{
    // Efeito do item "zzz cervo <3": depois do RecalculateStats do jogo, corta a velocidade de movimento
    // de volta à neutra (base + nível, com sprint) e converte o excedente em dano (Core.Stats.SpeedToDamage).
    // EXCEÇÃO à regra "só no host": stats são calculados em toda máquina, como nos itens do jogo; o movimento
    // do jogador roda no cliente dele, então o corte precisa acontecer lá também. O inventário é sincronizado.
    // O hook em RecalculateStats (BodyHooks, passo 3; roda para todo corpo) só existe enquanto alguém tem o item;
    // a lista de portadores é mantida por eventos de inventário/corpo, em toda máquina (o inventário é sincronizado).
    public sealed class CervoFeature : Feature, IHolderSet
    {
        // Setters privados do CharacterBody, como delegates (sem boxing nem reflexão por chamada).
        private static readonly Action<CharacterBody, float> SetMoveSpeed = Setter("moveSpeed");
        private static readonly Action<CharacterBody, float> SetDamage = Setter("damage");
        private static readonly Action<CharacterBody, float> SetAcceleration = Setter("acceleration");

        private static Action<CharacterBody, float> Setter(string property)
        {
            var set = typeof(CharacterBody).GetProperty(property)?.GetSetMethod(true);
            return set == null ? null : (Action<CharacterBody, float>)Delegate.CreateDelegate(typeof(Action<CharacterBody, float>), set);
        }

        private readonly HashSet<GameObject> _holders = new HashSet<GameObject>();

        public bool HasHolders => _holders.Count > 0;
        public bool Holds(GameObject body) => _holders.Contains(body);

        public CervoFeature(Cfg cfg, ManualLogSource log) : base("Cervo", cfg, log,
            "Item zzz cervo <3 (void verde): toda velocidade de movimento extra vira dano (1:1, +50% de conversão por cópia extra). Desligado: o item existe mas não faz nada.",
            enabledByDefault: true)
        {
        }

        protected override void OnEnable()
        {
            if (SetMoveSpeed == null || SetDamage == null || SetAcceleration == null)
                throw new MissingMemberException("CharacterBody.moveSpeed/damage/acceleration sem setter (versão do jogo mudou?)");
            Track("CharacterBody.onBodyInventoryChangedGlobal", () => CharacterBody.onBodyInventoryChangedGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyStartGlobal", () => CharacterBody.onBodyStartGlobal += OnHolderMaybeChanged);
            Track("CharacterBody.onBodyDestroyGlobal", () => CharacterBody.onBodyDestroyGlobal += OnBodyDestroy);
            BodyHooks.Cervo = this; // RecalculateStats: hook único em BodyHooks (passo 3)
        }

        protected override void OnDisable()
        {
            CharacterBody.onBodyInventoryChangedGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyStartGlobal -= OnHolderMaybeChanged;
            CharacterBody.onBodyDestroyGlobal -= OnBodyDestroy;
            _holders.Clear();
            BodyHooks.Sync();
        }

        public override void ResetRunState()
        {
            _holders.Clear();
            BodyHooks.Sync();
        }

        private void OnHolderMaybeChanged(CharacterBody body)
        {
            try
            {
                var idx = CervoItem.Index;
                if (idx == ItemIndex.None || !body) return;
                var inv = body.inventory;
                if (inv && inv.GetItemCountEffective(idx) > 0)
                {
                    if (_holders.Add(body.gameObject))
                    {
                        BodyHooks.Sync();
                        body.MarkAllStatsDirty(); // o Start do corpo já pode ter calculado os stats sem o hook
                    }
                }
                else if (_holders.Remove(body.gameObject)) BodyHooks.Sync();
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        private void OnBodyDestroy(CharacterBody body)
        {
            if (_holders.Count > 0 && body && _holders.Remove(body.gameObject)) BodyHooks.Sync();
        }

        // Passo 3 do BodyHooks (depois do cálculo do jogo, do Coração Pesado e do Bastião).
        internal void ApplyStats(CharacterBody self)
        {
            try
            {
                var idx = CervoItem.Index;
                if (idx == ItemIndex.None) return;
                var inv = self.inventory;
                if (!inv) return;
                var stacks = inv.GetItemCountEffective(idx);
                if (stacks <= 0) return;

                var neutral = (self.baseMoveSpeed + self.levelMoveSpeed * (self.level - 1f))
                    * (self.isSprinting ? self.sprintingSpeedMultiplier : 1f);
                var oldSpeed = self.moveSpeed;
                if (!SpeedToDamage.Convert(oldSpeed, neutral, self.damage, stacks, out var speed, out var damage)) return;
                SetMoveSpeed(self, speed);
                SetDamage(self, damage);
                SetAcceleration(self, self.acceleration * (speed / oldSpeed)); // o jogo deriva a aceleração da velocidade
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
    }
}
