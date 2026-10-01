using System;
using UnityEngine;
using UnityEngine.UI;
using WordRPG.Field;
using WordRPG.Game;

namespace WordRPG.UI
{
    // 미니맵·지도 그림: 글자 맵(FieldMap)을 한 칸 = 한 점으로 그린다 → 새 지역도 따로 손대지 않고 자동으로 나온다.
    // 지형은 은은한 색, 상자·샘·제단·상점·출입구·보스는 밝은 색 (Figma 'Minimap' / '지도')
    public static class MinimapArt
    {
        public static readonly Color32 Chest = new Color32(255, 209, 64, 255);
        public static readonly Color32 Fountain = new Color32(111, 227, 245, 255);
        public static readonly Color32 Altar = new Color32(182, 123, 230, 255);
        public static readonly Color32 Shop = new Color32(229, 83, 75, 255);
        public static readonly Color32 Door = new Color32(255, 255, 255, 255);
        public static readonly Color32 Boss = new Color32(255, 107, 138, 255);

        // done: 연 상자·쓰러뜨린 보스 → 바닥색으로
        public static Color32 ColorOf(FieldTile tile, FieldTheme theme, bool done)
        {
            bool library = theme == FieldTheme.Library;
            var floor = library ? new Color32(74, 63, 69, 255) : new Color32(217, 188, 133, 255);
            switch (tile)
            {
                case FieldTile.Wall: return library ? new Color32(154, 78, 38, 255) : new Color32(36, 80, 42, 255);
                case FieldTile.Grass: return library ? new Color32(110, 98, 115, 255) : new Color32(63, 122, 53, 255);
                case FieldTile.Water: return library ? new Color32(90, 63, 138, 255) : new Color32(79, 182, 224, 255);
                case FieldTile.Chest: return done ? floor : Chest;
                case FieldTile.Boss: return done ? floor : Boss;
                case FieldTile.Fountain: return Fountain;
                case FieldTile.Altar: return Altar;
                case FieldTile.Shop: return Shop;
                case FieldTile.Door: return Door;
                default: return floor;
            }
        }

        public static Texture2D Build(FieldMap map, FieldTheme theme, Func<Vector2Int, bool> done, Texture2D reuse = null)
        {
            var texture = reuse;
            if (texture == null || texture.width != map.Width || texture.height != map.Height)
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
                texture = new Texture2D(map.Width, map.Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "minimap"
                };
            }
            var pixels = new Color32[map.Width * map.Height];
            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var cell = new Vector2Int(x, y);
                var tile = map.Get(cell);
                bool isDone = (tile == FieldTile.Chest || tile == FieldTile.Boss) && done != null && done(cell);
                pixels[y * map.Width + x] = ColorOf(tile, theme, isDone);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }

    // 지도 그림 + 내 위치 점 (금테 흰 점). 미니맵과 큰 지도가 같이 쓴다
    public class MapPicture
    {
        public RectTransform Rect { get; private set; }
        private RawImage image;
        private RectTransform player;
        private int width = 1, height = 1;

        public static MapPicture Create(Transform parent, float minX, float minY, float maxX, float maxY, float dotSize)
        {
            var picture = new MapPicture();
            picture.Rect = UiKit.Rect("Map", parent, minX, minY, maxX, maxY);
            picture.image = picture.Rect.gameObject.AddComponent<RawImage>();
            picture.image.raycastTarget = false;
            var ring = UiKit.Pill(UiKit.Panel("Player", picture.Rect, Palette.Gold, 0, 0, 0, 0));
            ring.raycastTarget = false;
            ring.rectTransform.sizeDelta = new Vector2(dotSize, dotSize);
            var dot = UiKit.Pill(UiKit.Panel("Dot", ring.transform, Color.white));
            dot.raycastTarget = false;
            UiKit.Pad(dot.rectTransform, dotSize * 0.18f);
            picture.player = ring.rectTransform;
            return picture;
        }

        public void SetTexture(Texture2D texture)
        {
            image.texture = texture;
            width = Mathf.Max(1, texture.width);
            height = Mathf.Max(1, texture.height);
        }

        public void SetPlayer(Vector2Int cell)
        {
            var anchor = new Vector2((cell.x + 0.5f) / width, (cell.y + 0.5f) / height);
            player.anchorMin = player.anchorMax = anchor;
            player.anchoredPosition = Vector2.zero;
        }

        public Vector2 PlayerAnchor => player.anchorMin;
    }

    // 필드 왼쪽 위 미니맵 (누르면 큰 지도). 크기는 지역 맵 크기에 맞춘다 (한 칸 = 8px)
    public class MinimapView
    {
        public const float CellPixels = 8f;
        public Button Button { get; private set; }
        public MapPicture Picture { get; private set; }

        private RectTransform root;
        private Texture2D texture;
        private FieldMap map;
        private FieldTheme theme;
        private Func<Vector2Int, bool> done;

        public static MinimapView Create(Transform parent)
        {
            var view = new MinimapView();
            var panel = UiKit.RoundPanel("Minimap", parent, Palette.Scrim, UiKit.RadiusMd, 0, 0.865f, 0, 0.865f);
            view.root = panel.rectTransform;
            view.root.pivot = new Vector2(0, 1);
            view.root.anchoredPosition = new Vector2(24, 0);
            view.Button = UiKit.AddButton(panel);
            view.Picture = MapPicture.Create(panel.transform, 0, 0, 1, 1, 14);
            UiKit.Pad(view.Picture.Rect, 12);
            return view;
        }

