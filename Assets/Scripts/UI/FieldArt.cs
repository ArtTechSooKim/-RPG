using System.Collections.Generic;
using UnityEngine;
using WordRPG.Field;

namespace WordRPG.UI
{
    // 필드 타일 그림 (Ninja Adventure 팩에서 Tools/import_ninja_art.py 가 만든 Art/NinjaAdventure/Tiles/{테마}_{종류}[_done]).
    // 한 장 = 한 칸 (보스처럼 큰 그림은 48px이어도 한 칸에 맞춘다). 파일이 없으면 코드로 그린 임시 도트
    public static class FieldArt
    {
        private const string Folder = "Art/NinjaAdventure/Tiles/";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        // done: 연 보물상자, 쓰러뜨린 보스 자리
        public static Sprite ForTile(FieldTile tile, FieldTheme theme, bool done)
        {
            bool hasDone = done && (tile == FieldTile.Chest || tile == FieldTile.Boss);
            return Load($"{theme}_{tile}{(hasDone ? "_done" : "")}") ?? PlaceholderArt.ForTile(tile, theme, done);
        }

        // 출입구: 잠겼으면 {테마}_Door_locked(막힌 길), 아니면 도착 지역 테마 전용 그림 {테마}_Door_{도착 테마}
        // (예: 초원 → 숲은 그늘진 숲길 입구)이 있으면 그것, 없으면 {테마}_Door
        public static Sprite ForDoor(FieldTheme theme, FieldTheme? target, bool locked)
        {
            if (locked) return Load($"{theme}_Door_locked") ?? PlaceholderArt.LockedDoor(theme);
            if (target.HasValue && target.Value != theme)
            {
                var special = Load($"{theme}_Door_{target.Value}");
                if (special != null) return special;
            }
            return ForTile(FieldTile.Door, theme, false);
        }

        private static Sprite Load(string key)
        {
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var texture = Resources.Load<Texture2D>(Folder + key);
            Sprite sprite = null;
            if (texture != null)
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f),
                    texture.width, 0, SpriteMeshType.FullRect);
                sprite.name = key;
            }
            Cache[key] = sprite;
            return sprite;
        }
    }
}
