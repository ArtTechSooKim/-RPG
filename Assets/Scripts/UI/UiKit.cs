using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WordRPG.Items;

namespace WordRPG.UI
{
    // 색은 Figma UI 키트(WordRPG UI Kit)의 변수와 같은 값. 바꿀 때는 양쪽을 같이 바꿀 것
    public static class Palette
    {
        public static readonly Color Background = new Color(0.07f, 0.08f, 0.14f);
        public static readonly Color Panel = new Color(0.13f, 0.15f, 0.24f);
        public static readonly Color PanelLight = new Color(0.19f, 0.22f, 0.34f);
        public static readonly Color Track = new Color(0.04f, 0.05f, 0.09f);
        public static readonly Color Scrim = new Color(0f, 0f, 0f, 0.78f);
        public static readonly Color Text = new Color(0.95f, 0.96f, 1f);
        public static readonly Color TextDim = new Color(0.65f, 0.7f, 0.82f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
        public static readonly Color Good = new Color(0.25f, 0.75f, 0.4f);
        public static readonly Color Bad = new Color(0.9f, 0.3f, 0.3f);
        public static readonly Color Info = new Color(0.3f, 0.6f, 0.95f);
        public static readonly Color Button = new Color(0.24f, 0.3f, 0.52f);
        public static readonly Color Disabled = new Color(0.3f, 0.32f, 0.4f);
        public static readonly Color Neutral = new Color(0.35f, 0.35f, 0.4f);
        public static readonly Color Attack = new Color(0.6f, 0.25f, 0.28f);
        public static readonly Color Heal = new Color(0.2f, 0.5f, 0.33f);
        public static readonly Color Guard = new Color(0.22f, 0.38f, 0.62f);
        public static readonly Color Evolve = new Color(0.75f, 0.45f, 0.1f);
        public static readonly Color Confirm = new Color(0.85f, 0.25f, 0.25f);
        public static readonly Color OnAccent = Background;
        public static readonly Color MasteryLearning = new Color(0.2f, 0.28f, 0.45f);
        public static readonly Color MasteryReviewing = new Color(0.2f, 0.38f, 0.5f);
        public static readonly Color MasteryProficient = new Color(0.2f, 0.5f, 0.42f);
        public static readonly Color MasteryMastered = new Color(0.5f, 0.42f, 0.15f);
    }

    // 글꼴: 제목·숫자 = Jua, 본문 = Noto Sans KR (둘 다 OFL, Assets/Resources/UI/Fonts)
    public static class UiFonts
    {
        private static Font display, regular, bold, fallback;

        public static Font Display => display != null ? display : display = Load("UI/Fonts/Jua-Regular");
        public static Font Regular => regular != null ? regular : regular = Load("UI/Fonts/NotoSansKR-Regular");
        public static Font Bold => bold != null ? bold : bold = Load("UI/Fonts/NotoSansKR-Bold");

        private static Font Load(string path)
        {
            var font = Resources.Load<Font>(path);
            if (font != null) return font;
            // 글꼴 파일이 없을 때(예: 에셋을 지운 경우) 한글이 나오는 OS 글꼴로 대체
            if (fallback == null)
                fallback = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Noto Sans CJK KR", "Noto Sans KR", "Apple SD Gothic Neo", "Arial" }, 36);
            return fallback;
        }
    }

    // 코드로 uGUI를 조립하는 도우미
    public static class UiKit
    {
        public const int RadiusSm = 8;
        public const int RadiusMd = 16;
        public const int RadiusLg = 24;
        private const int PillRadius = 32;

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static Font Font => UiFonts.Regular;

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

        // 둥근 모서리 판 (Figma radius/sm·md·lg)
        public static Image RoundPanel(string name, Transform parent, Color color, int radius,
            float minX = 0, float minY = 0, float maxX = 1, float maxY = 1)
        {
            var image = Panel(name, parent, color, minX, minY, maxX, maxY);
            Round(image, radius);
            return image;
        }

        public static Image Round(Image image, int radius)
        {
            image.sprite = RoundedSprite(radius, 0);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            return image;
        }

        // 양 끝이 반원인 막대 (HP 바, 태그). 높이가 바뀌어도 반원이 유지된다
        public static Image Pill(Image image)
        {
            image.sprite = RoundedSprite(PillRadius, 0);
            image.type = Image.Type.Sliced;
            if (image.GetComponent<AutoPill>() == null) image.gameObject.AddComponent<AutoPill>();
            return image;
        }