        public void SetArea(FieldMap fieldMap, FieldTheme fieldTheme, Func<Vector2Int, bool> isDone)
        {
            map = fieldMap;
            theme = fieldTheme;
            done = isDone;
            root.sizeDelta = new Vector2(map.Width * CellPixels + 24, map.Height * CellPixels + 24);
            Redraw();
        }

        // 상자를 열거나 보스를 쓰러뜨렸을 때
        public void Redraw()
        {
            if (map == null) return;
            texture = MinimapArt.Build(map, theme, done, texture);
            Picture.SetTexture(texture);
        }

        public Texture2D Texture => texture;
    }

    // 큰 지도 (Figma '지도'): 지역 이름, 지도, 범례, 남은 보물상자
    public class MapView
    {
        public GameObject Root { get; private set; }
        public bool IsOpen => Root != null && Root.activeSelf;

        private Text title, info;
        private MapPicture picture;
        private RectTransform frame;

        public static MapView Create(Transform parent)
        {
            var view = new MapView();
            var root = UiKit.Stretch("MapView", parent);
            view.Root = root.gameObject;
            view.Root.AddComponent<Image>().color = Palette.Background;

            view.title = UiKit.Display(UiKit.Label("Title", root, "지도", 56, Palette.Gold, 0, 0.93f, 1, 0.99f));
            var panel = UiKit.RoundPanel("MapPanel", root, Palette.Panel, UiKit.RadiusLg, 0.5f, 0.6f, 0.5f, 0.6f);
            panel.raycastTarget = false;
            view.frame = panel.rectTransform;
            view.picture = MapPicture.Create(panel.transform, 0, 0, 1, 1, 36);
            UiKit.Pad(view.picture.Rect, 24);

            // 범례 두 줄
            var legend = new (string name, Color color)[]
            {
                ("나", Palette.Gold), ("보물상자", MinimapArt.Chest), ("회복의 샘", MinimapArt.Fountain), ("진화의 제단", MinimapArt.Altar),
                ("상점", MinimapArt.Shop), ("출입구", MinimapArt.Door), ("보스", MinimapArt.Boss), ("풀숲 (몬스터)", new Color32(63, 122, 53, 255)),
            };
            for (int row = 0; row < 2; row++)
            {
                var line = UiKit.Rect($"Legend_{row}", root, 0.03f, 0.205f - row * 0.04f, 0.97f, 0.24f - row * 0.04f);
                var layout = line.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 16;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                for (int i = row * 4; i < row * 4 + 4; i++)
                {
                    var chip = UiKit.Pill(UiKit.Panel($"Chip_{i}", line, Palette.Panel));
                    chip.raycastTarget = false;
                    var chipLayout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
                    chipLayout.padding = new RectOffset(16, 20, 8, 8);
                    chipLayout.spacing = 10;
                    chipLayout.childAlignment = TextAnchor.MiddleCenter;
                    chipLayout.childControlWidth = chipLayout.childControlHeight = true;
                    chipLayout.childForceExpandWidth = chipLayout.childForceExpandHeight = false;
                    var swatch = UiKit.Round(UiKit.Panel("Swatch", chip.transform, legend[i].color), UiKit.RadiusSm);
                    swatch.raycastTarget = false;
                    var size = swatch.gameObject.AddComponent<LayoutElement>();
                    size.preferredWidth = size.preferredHeight = 28;
                    var label = UiKit.Label("Name", chip.transform, legend[i].name, 28, Palette.Text, 0, 0, 1, 1);
                    label.horizontalOverflow = HorizontalWrapMode.Overflow;
                }
            }
            view.info = UiKit.Label("Info", root, "", 34, Palette.TextDim, 0.03f, 0.11f, 0.97f, 0.155f);

            var close = UiKit.MakeButton("MapCloseButton", root, "닫기", Palette.Neutral, 44, 0.05f, 0.015f, 0.95f, 0.095f);
            close.onClick.AddListener(view.Hide);
            view.Root.SetActive(false);
            return view;
        }

        public void Show(FieldArea area, Texture2D texture, Vector2Int playerCell, GameSession session)
        {
            title.text = $"지도 · {area.DisplayName}";
            picture.SetTexture(texture);
            picture.SetPlayer(playerCell);
            // 지도는 가로 936·세로 1000 안에 꽉 차게, 한 칸은 정수 px로
            float cell = Mathf.Floor(Mathf.Min(936f / texture.width, 1000f / texture.height));
            frame.sizeDelta = new Vector2(texture.width * cell + 48, texture.height * cell + 48);

            int chests = session.RemainingChests(area);
            string boss = area.Boss != null
                ? (session.World.IsBossDefeated(area.BossId) ? "   ·   보스 쓰러뜨림 ★" : $"   ·   보스 {area.Boss.Species.DisplayName}")
                : "";
            info.text = (chests > 0 ? $"남은 보물상자 {chests}개" : "보물상자를 모두 열었어요") + boss;
            Root.transform.SetAsLastSibling();
            Root.SetActive(true);
        }

        public void Hide() => Root.SetActive(false);
    }
}
