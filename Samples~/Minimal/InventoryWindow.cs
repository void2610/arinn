using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn.Samples
{
    /// <summary>
    /// 2 枚目のウィンドウ。マスは Selectable ではなく Image で、仮想カーソル（<see cref="GridCursor"/>）で動く。
    /// 鍵のかかったマスは飛ばし、左右は回り込み、下端から閉じるボタンへ抜ける。
    /// </summary>
    public sealed class InventoryWindow : WindowBase
    {
        private const int COLUMNS = 5;
        private const int ROWS = 3;
        private const float CELL_SIZE = 100f;
        private const float CELL_SPACING = 16f;

        private static readonly Color CellColor = new(0.25f, 0.27f, 0.34f);
        private static readonly Color LockedColor = new(0.1f, 0.1f, 0.12f);
        private static readonly Color CursorColor = new(0.85f, 0.55f, 0.15f);

        private readonly CompositeDisposable _disposables = new();
        private Image[,] _cells;
        private GridCursor _cursor;
        private CursorResolver _resolver;
        private Text _caption;

        public static InventoryWindow Create(Transform parent, NavigationController navigation, ConfirmDialog dialog)
        {
            var (window, panel) = SampleUI.CreateWindow<InventoryWindow>("InventoryWindow", parent, new Vector2(760f, 640f), "持ち物");
            window.Build(panel, navigation, dialog);
            return window;
        }

        protected override void OnDestroy()
        {
            _disposables.Dispose();
            _cursor?.Dispose();
            base.OnDestroy();
        }

        private void Build(RectTransform panel, NavigationController navigation, ConfirmDialog dialog)
        {
            var pitch = CELL_SIZE + CELL_SPACING;
            var area = SampleUI.CreateRect("Grid", panel, new Vector2(0f, 30f), new Vector2(COLUMNS * pitch, ROWS * pitch));
            _cells = new Image[COLUMNS, ROWS];
            for (var x = 0; x < COLUMNS; x++)
            {
                for (var y = 0; y < ROWS; y++)
                {
                    // 行は上が 0 なので、y が増えるほど下に置く
                    var position = new Vector2((x - (COLUMNS - 1) / 2f) * pitch, ((ROWS - 1) / 2f - y) * pitch);
                    _cells[x, y] = SampleUI.CreatePanel($"Cell {x},{y}", area, position, new Vector2(CELL_SIZE, CELL_SIZE), CellColor);
                }
            }
            _caption = SampleUI.CreateText("", panel, new Vector2(0f, -200f), new Vector2(700f, 50f), 26);
            var close = SampleUI.CreateButton("閉じる", panel, new Vector2(0f, -265f), new Vector2(260f, 64f));
            SetCloseButton(close);

            _cursor = new GridCursor(COLUMNS, ROWS, cell => !IsLocked(cell))
                .OnEdge(NavigationDirection.Left, EdgePolicy.WrapRow)
                .OnEdge(NavigationDirection.Right, EdgePolicy.WrapRow)
                .OnEdge(NavigationDirection.Down, EdgePolicy.Exit(close));
            _cursor.OnMoved.Subscribe(_ => Refresh()).AddTo(_disposables);
            _cursor.OnFocusChanged.Subscribe(_ => Refresh()).AddTo(_disposables);
            _cursor.OnSubmitted
                .Subscribe(_ =>
                {
                    var cell = _cursor.Position;
                    dialog.Open($"マス ({cell.x}, {cell.y}) の道具を捨てますか？", () => Debug.Log($"[arinn sample] ({cell.x}, {cell.y}) を捨てた"));
                })
                .AddTo(_disposables);

            // アンカーは Grid の全面に置かれるので、閉じるボタンから上へ押すとカーソルへ戻れる
            _resolver = new CursorResolver(_cursor, area);
            SetDefaultFocusElement(_resolver.Anchor);
            navigation.Register(this, new NavigationScope(transform).UseResolver(_resolver));
            Refresh();
        }

        // 右端の列の真ん中だけ鍵がかかっている
        private static bool IsLocked(Vector2Int cell) => cell.x == COLUMNS - 1 && cell.y == 1;

        private void Refresh()
        {
            for (var x = 0; x < COLUMNS; x++)
            {
                for (var y = 0; y < ROWS; y++)
                {
                    var cell = new Vector2Int(x, y);
                    var isCursor = _cursor.IsFocused && _cursor.Position == cell;
                    _cells[x, y].color = isCursor ? CursorColor : IsLocked(cell) ? LockedColor : CellColor;
                }
            }
            _caption.text = _cursor.IsFocused ? $"マス ({_cursor.Position.x}, {_cursor.Position.y})　決定で捨てる" : "";
        }
    }
}
