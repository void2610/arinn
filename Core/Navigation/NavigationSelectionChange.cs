using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// 選択が変わったきっかけ。
    /// </summary>
    public enum SelectionChangeSource
    {
        /// <summary> フォーカスの設定・ウィンドウの開閉・Unity の移動など、ライブラリの入力処理以外 </summary>
        Program,

        /// <summary> 方向入力による移動 </summary>
        Input,

        /// <summary> ポインタのホバー </summary>
        Hover,
    }

    /// <summary>
    /// 選択の変化。方向入力による移動と、プログラムやホバーによる変更を区別する（選択の演出を入力のときだけ出す、など）。
    /// </summary>
    public readonly struct NavigationSelectionChange
    {
        /// <summary>
        /// 新しく選択された要素。
        /// </summary>
        public GameObject Target { get; }

        public SelectionChangeSource Source { get; }

        /// <summary>
        /// 方向入力による移動か。
        /// </summary>
        public bool ByInput => Source == SelectionChangeSource.Input;

        public NavigationSelectionChange(GameObject target, SelectionChangeSource source)
        {
            Target = target;
            Source = source;
        }
    }
}
