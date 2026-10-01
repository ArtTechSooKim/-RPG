using System;
using NUnit.Framework;
using UnityEngine;
using WordRPG.Field;
using WordRPG.UI;

namespace WordRPG.Tests
{
    // 게임이 이름으로 불러오는 소리·그림 파일이 실제로 있는지 (Tools/import_ninja_art.py 로 만든 것)
    public class AudioArtTests
    {
        [Test]
        public void EveryMusicTrackHasAClip()
        {
            foreach (Music track in Enum.GetValues(typeof(Music)))
            {
                if (track == Music.None) continue;
                string path = "Audio/Music/" + track.ToString().ToLowerInvariant();
                Assert.IsNotNull(Resources.Load<AudioClip>(path), $"Resources/{path} 없음");
            }
        }

        [Test]
        public void EverySoundEffectHasAClip()
        {
            foreach (Sfx effect in Enum.GetValues(typeof(Sfx)))
            {
                string path = "Audio/Sfx/" + effect.ToString().ToLowerInvariant();
                Assert.IsNotNull(Resources.Load<AudioClip>(path), $"Resources/{path} 없음");
            }
        }

        [Test]
        public void EveryFieldTileHasArtForEveryTheme()
        {
            foreach (FieldTheme theme in Enum.GetValues(typeof(FieldTheme)))
            foreach (FieldTile tile in Enum.GetValues(typeof(FieldTile)))
            {
                string path = $"Art/NinjaAdventure/Tiles/{theme}_{tile}";
                Assert.IsNotNull(Resources.Load<Texture2D>(path), $"Resources/{path} 없음");
            }
        }

        [Test]
        public void PlayerSheetHasFourDirectionsOfWalkFrames()
        {
            var sheet = Resources.Load<Texture2D>(PlayerArt.SheetPath);
            Assert.IsNotNull(sheet, "주인공 시트 없음");
            Assert.GreaterOrEqual(sheet.width, 64, "4방향");
            Assert.GreaterOrEqual(sheet.height, 64, "걷기 4프레임");
            Assert.AreEqual(FilterMode.Point, sheet.filterMode, "도트는 Point 필터로 또렷하게");
        }
    }
}
