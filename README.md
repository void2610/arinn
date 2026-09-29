# arinn

Unity uGUI の UI フォーカスとナビゲーションを管理するライブラリ。ウィンドウを開くときにフォーカスを預かり、閉じたら返す。フォーカスが消えたら自動で戻す。

名前は古ノルド語の *arinn*（炉床）に由来する。ラテン語の *focus* も元は「炉床」を意味する。

## 機能

- **ウィンドウスタック**: `UIFocusManager.ShowWindow` / `HideWindow` で、表示・フォーカス・入力受付をまとめて切り替える。二重に開いても積み直さない。閉じ始めた時点で入力を切る
- **フォーカスの貸し借り**: 開く前のフォーカスを預かり、閉じたら返す。最後のウィンドウを閉じたら基底画面（`SwitchBase` / `SetBaseFocusSource`）の既定要素へ戻す
- **フォーカス監視**: フォーカスが消えたら、直前の要素がまだ操作できればすぐ戻す。消えていれば 0.5 秒待ってから、最前面のウィンドウ（なければ基底画面）の既定要素へ戻す
- **常時表示 UI**: スタックに積まずにフォーカスだけを借りる（`EnterPersistentUIFocus` / `ExitPersistentUIFocus`）
- **Cancel**: `TryPopScope` がウィンドウを閉じ、なければ常時表示 UI から戻る
- **遷移**: 見た目の出し入れは `IWindowTransition` で差し替える。既定は即時、LitMotion があれば `FadeWindowTransition` が使える

## インストール

`Packages/manifest.json` に追加する:

```json
"com.void2610.arinn": "https://github.com/void2610/arinn.git"
```

UniTask・R3・VContainer が必要（git パッケージのため `package.json` の依存には書いていない）。

- `Void2610.Arinn.LitMotion`（`FadeWindowTransition`）は LitMotion があるときだけ有効になる
- `Void2610.Arinn.InputSystem`（`InputSystemSubmitHoldProbe`）は Input System があるときだけ有効になる

## 使い方

```csharp
// LifetimeScope（シーンを跨ぐなら親のスコープ）
builder.RegisterArinn(manager => manager.DefaultTransition = new FadeWindowTransition(0.2f));

// 画面遷移
manager.SwitchBase(battleView);   // IFocusSource

// ウィンドウ
manager.ShowWindow(pauseView);    // WindowBase の派生
manager.HideWindow(pauseView);

// Cancel 入力
if (manager.TryPopScope()) return;
```

DI を通さない View からは `UIFocusManager.Instance` で引ける。

## テスト

`Tests/Editor` に EditMode テストを同梱している。利用側の `Packages/manifest.json` の `testables` に入れると実行される:

```json
"testables": ["com.void2610.arinn"]
```
