---
stepsCompleted: ['step-01-document-discovery', 'step-02-gdd-analysis', 'step-03-epic-coverage-validation', 'step-04-ux-alignment', 'step-05-epic-quality-review', 'step-06-final-assessment']
includedFiles:
  - _bmad-output/planning-artifacts/gdds/gdd-ror2-2026-10-11/gdd.md
  - _bmad-output/game-architecture.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/project-context.md
  - _bmad-output/planning-artifacts/prds/prd-ror2-2026-10-11/prd.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-10-11
**Project:** ror2

## Document Discovery

### GDD
- Whole: `planning-artifacts/gdds/gdd-ror2-2026-10-11/gdd.md` (5,1 KB)
- Sharded: nenhum

### Architecture
- Whole: `_bmad-output/game-architecture.md` (32 KB). Fora de `planning-artifacts/`.
- Sharded: nenhum

### Epics & Stories
- Whole (canônico): `planning-artifacts/epics.md` (6 épicos, 24 stories)
- Rascunho superado: `planning-artifacts/gdds/gdd-ror2-2026-10-11/epics.md` (1,9 KB, resumo de 6 épicos)

### UX Design
- Não existe (mod sem UI própria).

### Complementares
- `prds/prd-ror2-2026-10-11/prd.md` (FR/NFR originais), `addendum.md`, `decision-log.md`
- `_bmad-output/project-context.md`

## GDD Analysis

O GDD é enxuto e referencia FR-1…FR-13 por número; o texto completo dos FRs/NFRs está no PRD (`prd.md`), usado aqui como fonte de verdade.

### Functional Requirements

| FR | Texto (resumo fiel do PRD) | Onde o GDD o cita |
|---|---|---|
| FR-1 | Medir frame time e RAM no cenário de referência em 3 condições (sem mod, mod sem multiplicação, mod com multiplicação) | Épico 1 (tabela de épicos) |
| FR-2 | Custo por tick do Bustling Fungus não cresce sem limite, preservando a cura reconhecível | M1 |
| FR-3 | Custo por interação do Bundle of Fireworks não cresce sem limite, preservando o dano total reconhecível | M1 |
| FR-4 | Cada alteração de item pode ser ligada/desligada individualmente | Implícito ("ligável/desligável") |
| FR-5 | Novos itens entram na lista de cálculo limitado sem reescrever a lógica central | **Não citado** |
| FR-6 | Multiplicador inteiro positivo configurável, sem teto; 1 desliga (padrão x2) | M2 |
| FR-7 | Itens sem cálculo limitado não multiplicados acima de um teto configurável de stacks | M2 |
| FR-8 | Multiplicação só em pickups elegíveis; lunar e void configuráveis (padrão não) | M2 |
| FR-9 [v2] | Pickup elegível replicado aos demais jogadores na quantidade integral | M5 |
| FR-10 [v2] | Compartilhamento desligável; item compartilhado não é compartilhado de novo | M5 |
| FR-11 | Dificuldade aumenta por fator configurável decidido no host (4 eixos) | M3 |
| FR-12 | Desabilitar/encurtar partículas de lista configurável de efeitos de itens | M4 |
| FR-13 | Corte de partículas não altera dano, cura nem colisão | M4 |

Total FRs: 13

### Non-Functional Requirements

| NFR | Texto | Onde o GDD o cita |
|---|---|---|
| NFR-1 | Desempenho: FPS e RAM com mod (inclui multiplicação) melhores que a linha de base; metas após a linha de base | Especificações Técnicas |
| NFR-2 | Multiplayer: todos com o mod, mesma versão, sem dessincronização, config do host vale, sem tráfego por tick | Especificações Técnicas |
| NFR-3 | Observabilidade: log verboso desligado; medição por configuração | "log verboso desligado" |
| NFR-4 | Evitar alocação por tick nos caminhos alterados | "sem alocação por tick" |
| NFR-5 | Alvo é a versão mais recente do jogo | Cabeçalho ("patch 1.21") |
| NFR-6 | Dev em Linux, execução em Windows | Cabeçalho (plataformas) |

Total NFRs: 6

### Additional Requirements

- Cada alteração é ligável/desligável; valores de config do host valem para todos.
- Itens iniciais do cálculo limitado: Bustling Fungus e Bundle of Fireworks.
- Não elegíveis: temporários, scrap, equipamento; jogador morto/desconectado não recebe.
- Ordem pickup → multiplicação → compartilhamento, sem recursão.
- Fora de escopo: jogadores sem o mod, modo exponencial, cobrir todos os itens, conteúdo novo, compatibilidade com outros mods e patches futuros, Thunderstore.
- Não verificado (GDD): `Inventory.GiveItem` server-only, causa real dos gargalos, build cross-OS, netstandard2.0 vs 2.1.

