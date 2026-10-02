using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WordRPG.Field;
using WordRPG.Items;
using WordRPG.Monsters;

namespace WordRPG.Tests
{
    // [확인] 버튼 대상 고르기 · 이름표 규칙
    public class FieldInteractionTests
    {
        //   y=3  #####
        //   y=2  #ES.#   ← 진화의 제단 (1,2), 상점 (2,2)
        //   y=1  #FP.#   ← 회복의 샘 (1,1), 시작 (2,1)
        //   y=0  #####
        private static readonly FieldMap Town = FieldMap.Parse("#####\n#ES.#\n#FP.#\n#####");

        [Test]
        public void ConfirmPrefersFacedObject()
        {
            Assert.AreEqual(Direction.Left, FieldInteraction.FindTarget(Town, new Vector2Int(2, 1), Direction.Left), "바라보는 샘");
            Assert.AreEqual(Direction.Up, FieldInteraction.FindTarget(Town, new Vector2Int(2, 1), Direction.Up), "바라보는 상점");
        }

        [Test]
        public void ConfirmFallsBackToSideInFixedOrder()
        {
            // 아래(벽)를 보고 있으면 옆 칸 중 위 → 아래 → 왼 → 오른 순: 위의 상점이 먼저
            Assert.AreEqual(Direction.Up, FieldInteraction.FindTarget(Town, new Vector2Int(2, 1), Direction.Down));
            // (3,1): 위는 길, 왼쪽은 시작 칸(길) → 쓸 것 없음
            Assert.IsNull(FieldInteraction.FindTarget(Town, new Vector2Int(3, 1), Direction.Up));
        }

        [Test]
        public void GrassWallAndDoorAreNotConfirmTargets()
        {
            var map = FieldMap.Parse("#####\n#,D~#\n#.P.#\n#####");
            Assert.IsNull(FieldInteraction.FindTarget(map, new Vector2Int(2, 1), Direction.Up));
        }

        [Test]
        public void NameTagsOnLandmarksButNotChests()
        {
            Assert.IsTrue(FieldInteraction.HasNameTag(FieldTile.Altar));
            Assert.IsTrue(FieldInteraction.HasNameTag(FieldTile.Shop));
            Assert.IsTrue(FieldInteraction.HasNameTag(FieldTile.Fountain));
            Assert.IsTrue(FieldInteraction.HasNameTag(FieldTile.Boss));
            Assert.IsTrue(FieldInteraction.HasNameTag(FieldTile.Door));
            Assert.IsFalse(FieldInteraction.HasNameTag(FieldTile.Chest), "상자는 모양으로 알 수 있음");
            Assert.IsFalse(FieldInteraction.HasNameTag(FieldTile.Grass));

            var map = FieldMap.Parse("######\n#EC.D#\n#..P.#\n######");
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(1, 2), new Vector2Int(4, 2) }, FieldInteraction.Landmarks(map));
        }

        [Test]
        public void NearMeansWithinTwoCellsEachWay()
        {
            var player = new Vector2Int(5, 5);
            Assert.IsTrue(FieldInteraction.IsNear(player, new Vector2Int(7, 3)), "대각선 2칸");
            Assert.IsTrue(FieldInteraction.IsNear(player, new Vector2Int(5, 5)));
            Assert.IsFalse(FieldInteraction.IsNear(player, new Vector2Int(8, 5)), "3칸");
            Assert.IsFalse(FieldInteraction.IsNear(player, new Vector2Int(5, 2)));
        }

        [Test]
        public void LandmarkNamesComeFromData()
        {
            var king = TestData.Species("king", new MonsterStats(40, 1, 50), new MonsterStats(0, 0, 0)).Set("displayName", "까먹대왕");
            var shop = ScriptableObject.CreateInstance<ShopData>().Set("displayName", "초원 마을 잡화점");
            var library = ScriptableObject.CreateInstance<FieldArea>().Set("areaId", "library").Set("displayName", "잊혀진 서고")
                .Set("map", "###\n#P#\n#D#");
            //   y=3  #######
            //   y=2  #ESFBD#
            //   y=1  #C.P..#
            var area = ScriptableObject.CreateInstance<FieldArea>().Set("areaId", "town")
                .Set("map", "#######\n#ESFBD#\n#C.P..#\n#######")
                .Set("shop", shop).Set("boss", new BossEncounter(king, 7))
                .Set("exits", new List<AreaExit> { new AreaExit(library, 0) });

            Assert.AreEqual("진화의 제단", area.LandmarkName(new Vector2Int(1, 2)));
            Assert.AreEqual("초원 마을 잡화점", area.LandmarkName(new Vector2Int(2, 2)), "상점은 상점 이름");
            Assert.AreEqual("회복의 샘", area.LandmarkName(new Vector2Int(3, 2)));
            Assert.AreEqual("까먹대왕", area.LandmarkName(new Vector2Int(4, 2)), "보스는 보스 이름");
            Assert.AreEqual("잊혀진 서고", area.LandmarkName(new Vector2Int(5, 2)), "출입구는 도착 지역 이름");
            Assert.IsNull(area.LandmarkName(new Vector2Int(1, 1)), "보물상자");
            Assert.IsNull(area.LandmarkName(new Vector2Int(2, 1)), "길");

            var plain = ScriptableObject.CreateInstance<FieldArea>().Set("areaId", "plain").Set("map", "#####\n#SD.#\n#.P.#\n#####");
            Assert.AreEqual("상점", plain.LandmarkName(new Vector2Int(1, 2)), "상점 데이터가 없으면 그냥 '상점'");
            Assert.IsNull(plain.LandmarkName(new Vector2Int(2, 2)), "연결 안 된 출입구는 이름표 없음");
        }

        [Test]
        public void WalkerCanTurnToFaceTarget()
        {
            var walker = new FieldWalker(Town, new Vector2Int(2, 1));
            Assert.AreEqual(Direction.Down, walker.Facing);
            walker.Face(Direction.Left);
            Assert.AreEqual(Direction.Left, walker.Facing);
            Assert.AreEqual(new Vector2Int(2, 1), walker.Position, "제자리에서 돌기만");
        }

        [Test]
        public void ExpToNextLevelCountsDown()
        {
            var species = TestData.Species("nib", new MonsterStats(30, 14, 8), new MonsterStats(4, 3, 1));
            var monster = new MonsterInstance(species, 5, exp: 20);
            Assert.AreEqual(LevelCurve.ExpToNextLevel(5) - 20, monster.ExpToNextLevel);
            monster.GainExp(monster.ExpToNextLevel);
            Assert.AreEqual(6, monster.Level);
            Assert.AreEqual(LevelCurve.ExpToNextLevel(6), monster.ExpToNextLevel);
            Assert.AreEqual(0, new MonsterInstance(species, LevelCurve.MaxLevel).ExpToNextLevel, "최고 레벨");
        }
    }
}
