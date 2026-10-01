using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WordRPG.Field;
using WordRPG.Game;
using WordRPG.Items;
using WordRPG.Monsters;
using WordRPG.UI;
using WordRPG.Words;
using static WordRPG.Tests.UiDriver;

namespace WordRPG.Tests
{
    // 지역 이동(출입구)과 보스전 통합 테스트 — 실제 가상 패드로 걷는다
    public class DungeonScreenTests
    {
        //  A (바깥)   y=3 #D#   ← 출입구 (1,3)
        //             y=2 #.#
        //             y=1 #P#
        //             y=0 ###
        private const string OutsideMap = "#D#\n#.#\n#P#\n###";

        //  B (안쪽)   y=3 ###
        //             y=2 #P#
        //             y=1 #.#
        //             y=0 #D#   ← 출입구 (1,0)
        private const string InsideMap = "###\n#P#\n#.#\n#D#";

        private static WordDatabase Words()
        {
            var words = ScriptableObject.CreateInstance<WordDatabase>();
            words.ReplaceWords(TestData.SampleWords());
            return words;
        }

        private static MonsterSpecies Hero()
        {
            var strike = TestData.Skill("strike", SkillKind.Damage, SkillTarget.SingleEnemy, 40);
            return TestData.Species("hero", new MonsterStats(100, 30, 10), new MonsterStats(0, 0, 0), strike);
        }

        private static FieldArea Area(string id, string name, string map, MonsterSpecies enemy)
        {
            var table = ScriptableObject.CreateInstance<EncounterTable>()
                .Set("entries", new List<EncounterTable.Entry> { new EncounterTable.Entry(enemy, 1, 1, 1) });
            return ScriptableObject.CreateInstance<FieldArea>()
                .Set("areaId", id).Set("displayName", name).Set("map", map)
                .Set("encounters", table).Set("words", Words());
        }

        private static (FieldArea outside, FieldArea inside) LinkedAreas(MonsterSpecies enemy)
        {
            var outside = Area("outside", "바깥 들판", OutsideMap, enemy);
            var inside = Area("inside", "안쪽 서고", InsideMap, enemy).Set("theme", FieldTheme.Library);
            outside.Set("exits", new List<AreaExit> { new AreaExit(inside, 0) });
            inside.Set("exits", new List<AreaExit> { new AreaExit(outside, 0) });
            return (outside, inside);
        }

        private static FieldScreen CreateField(FieldArea area, GameSession session, GameDatabase database = null)
        {
            var go = new GameObject("DungeonFieldUnderTest");
            var field = go.AddComponent<FieldScreen>();
            field.Configure(area, session, step: 0.05f, animScale: 0.01f, gameDatabase: database);
            return field;
        }

        [UnityTest]
        public IEnumerator DoorsMoveBetweenAreasAndBack()
        {
            var hero = Hero();
            var (outside, inside) = LinkedAreas(hero);
            var session = GameSession.NewGame(new[] { hero }, 1);
            var field = CreateField(outside, session);
            yield return null;
            yield return null;

            // 위, 위 → 출입구를 밟으면 안쪽 서고의 출입구 칸에 나타남
            yield return HoldPad(field, "Pad_Up", () => field.IsMoving);
            yield return HoldPad(field, "Pad_Up", () => field.IsMoving);
            yield return WaitFor(() => field.CurrentArea == inside && !field.IsInBattle);

            Assert.AreSame(inside, field.CurrentArea);
            Assert.AreEqual(new Vector2Int(1, 0), field.PlayerCell);
            Assert.AreEqual("inside", session.World.AreaId, "현재 지역이 세이브 상태에 기록됨");
            StringAssert.Contains("안쪽 서고", AllText(field.transform.Find("FieldHud/SafeArea/TopBar")));
            StringAssert.Contains("안쪽 서고", field.ToastMessage);

            // 도착하자마자 되돌아가지 않음
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.AreSame(inside, field.CurrentArea);

            // 위로 한 칸 갔다가 다시 아래 출입구 → 바깥 들판의 출입구로
            yield return HoldPad(field, "Pad_Up", () => field.IsMoving);
            yield return HoldPad(field, "Pad_Down", () => field.IsMoving);
            yield return WaitFor(() => field.CurrentArea == outside && !field.IsInBattle);

            Assert.AreSame(outside, field.CurrentArea);
            Assert.AreEqual(new Vector2Int(1, 3), field.PlayerCell);

            Object.Destroy(field.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossIsFoughtOnceAndRemembered()
        {
            var hero = Hero();
            var bite = TestData.Skill("bite", SkillKind.Damage, SkillTarget.SingleEnemy, 1);
            var king = TestData.Species("king", new MonsterStats(40, 1, 50), new MonsterStats(0, 0, 0), bite)
                .Set("displayName", "까먹대왕").Set("expReward", 10);
            var area = Area("lair", "보스 방", "#####\n#.B.#\n#.P.#\n#####", hero)
                .Set("boss", new BossEncounter(king, 3));
            var session = GameSession.NewGame(new[] { hero }, 1);
            var field = CreateField(area, session);
            yield return null;
            yield return null;

            // 위 = 보스에 부딪힘 → 보스전
            yield return HoldPad(field, "Pad_Up", () => field.IsInBattle);
            yield return WaitFor(() => field.Battle.IsRunning);
            Assert.IsTrue(field.Battle.IsRunning);
            Assert.AreSame(king, field.Battle.Engine.Enemies[0].Monster.Species);
            Assert.AreEqual(3, field.Battle.Engine.Enemies[0].Monster.Level);
            StringAssert.Contains("보스 출현", AllText(field.Battle));

            yield return PlayUntilResult(field.Battle, answerCorrectly: true);
            FindButton(field.Battle.transform, "ResultButton_Primary").onClick.Invoke();
            yield return WaitFor(() => !field.IsInBattle);

            Assert.IsTrue(session.World.IsBossDefeated("lair:boss"));
            StringAssert.Contains("까먹대왕을 물리쳤다", field.ToastMessage);

            // 다시 부딪혀도 전투 없음
            yield return new WaitForSecondsRealtime(0.7f);
            yield return HoldPad(field, "Pad_Up", () => field.ToastMessage.Contains("있던 자리"), maxSeconds: 2f);
            Assert.IsFalse(field.IsInBattle);
            StringAssert.Contains("까먹대왕이 있던 자리", field.ToastMessage);
            Assert.AreEqual(1, session.Record.BattlesWon);

            Object.Destroy(field.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartsInTheAreaWhereTheGameWasSaved()
        {
            var hero = Hero();
            var (outside, inside) = LinkedAreas(hero);
            var database = ScriptableObject.CreateInstance<GameDatabase>();
            database.ReplaceContents(new[] { hero }, new ItemData[0], new[] { outside, inside });
            var session = GameSession.NewGame(new[] { hero }, 1);
            session.World.SetPosition("inside", new Vector2Int(1, 1));

            var field = CreateField(outside, session, database);
            yield return null;
            yield return null;

            Assert.AreSame(inside, field.CurrentArea, "세이브의 마지막 지역에서 시작");
            Assert.AreEqual(new Vector2Int(1, 1), field.PlayerCell);

            Object.Destroy(field.gameObject);
            yield return null;
        }
    }
}
