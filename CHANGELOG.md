# Changelog

## [Unreleased]

最初の公開に向けた版。

### Added

#### フォーカスとウィンドウ

- `UIFocusManager`：ウィンドウのスタック、開く前のフォーカスの預かりと返却、フォーカスが消えたときの復帰。インスタンスとして生成でき、`Instance` はロケータとして残す
- `WindowBase`：CanvasGroup で表示と入力の受付を切り替える基底クラス。`Open` / `Close` / `Toggle` で開閉し、閉じ始めた時点で入力を切る。既定要素と閉じるボタンはコードでも指定できる。`UIFocusManager.Instance` を使い、VContainer で注入されればそちらを優先する
- `IFocusSource`：ウィンドウと基底画面に共通の、既定要素を返す窓口。`SwitchBase` / `SetBaseFocusSource` で基底画面を設定する
- 常時表示 UI へのフォーカスの貸し借り（`EnterPersistentUIFocus` / `ExitPersistentUIFocus` / `TogglePersistentUIFocus`）
- Cancel の部品（`TryCloseTopWindow` / `TryPopScope`）と、入力でウィンドウを開閉する `RegisterToggleAction`
- 拡張点：`IInputScopeGate`（ゲームプレイ入力の停止）、`ISubmitHoldProbe`（決定の押下）、`IWindowTransition`（開閉の演出）
- 観測用のプロパティ `WindowCount` / `IsFocusOnDefaultElement`
- フォーカスの予約は、ウィンドウの予約と常時表示 UI の予約を別の枠で持つ。既定要素の取得が例外を投げても、警告を出してフォーカスを動かさずに進む（ウィンドウを閉じる途中でも閉じきる）。押せないだけの要素にもフォーカスを戻す。常時表示 UI から戻る先が消えていれば既定要素へ戻す

#### ナビゲーション

- `NavigationController`：今のスコープ（最前面のウィンドウ、なければ基底画面）の中だけから移動先を決める。EventSystem の move を止めて自前で解決するので、ウィンドウの背面の UI へ飛ばない
- `NavigationScope`：スコープの根、端の挙動（`EdgePolicy.Stop` / `Exit` / `WrapRow`）、候補の除外、要素ごとの移動先の明示（`Link`）、操作できない要素と Scrollbar を候補に含めるか（`IncludeNonInteractable`、`IncludeScrollbars`）、壁（`Block`）、入力の丸め方（`WithInputMode`）とリピートの有無（`WithoutRepeat`、`WithRepeat`）、要素への方向入力の受け渡し（`PassMoveToElement`）、選択に合わせたスクロール（中央寄せも可）、解決器の差し替えをコードで宣言する
- `INavigationScopeSource`：画面が自分のスコープを宣言する。`WindowBase` は既定でウィンドウの transform を根にし、`CreateNavigationScope` のオーバーライドで変えられる。スコープは最初に今の画面になったときに一度だけ作るので、登録の手順が要らない
- `NavigationController.Register`：外から画面にスコープを結び付ける（画面の宣言より優先する）
- `SpatialResolver` と `DirectionalResolver`：入力の時点の RectTransform の位置から移動先を決める
- `DirectionRepeater`：移動入力を 4 方向へ丸め、押し続けたときにリピートする
- `SetMoveBlocker`：修飾ボタン（LB など）を押している間の方向入力を移動として扱わない
- `SelectionChanged`：選択の変化を、方向入力、ホバー、それ以外に分けて通知する
- `ScrollIntoView`：選択した要素が見える位置まで ScrollRect を動かす
- `IDiagonalCursor` / `IDiagonalNavigationResolver`：`EightWay` の斜めの入力で、`GridCursor` は斜め先のマスへ直接動く（斜め先が止まれなければ動かない）
- ホバー選択（`EnableHoverSelection`）：ポインタが動いたときだけ選び、スコープの外は選ばない
- 仮想カーソル：`GridCursor` / `ListCursor` / `CursorResolver`。フォーカスはライブラリ内部のアンカーが受ける。止まれない位置を飛び越えるか手前で止まるかを選べる。`CursorResolver` の `returnsToAnchor` で、何が選ばれていても方向入力をカーソルへ向けられる
- 拡張点：`INavigationInput`（方向入力）、`IPointerPositionSource`（ポインタの位置）、`INavigationResolver`（移動先の決め方）

#### 任意のアセンブリ

- `Void2610.Arinn.InputSystem`：`InputSystemNavigationInput` / `InputSystemPointerPosition` / `InputSystemSubmitHoldProbe` と、まとめて設定する `UseInputSystem`
- `Void2610.Arinn.LitMotion`：`FadeWindowTransition`
- `Void2610.Arinn.LiminalPalette`：`Arinn/*` の観測コマンド（Editor と Development Build のみ）
- VContainer への登録（`RegisterArinn` / `RegisterArinnNavigation`）

#### ドキュメントとサンプル

- チュートリアル（`Documentation~/tutorial.md`）
- サンプル `Minimal`：基底画面、ウィンドウ 2 枚（スクロール一覧と仮想カーソルのグリッド）、確認ダイアログをコードだけで組み立てる。LifetimeScope には arinn と Presenter だけを登録し、Presenter が `FindFirstObjectByType` で View を取得して画面同士を繋ぐ
- EditMode テストと、Input System 連携の PlayMode テスト
- コンパイルの確認（`.ci/compile/build.sh` と GitHub Actions）：Unity を起動せず、非公式のリファレンスアセンブリに対して dotnet でコンパイルする。サンプルとテストも対象にする
