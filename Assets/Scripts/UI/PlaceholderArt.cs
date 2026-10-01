using System;
using System.Collections.Generic;
using UnityEngine;
using WordRPG.Field;

namespace WordRPG.UI
{
    // 아트가 들어오기 전까지 쓰는 16x16 도트 스프라이트를 코드로 만든다.
    // 실제 아트로 바꿀 때는 이 클래스 대신 스프라이트 에셋을 쓰면 된다
    public static class PlaceholderArt
    {
        private const int Size = 16;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        private static readonly Color PathColor = new Color(0.80f, 0.71f, 0.50f);
        private static readonly Color PathDark = new Color(0.72f, 0.62f, 0.42f);
        private static readonly Color GrassBase = new Color(0.18f, 0.48f, 0.22f);
        private static readonly Color GrassBlade = new Color(0.36f, 0.72f, 0.33f);
        private static readonly Color Ground = new Color(0.33f, 0.62f, 0.32f);
        private static readonly Color Canopy = new Color(0.10f, 0.36f, 0.17f);
        private static readonly Color CanopyLight = new Color(0.18f, 0.48f, 0.24f);
        private static readonly Color Trunk = new Color(0.42f, 0.27f, 0.14f);
        private static readonly Color WaterColor = new Color(0.20f, 0.45f, 0.85f);
        private static readonly Color WaterWave = new Color(0.55f, 0.75f, 1f);

        public static readonly Color OutsideMap = new Color(0.10f, 0.22f, 0.12f);

        public static Sprite ForTile(FieldTile tile, bool openedChest = false)
        {
            switch (tile)
            {
                case FieldTile.Floor: return Make("floor", Path);
                case FieldTile.Grass: return Make("grass", Grass);
                case FieldTile.Wall: return Make("tree", Tree);
                case FieldTile.Water: return Make("water", Water);
                case FieldTile.Fountain: return Make("fountain", Fountain);
                case FieldTile.Chest: return openedChest ? Make("chest_open", ChestOpen) : Make("chest", ChestClosed);
                case FieldTile.Altar: return Make("altar", Altar);
                case FieldTile.Shop: return Make("shop", ShopStall);
                default: return Make("floor", Path);
            }
        }

        public static Sprite Player(Direction facing) => Make("player_" + facing, (x, y) => PlayerPixel(x, y, facing));

        // ------------------------------------------------------------ 픽셀 규칙

        private static Color Path(int x, int y) => Noise(x, y, 7) < 0.12f ? PathDark : PathColor;

        private static Color Grass(int x, int y)
        {
            // 4x4 칸마다 '^' 모양 풀잎
            int lx = x % 4, ly = y % 4;
            bool shift = (y / 4) % 2 == 1;
            int bladeX = shift ? 0 : 2;
            if (lx == bladeX && ly < 3) return GrassBlade;
            if (Math.Abs(lx - bladeX) == 1 && ly == 1) return GrassBlade;
            return GrassBase;
        }

        private static Color Tree(int x, int y)
        {
            if (x >= 6 && x <= 9 && y <= 4) return Trunk;
            float dx = x - 7.5f, dy = y - 9f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d <= 6.5f) return dx < -1f && dy > 1f ? CanopyLight : Canopy;
            return Ground;
        }

        private static Color Water(int x, int y) => (x + y * 3) % 8 == 0 && y % 4 == 1 ? WaterWave : WaterColor;

        private static Color Fountain(int x, int y)
        {
            float d = Distance(x, y);
            if (d <= 1.8f) return Color.white;
            if (d <= 5f) return new Color(0.3f, 0.75f, 0.95f);
            if (d <= 7f) return new Color(0.62f, 0.62f, 0.68f);
            return Path(x, y);
        }

        private static Color ChestClosed(int x, int y)
        {
            if (x < 2 || x > 13 || y < 2 || y > 11) return Path(x, y);
            if (x == 2 || x == 13 || y == 2 || y == 11) return new Color(0.32f, 0.18f, 0.08f);
            if (x >= 7 && x <= 8 && y >= 5 && y <= 8) return new Color(1f, 0.85f, 0.3f);
            if (y == 7) return new Color(0.95f, 0.75f, 0.25f);
            return new Color(0.58f, 0.35f, 0.15f);
        }

        private static Color ChestOpen(int x, int y)
        {
            if (x < 2 || x > 13 || y < 2 || y > 11) return Path(x, y);
            if (x == 2 || x == 13 || y == 2 || y == 11) return new Color(0.25f, 0.15f, 0.07f);
            if (y >= 7) return new Color(0.12f, 0.08f, 0.04f);
            return new Color(0.42f, 0.26f, 0.12f);
        }

        // 진화의 제단: 돌 받침 위에 떠 있는 보라색 수정
        private static Color Altar(int x, int y)
        {
            if (y >= 1 && y <= 5 && x >= 3 && x <= 12) return y == 5 || x == 3 || x == 12 ? new Color(0.45f, 0.45f, 0.52f) : new Color(0.62f, 0.62f, 0.7f);
            int dx = Math.Abs(x * 2 - 15), dy = Math.Abs(y * 2 - 21);
            if (dx + dy <= 8) return dx + dy <= 3 ? new Color(0.95f, 0.8f, 1f) : new Color(0.62f, 0.3f, 0.9f);
            return Path(x, y);
        }

        // 상점: 빨강·하양 줄무늬 차양 + 나무 진열대 + 금화
        private static Color ShopStall(int x, int y)
        {
            if (y >= 11 && y <= 14 && x >= 1 && x <= 14) return (x / 2) % 2 == 0 ? new Color(0.85f, 0.2f, 0.2f) : Color.white;
            if (y >= 2 && y <= 7 && x >= 2 && x <= 13)
            {
                if (x >= 6 && x <= 9 && y >= 4 && y <= 6) return new Color(1f, 0.85f, 0.25f);
                return y == 7 ? new Color(0.4f, 0.25f, 0.1f) : new Color(0.6f, 0.4f, 0.2f);
            }
            if ((x == 2 || x == 13) && y >= 8 && y <= 10) return new Color(0.4f, 0.25f, 0.1f);
            return Path(x, y);
        }

        private static Color PlayerPixel(int x, int y, Direction facing)
        {
            float d = Distance(x, y);
            if (d > 6.8f) return Color.clear;
            if (d > 5.6f) return new Color(0.55f, 0.22f, 0.05f);

            // 바라보는 방향에 눈 두 개
            Vector2Int[] eyes;
            switch (facing)
            {
                case Direction.Up: return new Color(0.85f, 0.42f, 0.12f); // 뒷모습
                case Direction.Left: eyes = new[] { new Vector2Int(4, 9), new Vector2Int(7, 9) }; break;
                case Direction.Right: eyes = new[] { new Vector2Int(8, 9), new Vector2Int(11, 9) }; break;
                default: eyes = new[] { new Vector2Int(5, 8), new Vector2Int(10, 8) }; break;
            }
            foreach (var eye in eyes)
            {
                if (x == eye.x && (y == eye.y || y == eye.y + 1)) return new Color(0.1f, 0.1f, 0.15f);
            }
            return new Color(1f, 0.58f, 0.22f);
        }

        // ------------------------------------------------------------ 도우미

        private static float Distance(int x, int y)
        {
            float dx = x - 7.5f, dy = y - 7.5f;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // 좌표 기반 고정 노이즈 (매번 같은 무늬)
        private static float Noise(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 982451653;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        private static Sprite Make(string key, Func<int, int, Color> pixel)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "placeholder_" + key
            };
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                pixels[y * Size + x] = pixel(x, y);
            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