### GDD Completeness Assessment

**Pontos fortes:** mecânicas M1–M5 rastreáveis aos FRs; escopo MVP versus v2 claro; fora de escopo explícito; decision log presente.

**Lacunas e inconsistências:**
1. **FR-5 não aparece no GDD** (extensibilidade da lista); FR-4 só implícito.
2. **Números em aberto:** limite e fórmula de M1, valores por eixo de M3, lista de partículas de M4, metas de FPS/RAM. Todos marcados como dependentes de medição ou playtest (aceitável, tratado em épicos).
3. **Versão do jogo:** GDD diz "patch 1.21 (8/out/2026)"; a arquitetura e o `CLAUDE.md` confirmam 1.5.0 build #1210. O GDD está defasado nesse ponto.
4. **Dependências R2API:** GDD lista `Items/Language/Networking`; a arquitetura conclui que `Items` e `Language` não são necessários.
5. **Escopo de config de partículas:** GDD diz que valores do host valem para todos; a arquitetura mantém `Particles` local (desvio consciente registrado).
6. **GDD sem critérios de aceitação mensuráveis** para "efeito reconhecível" (definido só em 2.3).

## Epic Coverage Validation

### Coverage Matrix

| FR | Requisito | Cobertura em `epics.md` | Status |
|---|---|---|---|
| FR-1 | Medição em 3 condições | Epic 1 (1.4 `Probe`, 1.6 condições 1 e 2); Epic 3 (3.5 condição 3) | ✓ Coberto |
| FR-2 | Limite do Bustling Fungus | Story 2.1 (validação 2.3) | ✓ Coberto |
| FR-3 | Limite do Bundle of Fireworks | Story 2.2 (validação 2.3) | ✓ Coberto |
| FR-4 | Liga/desliga por alteração | Story 1.2 (mecanismo); 2.1, 2.2 (flag por item); `Enabled` em 3.2, 4.2, 5.2, 6.2 | ✓ Coberto |
| FR-5 | Lista extensível | Stories 2.1 (AC de nova entrada sem mudar o núcleo) e 2.2 | ✓ Coberto |
| FR-6 | Multiplicador configurável | Stories 3.1, 3.2 | ✓ Coberto |
| FR-7 | Teto de stacks | Story 3.4 | ✓ Coberto |
| FR-8 | Elegibilidade | Stories 3.1, 3.3 | ✓ Coberto |
| FR-9 [v2] | Replicação integral | Stories 6.1, 6.2 | ✓ Coberto |
| FR-10 [v2] | Desligável, sem recursão | Stories 6.1, 6.2 | ✓ Coberto |
| FR-11 | Fator de dificuldade (4 eixos) | Stories 4.1 a 4.4 | ✓ Coberto |
| FR-12 | Lista de partículas | Stories 5.1, 5.2 | ✓ Coberto |
| FR-13 | Sem alterar gameplay | Story 5.3 | ✓ Coberto |

FRs em `epics.md` que não estão no GDD/PRD: nenhum.

### NFR Coverage (complementar)

| NFR | Cobertura | Observação |
|---|---|---|
| NFR-1 | 2.3, 3.5, 4.4, 5.3, 6.3 | Metas numéricas só após 1.6/3.5 |
| NFR-2 | 3.2, 3.5, 4.2, 6.2, 6.3 | Testado com host e clientes reais |
| NFR-3 | 1.2 (`VerboseLogging`), 1.4 (`Probe`) | |
| NFR-4 | ACs de "sem alocação por tick" em 2.1, 2.2, 4.2, 5.2 | Sem teste automatizado possível; verificado pelo `Probe` |
| NFR-5 | 1.1 (versão e `Warning`) | Cobertura mínima (apenas aviso) |
| NFR-6 | 1.1 (build Linux, carga Windows/Proton) | |

### Missing Requirements

Nenhum FR sem cobertura.

**Observações de qualidade da cobertura:**
- FR-5 está coberto apenas como AC dentro de 2.1; não há teste que o prove com um terceiro item. Risco baixo.
- NFR-4 só é verificável por medição em jogo, não por `dotnet test`.
- NFR-5: a verificação de versão cobre só aviso; a arquitetura a marca como adiada.

### Coverage Statistics

- Total GDD/PRD FRs: 13
- FRs covered in epics: 13
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

Não encontrado (`*ux*.md` e `*ux*/index.md` ausentes em `planning-artifacts/`).

