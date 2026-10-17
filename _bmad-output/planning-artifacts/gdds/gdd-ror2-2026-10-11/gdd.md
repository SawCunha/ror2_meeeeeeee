---
title: 'ror2 - Game Design Document'
game_type: 'roguelike (mod para Risk of Rain 2)'
platforms: ['PC Windows (execução)', 'Linux (desenvolvimento)']
created: '2026-10-11'
updated: '2026-10-11'
source_prd: 'prds/prd-ror2-2026-10-11/prd.md'
---

# ror2 - Game Design Document

**Autor:** Sawcunha
**Tipo:** Mod C# (BepInEx/R2API) para Risk of Rain 2 (Unity), co-op de 3 jogadores
**Plataforma:** RoR2 versão mais recente (patch 1.21, 8/out/2026), execução em Windows via r2modman

> Escopo: este GDD descreve o **comportamento do mod** (regras, números, configuração). O jogo base fornece personagens, itens, níveis, arte, áudio e UI. Não há conteúdo novo. Seções do template sem sentido para um mod foram omitidas.

---

## Resumo Executivo

### Conceito

Em partidas longas com muitos itens, o FPS cai (2–3 FPS relatados) e a RAM dispara porque efeitos por tick escalam por stack. O mod (1) limita o cálculo desses efeitos, (2) multiplica os itens coletados e (3) compensa com dificuldade maior, para que partidas longas em grupo voltem a ser jogáveis.

### Público

Uso pessoal: o autor e 2 amigos (3 jogadores), todos com o mod instalado na mesma versão. Fora do público: Thunderstore e partidas públicas.

---

## Objetivos e Contexto

- Tornar jogável o late game com 50+ stacks de Bustling Fungus e ~200 de Bundle of Fireworks (cenário de referência: 3 jogadores, seed fixo).
- Manter o efeito de cada item reconhecível apesar do limite.
- Medir ganho real (frame time e RAM) em 3 condições: sem mod, mod sem multiplicação, mod com multiplicação.
- Contexto: primeiro mod do autor, experiente em Java, sem C#.

---

## Mecânicas do Mod

Cada alteração é ligável/desligável por configuração. Valores da configuração do **host** valem para todos.

### M1. Cálculo limitado de itens (FR-2/FR-3)
- Itens iniciais: Bustling Fungus e Bundle of Fireworks.
- Deixam de crescer sem limite por stack, mantendo efeito reconhecível.
- [NOTE FOR DESIGNER] Limite e fórmula não definidos (manter quantidade ou agregar). Depende da causa real do custo, ainda não verificada.

### M2. Multiplicação de itens (FR-6/7/8)
- Multiplicador inteiro positivo, sem teto, padrão x2; 1 desliga.
- Elegíveis: itens brancos, verdes, vermelhos e boss de baú, via drop ou impressora.
- Lunares e void: configuráveis, padrão desligado.
- Não elegíveis: itens temporários, scrap e equipamento.
- Jogador morto ou desconectado não recebe.
- Itens sem cálculo limitado têm teto de stacks por item e por jogador, configurável. Padrão: sem teto (desligado). Só M1 protege o desempenho por padrão.

### M3. Dificuldade (FR-11, MVP mínimo)
- Fator configurável, decidido no host.
- Um único fator escala quatro eixos: vida dos inimigos, dano dos inimigos, quantidade de inimigos (orçamento de spawn) e ritmo (velocidade da escalada do coeficiente de dificuldade).
- [NOTE FOR DESIGNER] Valores por eixo e curva (linear ou outra) a calibrar em playtest. Eixos configuráveis individualmente é candidato a refinamento.

### M4. Corte de partículas (FR-12/13)
- Desabilita ou encurta efeitos visuais de itens por lista configurável, sem alterar dano, cura ou colisão.
- [NOTE FOR DESIGNER] Lista de partículas a cortar em aberto.

### M5. Compartilhamento (FR-9/10, v2)
- Todo pickup elegível é replicado integralmente aos demais jogadores, incluindo cópias da multiplicação.
- Não recursivo. Ordem: pickup, multiplicação, compartilhamento.

---

## Especificações Técnicas

- Framework: BepInEx + RoR2BepInExPack 1.45.0, R2API (Items/Language/Networking), HookGenPatcher/MMHOOK, Risk of Options opcional.
- Multiplayer: host autoritativo, sem tráfego de rede por tick, sem dessincronização.
- Performance: log verboso desligado, sem alocação por tick.
- Metas numéricas de FPS e RAM: [ASSUMPTION] definidas somente após a linha de base (FR-1).
- Detalhes de implementação ficam para a arquitetura.

---

## Épicos de Desenvolvimento

| # | Épico | Escopo |
|---|-------|--------|
| 1 | Medição e linha de base | FR-1 |
| 2 | Cálculo limitado | FR-2/FR-3, M1 |
| 3 | Multiplicação de itens | FR-6/7/8, M2 |
| 4 | Dificuldade mínima | FR-11, M3 |
| 5 | Corte de partículas | FR-12/13, M4 |
| 6 | Compartilhamento (v2) | FR-9/10, M5 |

Detalhamento em `epics.md`.

---

## Métricas de Sucesso

- Frame time e RAM nas 3 condições do cenário de referência (FR-1).
- Partida de 3 jogadores sem dessincronização.
- Metas numéricas: após a linha de base.

---

## Fora de Escopo

- Suporte a jogadores sem o mod, mod só-host.
- Modo exponencial de multiplicação.
- Cobrir todos os itens do jogo.
- Conteúdo novo.
- Compatibilidade com outros mods de drops ou patches futuros.
- Publicação no Thunderstore.
- v2: compartilhamento (M5).

---

## Suposições e Dependências

- [ASSUMPTION] Multiplicador padrão x2 e regras de elegibilidade (reversíveis).
- [ASSUMPTION] Configuração do host vale para todos.
- Não verificado: `Inventory.GiveItem` ser server-only, causa real dos gargalos, build cross-OS Linux→Windows, divergência netstandard2.0 vs 2.1.
