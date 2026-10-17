---
baseline_commit: 1afd157
---

# Story 3.5: Condição 3 da linha de base

Status: ready-for-dev

## Story

Como autor do mod, quero medir o cenário de referência com a multiplicação ligada, para confirmar que o mod, com multiplicação, está melhor que o jogo sem mod (FR-1, NFR-1).

## Acceptance Criteria

(Fonte: `epics.md`, Story 3.5: condição 3 medida e guardada em `docs/` ao lado das condições 1 e 2; metas de FPS/RAM registradas; partida de 3 jogadores sem dessincronização.)

## Tasks / Subtasks

- [ ] Medir condições 1, 2 e 3 no cenário de `docs/linha-de-base.md` (VSync/limite de FPS desligados)
  - `ror2meeeee_probe_label c1|c2|c3`, 3 janelas cada; `python3 tools/probe-report.py`
- [ ] Guardar os CSVs em `docs/baseline/` e preencher a tabela de `docs/linha-de-base.md`
- [ ] Partida de 3 jogadores sem dessincronização; `ror2meeeee_status` limpo
- [ ] Registrar metas de FPS/RAM ou ajustar `LimitedCalc`/`StackCap` se c3 > c1

## Dev Notes

- Instrumentação pronta: janelas automáticas rotuláveis e `tools/probe-report.py` (mediana por condição).
- **Depende do autor jogando.**
