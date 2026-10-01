using UnityEngine;
using UnityEngine.UI;

namespace WordRPG.UI
{
    public static class Palette
    {
        public static readonly Color Background = new Color(0.07f, 0.08f, 0.14f);
        public static readonly Color Panel = new Color(0.13f, 0.15f, 0.24f);
        public static readonly Color PanelLight = new Color(0.19f, 0.22f, 0.34f);
        public static readonly Color Text = new Color(0.95f, 0.96f, 1f);
        public static readonly Color TextDim = new Color(0.65f, 0.7f, 0.82f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
        public static readonly Color Good = new Color(0.25f, 0.75f, 0.4f);
        public static readonly Color Bad = new Color(0.9f, 0.3f, 0.3f);
        public static readonly Color Info = new Color(0.3f, 0.6f, 0.95f);
        public static readonly Color Button = new Color(0.24f, 0.3f, 0.52f);
    }

    // 코드로 uGUI를 조립하는 도우미. 아트가 들어오기 전 플레이스홀더 UI용
    public static class UiKit
    {
        private static Font font;

        // 한글 글리프가 있는 OS 폰트 (Windows / Android / iOS). 정식 폰트는 이후 TMP로 교체
        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Malgun Gothic", "Noto Sans CJK KR", "Noto Sans KR", "Apple SD Gothic Neo", "Arial" }, 36);
                }
                return font;
            }
        }

        public static RectTransform Rect(string name, Transform parent, float minX, float minY, float maxX, float maxY)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent) => Rect(name, parent, 0, 0, 1, 1);

        public static void Pad(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Pad(RectTransform rt, float all) => Pad(rt, all, all, all, all);

        public static Image Panel(string name, Transform parent, Color color, float minX = 0, float minY = 0,
            float maxX = 1, float maxY = 1)
        {
            var rt = Rect(name, parent, minX, minY, maxX, maxY);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            float minX, float minY, float maxX, float maxY, TextAnchor anchor = TextAnchor.MiddleCenter,
            FontStyle style = FontStyle.Normal, bool bestFit = false, int minSize = 20)
        {
            var rt = Rect(name, parent, minX, minY, maxX, maxY);
            var label = rt.gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
            label.fontStyle = style;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            if (bestFit)
            {
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = minSize;
                label.resizeTextMaxSize = size;
            }
            return label;
        }

        public static Button MakeButton(string name, Transform parent, string text, Color color, int fontSize,
            float minX, float minY, float maxX, float maxY, bool bestFit = false)
        {
            var image = Panel(name, parent, color, minX, minY, maxX, maxY);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            button.colors = colors;

            var label = Label("Label", image.transform, text, fontSize, Palette.Text, 0, 0, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Bold, bestFit, 22);
            Pad(label.rectTransform, 12, 4, 12, 4);
            return button;
        }

        public static Text LabelOf(Button button) => button.GetComponentInChildren<Text>();

        public static void SetColor(Button button, Color color) => button.targetGraphic.color = color;
    }
}
