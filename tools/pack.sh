#!/usr/bin/env bash
# Gera dist/Sawcunha-Ror2Meeeee-<versão>.zip para importar no r2modman.
# O icon.png do pacote (ícone no r2modman/Thunderstore) sai de src/Ror2Meeeee/Assets/mod_icon.png,
# o mesmo PNG embutido no DLL para a tela de mods do jogo: trocar a arte é trocar só esse arquivo.
set -euo pipefail
cd "$(dirname "$0")/.."
ver=$(grep -oP 'string Version = "\K[^"]+' src/Ror2Meeeee.Core/ModInfo.cs)
grep -q "\"version_number\": \"$ver\"" package/manifest.json || { echo "package/manifest.json não está em $ver" >&2; exit 1; }
dotnet build src/Ror2Meeeee -c Release
out=src/Ror2Meeeee/bin/Release/netstandard2.1
stage=$(mktemp -d); trap 'rm -rf "$stage"' EXIT
cp package/manifest.json package/README.md package/CHANGELOG.md "$out/Ror2Meeeee.dll" "$out/Ror2Meeeee.Core.dll" "$stage/"
cp src/Ror2Meeeee/Assets/mod_icon.png "$stage/icon.png"
mkdir -p dist; zip -j -q "dist/Sawcunha-Ror2Meeeee-$ver.zip" "$stage"/*
echo "dist/Sawcunha-Ror2Meeeee-$ver.zip"
