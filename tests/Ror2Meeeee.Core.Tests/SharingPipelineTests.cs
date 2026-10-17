using Ror2Meeeee.Core.Eligibility;
using Ror2Meeeee.Core.Pipeline;
using Xunit;

namespace Ror2Meeeee.Core.Tests
{
    // Testes da regra pura de compartilhamento (SharingPipeline + EligibilityTable.IsShareable).
    public class SharingPipelineTests
    {
        // Pickup de 1 cópia de um jogador vivo e conectado, sem isenção de teto.
        private static PickupContext Pickup(ItemKind kind = ItemKind.White, int count = 1)
            => new PickupContext(kind, count, 0, true, true, false);
        // Regras do Multiplicador com lunar e void desligados.
        private static MultiplierRules Rules(int mult = 2, int cap = 0) => new MultiplierRules(mult, false, false, cap);
        // Destinatário vivo e conectado com o stack informado.
        private static Recipient Live(int stack = 0) => new Recipient(stack, true, true);

        // Multiplicador x2: cada um dos outros recebe 2 cópias.
        [Fact]
        public void Mult2_TwoOthers_EachGetsTwo() =>
            Assert.Equal(new[] { 2, 2 }, SharingPipeline.Resolve(Pickup(), Rules(2), true, false, new[] { Live(), Live() }));

        // Compartilhamento desligado: ninguém recebe.
        [Fact]
        public void Disabled_ReturnsZeros() =>
            Assert.Equal(new[] { 0, 0 }, SharingPipeline.Resolve(Pickup(), Rules(2), false, false, new[] { Live(), Live() }));

        // Concessão vinda do próprio compartilhamento não é compartilhada de novo.
        [Fact]
        public void FromSharing_IsNotSharedAgain() =>
            Assert.Equal(new[] { 0 }, SharingPipeline.Resolve(Pickup(), Rules(2), true, true, new[] { Live() }));

        // Morto ou desconectado não recebe; só o vivo e conectado.
        [Fact]
        public void DeadOrDisconnected_GetNothing() =>
            Assert.Equal(new[] { 0, 0, 2 }, SharingPipeline.Resolve(Pickup(), Rules(2), true, false,
                new[] { new Recipient(0, false, true), new Recipient(0, true, false), Live() }));

        // Equipamento e itens únicos do mod nunca são compartilhados.
        [Theory]
        [InlineData(ItemKind.Equipment)] [InlineData(ItemKind.Other)]
        public void UnshareableKinds_GetNothing(ItemKind kind) =>
            Assert.Equal(new[] { 0 }, SharingPipeline.Resolve(Pickup(kind), Rules(2), true, false, new[] { Live() }));

        // Tipos que não multiplicam (aqui lunar/void desligados nas regras) ainda são compartilhados, em 1 cópia.
        [Theory]
        [InlineData(ItemKind.Temporary)] [InlineData(ItemKind.Scrap)] [InlineData(ItemKind.Special)]
        [InlineData(ItemKind.Lunar)] [InlineData(ItemKind.Void)]
        public void NonMultipliedKinds_AreSharedOnce(ItemKind kind) =>
            Assert.Equal(new[] { 1 }, SharingPipeline.Resolve(Pickup(kind), Rules(2), true, false, new[] { Live() }));

        // O teto de stacks vale para o stack de cada destinatário.
        [Fact]
        public void StackCap_AppliesPerRecipient() =>
            Assert.Equal(new[] { 5, 1 }, SharingPipeline.Resolve(Pickup(), Rules(5, cap: 5), true, false,
                new[] { Live(0), Live(4) }));

        // Destinatário já no teto não recebe nem a cópia original.
        [Fact]
        public void StackCap_RecipientAtCap_GetsNothing() =>
            Assert.Equal(new[] { 0, 0 }, SharingPipeline.Resolve(Pickup(), Rules(5, cap: 5), true, false,
                new[] { Live(5), Live(9) }));

        // Multiplicação enorme satura em int.MaxValue em vez de estourar.
        [Fact]
        public void Saturates_WithoutOverflow() =>
            Assert.Equal(new[] { int.MaxValue }, SharingPipeline.Resolve(Pickup(count: 5), Rules(int.MaxValue), true, false, new[] { Live() }));

        // Sem outros jogadores: resultado vazio.
        [Fact]
        public void NoOthers_ReturnsEmpty() =>
            Assert.Empty(SharingPipeline.Resolve(Pickup(), Rules(2), true, false, new Recipient[0]));
    }
}
