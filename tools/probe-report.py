#!/usr/bin/env python3
"""Resume o ror2meeeee-probe.csv por condição (nome da fase sem o sufixo -N).

Uso: python3 tools/probe-report.py [caminho.csv]
Rotule as janelas com `ror2meeeee_probe_label <condicao>` no console do jogo.
"""
import csv, os, re, statistics, sys
from collections import OrderedDict

DEFAULT = os.path.expanduser(
    "~/.config/r2modmanPlus-local/RiskOfRain2/profiles/Default/BepInEx/ror2meeeee-probe.csv")
path = sys.argv[1] if len(sys.argv) > 1 else DEFAULT
rows = list(csv.DictReader(open(path)))
groups = OrderedDict()
for r in rows:
    groups.setdefault(re.sub(r"-\d+$", "", r["phase"]), []).append(r)

# 0.2.0+: ram_unity_alloc_mb (alocada); CSVs antigos: ram_unity_mb (reservada).
unity = ("ram_unity_alloc_mb", "RAM Unity aloc") if rows and "ram_unity_alloc_mb" in rows[0] else ("ram_unity_mb", "RAM Unity MB")
cols = [("mean_ms", "média ms"), ("p95_ms", "p95 ms"), ("ram_managed_mb", "RAM ger. MB"),
        unity, ("projectiles", "projéteis"),
        ("damage_total", "dano total"), ("heal_total", "cura total")]
# 0.8.6+: eventos de cura, ajudantes e itens dos jogadores (só aparecem em CSVs novos).
cols += [(c, h) for c, h in (("heal_events", "ev. cura"), ("minions", "ajudantes"), ("player_items", "itens"))
         if rows and c in rows[0]]
print(f"{'condição':<16}{'n':>3}" + "".join(f"{h:>14}" for _, h in cols))
for name, rs in groups.items():
    vals = [statistics.median(float(r[c]) for r in rs) for c, _ in cols]
    print(f"{name:<16}{len(rs):>3}" + "".join(f"{v:>14.1f}" for v in vals))
print("\n(mediana por condição; janelas truncadas:",
      sum(r["truncated"] == "true" for r in rows), ")")
