using System;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn.Samples
{
    /// <summary>
    /// サンプルの UI をコードで組み立てる補助。見た目は最低限で、arinn の使い方とは関係しない。
    /// </summary>
    internal static class SampleUI
    {
        public static readonly Color PanelColor = new(0.12f, 0.13f, 0.17f, 0.97f);
        public static readonly Color DimColor = new(0f, 0f, 0f, 0.55f);

        private static Font _font;

        private static Font Font => _font ? _font : _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas CreateCanvas(Transform parent)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        /// <summary>
        /// 親の中央を基準に、位置と大きさを指定した RectTransform を作る。
        /// </summary>
        public static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>
        /// 親の全面に広がる RectTransform を作る。
        /// </summary>
        public static RectTransform CreateStretch(string name, Transform parent)
        {
            var rect = CreateRect(name, parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            return rect;
        }

        public static Image CreatePanel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var image = CreateRect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text CreateText(string content, Transform parent, Vector2 position, Vector2 size, int fontSize = 28)
        {
            var text = CreateRect("Text", parent, position, size).gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        public static Button CreateButton(string label, Transform parent, Vector2 position, Vector2 size, Action onClick = null)
        {
            var rect = CreateRect(label, parent, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = new ColorBlock
            {
                normalColor = new Color(0.25f, 0.27f, 0.34f),
                highlightedColor = new Color(0.34f, 0.37f, 0.46f),
                selectedColor = new Color(0.85f, 0.55f, 0.15f),
                pressedColor = new Color(0.95f, 0.7f, 0.3f),
                disabledColor = new Color(0.18f, 0.18f, 0.2f, 0.6f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f,
            };
            var text = CreateText(label, rect, Vector2.zero, Vector2.zero);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>
        /// ウィンドウの土台を作る。画面全体を暗くして背面のクリックを遮り、中央にパネルを置く。
        /// </summary>
        public static (T Window, RectTransform Panel) CreateWindow<T>(string name, Transform parent, Vector2 panelSize, string title)
            where T : WindowBase
        {
            var root = CreateStretch(name, parent);
            root.gameObject.AddComponent<Image>().color = DimColor;
            var window = root.gameObject.AddComponent<T>();
            var panel = CreatePanel("Panel", root, Vector2.zero, panelSize, PanelColor).rectTransform;
            CreateText(title, panel, new Vector2(0f, panelSize.y / 2f - 40f), new Vector2(panelSize.x, 60f), 34);
            return (window, panel);
        }

        /// <summary>
        /// 縦に並ぶスクロール一覧を作る。返り値の content にボタンを足していく。
        /// </summary>
        public static (ScrollRect ScrollRect, RectTransform Content) CreateVerticalScroll(Transform parent, Vector2 position, Vector2 size)
        {
            var scrollRect = CreateRect("Scroll", parent, position, size).gameObject.AddComponent<ScrollRect>();
            var viewport = CreateStretch("Viewport", scrollRect.transform);
            // ドラッグを受けるための透明な Graphic
            viewport.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = CreateRect("Content", viewport, Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            return (scrollRect, content);
        }

        /// <summary>
        /// レイアウトグループの中に置くボタン。高さはレイアウトに任せる。
        /// </summary>
        public static Button CreateListButton(string label, Transform content, float height, Action onClick = null)
        {
            var button = CreateButton(label, content, Vector2.zero, Vector2.zero, onClick);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return button;
        }
    }
}
