#!/usr/bin/env bash
# 依存を取ってきて、arinn の各アセンブリとサンプルとテストがコンパイルできるかを確かめる
set -euo pipefail
cd "$(dirname "$0")"
./fetch-deps.sh
for project in \
  Arinn/Void2610.Arinn.InputSystem/Void2610.Arinn.InputSystem.csproj \
  Arinn/Void2610.Arinn.LiminalPalette/Void2610.Arinn.LiminalPalette.csproj \
  Arinn/Void2610.Arinn.Tests/Void2610.Arinn.Tests.csproj \
  Arinn/Void2610.Arinn.Samples.Minimal/Void2610.Arinn.Samples.Minimal.csproj; do
  dotnet build "$project" --nologo -v quiet
done