### Alignment Issues

N/A: não há documento de UX para alinhar.

### Warnings

Interface própria é **parcialmente implícita**, mas de baixo risco:

1. **"Tela de configuração" (PRD FR-8, FR-12):** o PRD fala em escolher lunar/void "na tela de configuração". A arquitetura entrega só o arquivo `ConfigFile` do BepInEx; o menu in-game via Risk of Options está **adiado** e não tem story. Se a tela in-game for expectativa real, falta uma story; se o `.cfg` basta, o texto do PRD deve ser ajustado.
2. **Overlay de medição (`General.OverlayEnabled`):** a arquitetura prevê um overlay opcional, mas nenhuma story o implementa (a 1.4 só gera CSV). Item opcional, sem FR.
3. **Saída do `ror2meeeee_status`:** comando de console, coberto na Story 1.5.

Nenhum desses itens bloqueia a implementação.

## Epic Quality Review

Revisão adversarial dos 6 épicos e 24 stories de `epics.md` contra as boas práticas do `gds-create-epics-and-stories`.

### Checklist por épico

| Épico | Valor ao usuário | Independência | Sem dependência futura | Tamanho | ACs | Rastreio FR |
|---|---|---|---|---|---|---|
| 1 Ambiente, medição e linha de base | 🟡 borderline (técnico) | ✓ | ✓ | 🟡 1.1 grande | ✓ | ✓ |
| 2 Cálculo limitado | ✓ | ✓ (usa 1) | ✓ | ✓ | 🟠 sem limiar mensurável | ✓ |
| 3 Multiplicação | ✓ | ✓ (usa 1, 2) | ✓ | ✓ | 🟠 origem do pickup | ✓ |
| 4 Dificuldade | ✓ | ✓ (calibra com 2 e 3, passado) | ✓ | 🟡 4.2 grande | 🟡 subjetivo em 4.4 | ✓ |
| 5 Partículas | ✓ | ✓ (usa 1) | ✓ | ✓ | 🟠 hook e contador | ✓ |
| 6 Compartilhamento (v2) | ✓ | ✓ (usa 3) | ✓ | ✓ | ✓ | ✓ |

Starter template: ✓ (Story 1.1). Greenfield: setup, ambiente e build cedo, no Épico 1. Estruturas de dados criadas quando necessárias: ✓.

### 🔴 Critical Violations

Nenhuma. Não há dependência futura nem épico-sem-valor que impeça a implementação.

### 🟠 Major Issues

1. **Stories 2.1 e 2.2: ACs sem limiar mensurável.** "O trabalho por tick deixa de crescer com os stacks" e "o custo por interação deixa de crescer sem limite" não dizem quanto é aceitável. A 2.3 mede, mas não existe critério de aprovação.
   *Remediação:* acrescentar a 2.1/2.2 um limiar relativo, por exemplo "p95 do hook com N stacks no limite ≤ X% do p95 com 1 stack", fixado após a Story 1.6.
2. **Story 3.3: origem do pickup não é distinguível no ponto de hook.** A arquitetura nota que `GiveItemPermanent` captura toda concessão permanente (itens iniciais, conversões), não só pickups. Os ACs listam "baú, drop e impressora" como elegíveis, mas não tratam concessões não-pickup (itens iniciais, Artifact of Command, trocas de void/lunar, scrapper).
   *Remediação:* acrescentar um AC "concessões que não são pickup (itens iniciais, conversões) não são multiplicadas" e que a 1.3 liste as origens reais.
3. **Contingência A8 sem story.** Se o relatório 1.3 mostrar que o efeito do Fungus/Fireworks roda no cliente, a lógica "só no host" das stories 2.1/2.2 não reduz o custo no cliente, e a ADR-1 prevê `INetMessage` como contingência. Não há story condicional para isso.
   *Remediação:* acrescentar à 1.3 um AC de decisão (host-only suficiente? sim/não) e, se não, uma story de sincronização no Épico 2.
4. **Story 5.1/5.2: contador de partículas e ponto de hook não localizados.** O `Probe` da 1.4 conta projéteis e procs, não partículas; a 5.2 assume um "hook de criação do efeito" que nenhuma story investiga (a 1.3 cobre só itens e pickup).
   *Remediação:* na 5.1, incluir (a) a extensão do `Probe` com contador de partículas ativas e (b) a localização, no `RoR2.dll`, de onde os efeitos de item são instanciados (registrar em `docs/`).

### 🟡 Minor Concerns