        // 둥근 테두리만 (차례·대상 표시)
        public static Image Outline(Image image, int radius, int thickness)
        {
            image.sprite = RoundedSprite(radius, thickness);
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            image.pixelsPerUnitMultiplier = 1f;
            return image;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            float minX, float minY, float maxX, float maxY, TextAnchor anchor = TextAnchor.MiddleCenter,
            FontStyle style = FontStyle.Normal, bool bestFit = false, int minSize = 20)
        {
            var rt = Rect(name, parent, minX, minY, maxX, maxY);
            var label = rt.gameObject.AddComponent<Text>();
            // 굵은 글씨는 가짜 굵기 대신 Noto Sans KR Bold 글꼴을 쓴다
            label.font = style == FontStyle.Bold ? UiFonts.Bold : UiFonts.Regular;
            label.fontStyle = style == FontStyle.Bold ? FontStyle.Normal : style;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
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

        // 제목·숫자용 Jua 글꼴로 바꾼다
        public static Text Display(Text label)
        {
            label.font = UiFonts.Display;
            label.fontStyle = FontStyle.Normal;
            return label;
        }

        public static Button MakeButton(string name, Transform parent, string text, Color color, int fontSize,
            float minX, float minY, float maxX, float maxY, bool bestFit = false)
        {
            var image = RoundPanel(name, parent, color, RadiusMd, minX, minY, maxX, maxY);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            button.colors = colors;

            var label = Display(Label("Label", image.transform, text, fontSize, Palette.Text, 0, 0, 1, 1,
                TextAnchor.MiddleCenter, FontStyle.Normal, bestFit, 22));
            Pad(label.rectTransform, 16, 4, 16, 4);
            return button;
        }

        // [아이콘] 제목 — 가운데 정렬 한 줄 (아이콘이 없으면 글자만). 반환: 글자
        public static Text IconTitle(string name, Transform parent, Sprite icon, string text, int size, Color color,
            float minX, float minY, float maxX, float maxY, bool display = true)
        {
            var row = Rect(name, parent, minX, minY, maxX, maxY);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            if (icon != null)
            {
                var image = IconImage("Icon", row, icon, 0, 0, 1, 1);
                var element = image.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = element.preferredHeight = size * 1.15f;
            }
            var label = Label("Text", row, text, size, color, 0, 0, 1, 1);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            return display ? Display(label) : label;
        }

        public static Text LabelOf(Button button) => button.GetComponentInChildren<Text>();

        public static void SetColor(Button button, Color color) => button.targetGraphic.color = color;

        // ------------------------------------------------------------ 아이콘 (Figma에서 내보낸 PNG, Assets/Resources/UI/Icons)

        public static Sprite Icon(string name) => LoadSprite("UI/Icons/" + name);

        // 아이템에 아이콘이 지정돼 있으면 그것, 없으면 itemId 이름의 PNG
        public static Sprite ItemIcon(ItemData item)
        {
            if (item == null) return null;
            return item.Icon != null ? item.Icon : LoadSprite("UI/Icons/Items/" + item.ItemId);
        }

        public static Image IconImage(string name, Transform parent, Sprite sprite,
            float minX, float minY, float maxX, float maxY)
        {
            var image = Panel(name, parent, Color.white, minX, minY, maxX, maxY);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        private static Sprite LoadSprite(string path)
        {
            if (SpriteCache.TryGetValue(path, out var cached)) return cached;
            var sprite = Resources.Load<Sprite>(path);
            SpriteCache[path] = sprite;
            return sprite;
        }

        // 흰색 둥근 사각형(또는 테두리) 텍스처를 코드로 만들어 9-slice 스프라이트로 쓴다. 1픽셀 = 캔버스 1단위
        private static Sprite RoundedSprite(int radius, int thickness)
        {
            string key = $"round_{radius}_{thickness}";
            if (SpriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

            int border = radius + 2;
            int size = border * 2 + 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = key
            };
            var pixels = new Color[size * size];
            float lo = border, hi = size - border;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = x + 0.5f, cy = y + 0.5f;
                float dx = Mathf.Max(lo - cx, 0f, cx - hi), dy = Mathf.Max(lo - cy, 0f, cy - hi);
                float outside = Mathf.Sqrt(dx * dx + dy * dy) - radius; // 바깥쪽 둥근 경계까지 거리
                float alpha = Mathf.Clamp01(0.5f - outside);
                // 사각형 가장자리(코너 아닌 곳)도 radius 안쪽으로 맞춘다
                float edge = Mathf.Min(Mathf.Min(cx, size - cx), Mathf.Min(cy, size - cy)) - (border - radius);
                if (dx == 0f && dy == 0f) alpha = Mathf.Clamp01(edge + 0.5f);
                if (thickness > 0)
                {
                    float inner = Mathf.Clamp01(-outside - thickness + 0.5f);
                    if (dx == 0f && dy == 0f) inner = Mathf.Clamp01(edge - thickness + 0.5f);
                    alpha = Mathf.Clamp01(alpha - inner);
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = key;
            SpriteCache[key] = sprite;
            return sprite;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            // 이 프로젝트는 Input System 전용이라 StandaloneInputModule이 아닌 InputSystemUIInputModule을 쓴다
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static void ApplySafeArea(RectTransform rt)
        {
            var safe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        }

        // 한국어 조사: 받침 있으면 withBatchim, 없으면 withoutBatchim. ("으로/로"는 ㄹ받침이면 "로")
        //   WithJosa("펜촉이", "이", "가") → "펜촉이가",  WithJosa("백과거북", "으로", "로") → "백과거북으로"
        public static string WithJosa(string word, string withBatchim, string withoutBatchim)
        {
            if (string.IsNullOrEmpty(word)) return word;
            char last = word[word.Length - 1];
            if (last < 0xAC00 || last > 0xD7A3) return word + withoutBatchim; // 한글이 아니면 받침 없는 쪽
            int batchim = (last - 0xAC00) % 28;
            bool useFirst = batchim != 0 && !(withBatchim == "으로" && batchim == 8);
            return word + (useFirst ? withBatchim : withoutBatchim);
        }
    }
}
