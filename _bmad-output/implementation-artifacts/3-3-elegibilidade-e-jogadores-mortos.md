---
baseline_commit: 1afd157
---

# Story 3.3: Elegibilidade e jogadores mortos

Status: in-progress

## Story

Como jogador do grupo, quero que só os pickups certos sejam multiplicados e que mortos não recebam, para a regra ser previsível e justa (FR-8).

## Acceptance Criteria

1. `IncludeLunar`/`IncludeVoid` (padrão `false`) controlam lunar e void.
2. Itens comuns de baú, drop e impressora multiplicam; temporário, scrap e equipamento não.
3. Morto ou desconectado não recebe; sem acúmulo retroativo.
4. Concessões que não são pickup não multiplicam; comportamento por origem em `docs/`.
5. Tabela real conferida com o relatório 1.3.

## Tasks / Subtasks

- [x] Classificação do jogo -> `ItemKind` em `MultiplierFeature.Classify` (AC: #1, #2, #5)
- [x] Vivo/conectado via `HealthComponent.alive` e `PlayerCharacterMasterController.isConnected` (AC: #3)
- [x] Origens documentadas em `docs/teste-epic3.md` e `docs/relatorio-ror2-dll.md` (AC: #4)
- [ ] Validação em jogo (AC: #1 a #3): void multiplicado com `IncludeVoid=true` (BearVoid, CritGlassesVoid, `+299`) confirmado; faltam lunar, scrap, temporário e jogador morto/reconectado

## Dev Notes

- Tiers de 1.5.0 além dos clássicos (`FoodTier`, `Cursed`, `Page`, `Hex`, `NoTier`) caem em `Other` e não multiplicam. Divergência com o épico, que não os previa.
- Um jogador morto não consegue coletar pickup; a checagem existe para a v2 (compartilhamento) e para casos como `Command` por outro jogador.
- Linhas "verificar" da tabela de origens (Command, scrapper) dependem do jogo.

## Dev Agent Record

### File List

- `src/Ror2Meeeee/Features/Multiplier/MultiplierFeature.cs`, `docs/teste-epic3.md`

### Change Log

- 2026-10-11: elegibilidade.