1. **Epic 1 é técnico por natureza** (infraestrutura e medição). Aceitável para um mod pessoal em que o usuário é o autor, e justificado: nenhuma otimização é permitida antes da linha de base.
2. **Story 1.1 é grande** (SDK, solução, build, carga no jogo, git, `.gitignore`). Risco de estourar uma sessão; dividir em "build" e "carga no jogo" se necessário.
3. **Story 4.2 acumula investigação e implementação** (descompilar + hooks de stats). Considerar separar a investigação em story própria.
4. **Story 3.4:** o AC fala em "por item e por jogador", mas só existe uma chave `StackCap`; não está claro se o teto é por item, por jogador ou combinado.
5. **Story 4.1** cria ganhos configuráveis por eixo, enquanto o GDD marca "eixos configuráveis individualmente" como candidato a refinamento (MVP: fator único). Escopo ligeiramente acima do GDD.
6. **Story 1.2** usa uma "feature de teste" cujo ciclo de vida não é definido (remover ou manter como `Debug`).
7. **Caso de borda do project-context sem AC:** troca de host/queda do host descarta o estado por run; nenhuma story o verifica.
8. **Overlay opcional** da arquitetura não tem story (sem FR, sem risco).
9. **Story 4.4 e 5.3** dependem de avaliação humana (impressão do grupo, "ruído esperado"); aceitável, mas sem critério numérico.
10. **Story 5.1** admite adiar o Épico 5 se o ganho for desprezível; positivo, porém falta registrar o gatilho numérico.

### Pontos fortes

- Todas as stories têm Given/When/Then e a maioria cita FR/NFR.
- O pipeline puro no `Core` (3.1, 6.1) torna a regra de negócio testável fora do jogo.
- A medição vem antes de qualquer otimização, em linha com o `project-context.md`.
- Dependências sempre apontam para trás (nenhuma story depende de uma futura).

## Summary and Recommendations

### Overall Readiness Status

**NEEDS WORK (leve).** Nenhum problema crítico e 100% dos FRs cobertos. A implementação do **Épico 1 pode começar agora**, porque as correções abaixo não o afetam. Os 4 problemas maiores tocam os Épicos 2, 3 e 5 e devem ser corrigidos em `epics.md` antes de detalhar essas stories.

### Critical Issues Requiring Immediate Action

Nenhum bloqueio crítico. Problemas maiores (corrigir antes dos Épicos 2, 3 e 5):

1. **Stories 2.1/2.2:** ACs sem limiar mensurável de desempenho.
2. **Story 3.3:** concessões que não são pickup (itens iniciais, conversões) não têm tratamento; o ponto de hook captura todas.
3. **Contingência A8:** sem decisão nem story caso o efeito de M1 rode no cliente.
4. **Stories 5.1/5.2:** sem contador de partículas no `Probe` e sem investigação do ponto de hook dos efeitos.

### Recommended Next Steps

1. Editar `epics.md` para os 4 itens maiores (ACs adicionais nas stories 1.3, 2.1, 2.2, 3.3 e 5.1); estimativa: poucos minutos.
2. Instalar o .NET SDK 8+ e rodar `git init`; são pré-requisitos da Story 1.1.
3. Alinhar a documentação: atualizar o GDD ("patch 1.21" → 1.5.0 #1210; dependências R2API) e decidir se o PRD "tela de configuração" vira Risk of Options ou `.cfg`.
4. Rodar `gds-sprint-planning` e depois `gds-create-story` para a Story 1.1.
5. Opcional: dividir a Story 1.1 em "build" e "carga no jogo", e a 4.2 em investigação e implementação.

### Final Note

Esta avaliação identificou **23 observações em 4 categorias**: 6 lacunas/inconsistências do GDD, 3 avisos de UX, 4 problemas maiores e 10 menores de qualidade de épicos (mais 3 observações de cobertura). Corrija os maiores antes de detalhar os Épicos 2, 3 e 5, ou prossiga como está e trate-os ao criar cada story.

**Avaliador:** gds-check-implementation-readiness (Claude) · **Data:** 2026-10-11

## Remediação aplicada (2026-10-11)

Os 4 problemas maiores foram corrigidos em `epics.md`:

1. Stories 2.1 e 2.2: AC com limiar relativo de p95 (X% definido após a Story 1.6).
2. Story 3.3: AC para concessões não-pickup; Story 1.3 passa a listar as origens de concessão.
3. Story 1.3: AC de decisão A8 (host-only suficiente?); nova **Story 2.4 (condicional)** com `INetMessage`.
4. Story 5.1: extensão do `Probe` com contador de partículas e localização do hook dos efeitos.

Total: 25 stories. Os 10 itens menores e as lacunas do GDD seguem em aberto.
