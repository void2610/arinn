#!/usr/bin/env bash
# コンパイル確認に使う依存ライブラリのソースを .deps へ取ってくる
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p .deps

fetch() {
  local name="$1" url="$2" ref="$3"
  if [ -d ".deps/$name/.git" ]; then return; fi
  git -c advice.detachedHead=false clone --quiet --depth 1 --branch "$ref" "$url" ".deps/$name"
}

fetch UniTask https://github.com/Cysharp/UniTask.git 2.5.11
fetch R3 https://github.com/Cysharp/R3.git 1.3.1
fetch VContainer https://github.com/hadashiA/VContainer.git 1.19.0
fetch LiminalPalette https://github.com/void2610/liminal-palette.git main

# 非公式のリファレンスアセンブリの UnityEngine.UI は protected を public に書き換えてあり、protected override を誤ってエラーにするため、ソースからビルドする
if [ ! -d .deps/ugui ]; then
  mkdir -p .deps/ugui
  curl -sSL https://download.packages.unity.com/com.unity.ugui/-/com.unity.ugui-3.0.0-exp.1.tgz | tar xz -C .deps/ugui
fi
