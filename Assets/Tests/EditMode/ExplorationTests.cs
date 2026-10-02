using NUnit.Framework;
using UnityEngine;
using WordRPG.Field;
using WordRPG.Game;
using WordRPG.Monsters;
using WordRPG.Save;
using WordRPG.UI;

namespace WordRPG.Tests
{
    // 탐험 안개: 걸은 곳 둘레(반지름 3칸)만 지도에 보이고, 세이브에 남는다
    public class ExplorationTests
    {
        private static GameSession NewSession() =>
            GameSession.NewGame(TestData.Hero(new MonsterStats(10, 5, 5)), 1);

        [Test]
        public void RevealOpensCircleAroundPlayerOnce()
        {
            var world = new WorldState();
            int first = world.Reveal("meadow", 11, 11, new Vector2Int(5, 5), 3);
            Assert.Greater(first, 20, "반지름 3칸 원");
            Assert.IsTrue(world.IsExplored("meadow", new Vector2Int(5, 8)), "위로 3칸");
            Assert.IsTrue(world.IsExplored("meadow", new Vector2Int(7, 7)), "대각선 2칸");
            Assert.IsFalse(world.IsExplored("meadow", new Vector2Int(5, 9)), "4칸은 아직");
            Assert.IsFalse(world.IsExplored("meadow", new Vector2Int(8, 8)), "대각선 3칸(약 4.2)은 아직");
            Assert.AreEqual(0, world.Reveal("meadow", 11, 11, new Vector2Int(5, 5), 3), "같은 곳은 새로 밝힐 게 없음");
            Assert.IsFalse(world.IsExplored("library", new Vector2Int(5, 5)), "지역마다 따로");
        }

        [Test]
        public void RevealStaysInsideMap()
        {
            var world = new WorldState();
            int added = world.Reveal("meadow", 4, 4, new Vector2Int(0, 0), 3);
            Assert.LessOrEqual(added, 16);
            Assert.IsFalse(world.IsExplored("meadow", new Vector2Int(-1, 0)));
        }

        [Test]
        public void ExplorationSurvivesSaveAndLoad()
        {
            var session = NewSession();
            session.World.Reveal("meadow", 19, 23, new Vector2Int(8, 3));
            var json = session.ToSaveData(System.DateTime.UtcNow).ToJson();
            var loaded = SaveData.FromJson(json).World;
            Assert.IsTrue(loaded.IsExplored("meadow", new Vector2Int(8, 5)));
            Assert.IsFalse(loaded.IsExplored("meadow", new Vector2Int(8, 10)));
        }

        [Test]
        public void OldSaveWithoutExplorationStartsUnexplored()
        {
            var json = NewSession().ToSaveData(System.DateTime.UtcNow).ToJson().Replace("\"explored\"", "\"removedField\"");
            var world = SaveData.FromJson(json).World;
            Assert.IsFalse(world.IsExplored("meadow", new Vector2Int(1, 1)));
            Assert.Greater(world.Reveal("meadow", 19, 23, new Vector2Int(1, 1)), 0, "예전 세이브도 이어서 탐험");
        }

        [Test]
        public void ResizedMapKeepsExploredCellsThatStillExist()
        {
            var world = new WorldState();
            world.Reveal("meadow", 10, 10, new Vector2Int(2, 2), 1);
            world.Reveal("meadow", 12, 8, new Vector2Int(11, 7), 0); // 맵 크기가 바뀜
            Assert.IsTrue(world.IsExplored("meadow", new Vector2Int(2, 2)), "그대로 있는 칸은 기록 유지");
            Assert.IsTrue(world.IsExplored("meadow", new Vector2Int(11, 7)));
        }

        [Test]
        public void ProgressCountsCellsThatAreNotWalls()
        {
            //   ######
            //   #P..C#
            //   ######
            var area = ScriptableObject.CreateInstance<FieldArea>().Set("areaId", "hall").Set("map", "######\n#P..C#\n######");
            var session = NewSession();
            Assert.AreEqual((0, 4), session.ExplorationProgress(area));
            session.World.Reveal("hall", area.Map.Width, area.Map.Height, area.Map.Start, 1);
            Assert.AreEqual((2, 4), session.ExplorationProgress(area), "P와 오른쪽 한 칸 (벽은 안 셈)");
        }

        [Test]
        public void MinimapHidesUnexploredCellsEvenChests()
        {
            var area = ScriptableObject.CreateInstance<FieldArea>().Set("areaId", "hall").Set("map", "######\n#P..C#\n######");
            var chest = area.Map.Chests[0];
            var hidden = MinimapArt.Build(area.Map, FieldTheme.Meadow, _ => false, null, _ => false);
            Assert.AreEqual((Color)MinimapArt.Fog, (Color)hidden.GetPixel(chest.x, chest.y), "안 가 본 곳의 상자는 안 보임");
            var shown = MinimapArt.Build(area.Map, FieldTheme.Meadow, _ => false, null, _ => true);
            Assert.AreEqual((Color)MinimapArt.Chest, (Color)shown.GetPixel(chest.x, chest.y));
            Object.DestroyImmediate(hidden);
            Object.DestroyImmediate(shown);
        }
    }
}
