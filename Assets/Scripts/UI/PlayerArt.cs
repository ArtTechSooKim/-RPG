using System.Collections.Generic;
using UnityEngine;
using WordRPG.Field;

namespace WordRPG.UI
{
    // 필드 주인공 그림 (Ninja Adventure 'Boy' 시트, 16×16 칸).
    // 시트: 열 = 방향(아래·위·왼쪽·오른쪽), 행 = 걷기 4프레임(첫 행 = 서 있는 모습). 시트가 없으면 임시 도트
    public static class PlayerArt
    {
        public const string SheetPath = "Art/NinjaAdventure/Player/Boy";
        public const float FramesPerSecond = 8f;
        private const int Cell = 16;
        private const int WalkFrames = 4;

        private static Texture2D sheet;
        private static bool loaded;
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        public static Sprite Get(Direction facing, int step)
        {
            if (!loaded)
            {
                loaded = true;
                sheet = Resources.Load<Texture2D>(SheetPath);
            }
            if (sheet == null) return PlaceholderArt.Player(facing);

            int column = Column(facing);
            int row = ((step % WalkFrames) + WalkFrames) % WalkFrames;
            int key = column * 16 + row;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var rect = new Rect(column * Cell, sheet.height - (row + 1) * Cell, Cell, Cell);
            var sprite = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.5f), Cell, 0, SpriteMeshType.FullRect);
            sprite.name = $"player_{facing}_{row}";
            Cache[key] = sprite;
            return sprite;
        }

        private static int Column(Direction facing)
        {
            switch (facing)
            {
                case Direction.Down: return 0;
                case Direction.Up: return 1;
                case Direction.Left: return 2;
                default: return 3;
            }
        }
    }
}
