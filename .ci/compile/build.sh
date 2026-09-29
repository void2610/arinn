#!/usr/bin/env bash
# 依存を取ってきて、arinn の各アセンブリとサンプルとテストがコンパイルできるか、コーディング規約に沿っているかを確かめる
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

# 直すときは --verify-no-changes を外して同じ順に回す（style の修正でメンバーの順序が崩れることがあるので、最後に analyzers をもう一度通す）
for project in Arinn/*/*.csproj; do
  dotnet format analyzers "$project" --severity warn --verify-no-changes
  dotnet format whitespace "$project" --verify-no-changes
  dotnet format style "$project" --severity warn --verify-no-changes
  dotnet format analyzers "$project" --severity warn --verify-no-changes
done
