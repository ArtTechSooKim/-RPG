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
    // 필드 통합 테스트: 실제 가상 패드를 눌러 걷고, 상자·샘·풀숲 조우·전투 복귀까지 확인
    public class FieldScreenTests
    {
        //   y=4  #####
        //   y=3  #,,,#     ← 풀숲 (조우 확률 100%)
        //   y=2  #.C.#     ← 보물상자 (2,2)
        //   y=1  #FP.#     ← 회복의 샘 (1,1), 시작 (2,1)
        //   y=0  #####
        private const string TestMap = "#####\n#,,,#\n#.C.#\n#FP.#\n#####";

        private ItemData ink;

        private FieldScreen CreateField(GameSession session, MonsterSpecies enemy)
        {
            ink = TestData.Item("shiny_ink").Set("displayName", "빛나는 잉크");
            var table = ScriptableObject.CreateInstance<EncounterTable>()
                .Set("entries", new List<EncounterTable.Entry> { new EncounterTable.Entry(enemy, 1, 1, 1) })
                .Set("minGroupSize", 1).Set("maxGroupSize", 1);
            var words = ScriptableObject.CreateInstance<WordDatabase>().Set("regionId", "test").Set("regionName", "테스트");
            words.ReplaceWords(TestData.SampleWords());
            var area = ScriptableObject.CreateInstance<FieldArea>()
                .Set("areaId", "test").Set("displayName", "테스트 들판").Set("map", TestMap)
                .Set("encounters", table).Set("words", words)
                .Set("encounterRate", 1f).Set("minStepsBetweenEncounters", 0)
                .Set("chests", new List<ChestContent> { new ChestContent(ink, 2) });

            var go = new GameObject("FieldUnderTest");
            var field = go.AddComponent<FieldScreen>();
            field.Configure(area, session, step: 0.05f, animScale: 0.01f);
            return field;
        }

        private static MonsterSpecies Hero(int hp, int atk)
        {
            var strike = TestData.Skill("strike", SkillKind.Damage, SkillTarget.SingleEnemy, 40);
            return TestData.Species("hero", new MonsterStats(hp, atk, 10), new MonsterStats(0, 0, 0), strike);
        }

        private static MonsterSpecies Enemy(int hp, int atk, int power)
        {
            var bite = TestData.Skill("bite", SkillKind.Damage, SkillTarget.SingleEnemy, power);
            return TestData.Species("enemy", new MonsterStats(hp, atk, 50), new MonsterStats(0, 0, 0), bite);
        }

        [UnityTest]
        public IEnumerator ChestFountainEncounterAndReturnToField()
        {
            var session = GameSession.NewGame(new[] { Hero(100, 30) }, 1);
            var field = CreateField(session, Enemy(40, 1, 5));
            yield return null;
            yield return null;
            Assert.AreEqual(new Vector2Int(2, 1), field.PlayerCell);

            // 1) 위 = 보물상자에 부딪힘 → 빛나는 잉크 2개, 자리는 그대로
            yield return HoldPad(field, "Pad_Up", () => session.Inventory.GetCount(ink) > 0);
            Assert.AreEqual(2, session.Inventory.GetCount(ink));
            StringAssert.Contains("빛나는 잉크", field.ToastMessage);
            Assert.AreEqual(new Vector2Int(2, 1), field.PlayerCell);

            // 2) 왼쪽 = 회복의 샘 → 파티 회복
            session.Party[0].TakeDamage(50);
            yield return HoldPad(field, "Pad_Left", () => session.Party[0].CurrentHp == session.Party[0].Stats.MaxHp);
            Assert.AreEqual(session.Party[0].Stats.MaxHp, session.Party[0].CurrentHp);
            StringAssert.Contains("회복의 샘", field.ToastMessage);

            // 3) 오른쪽 → 위 → 위(풀숲) = 조우
            yield return HoldPad(field, "Pad_Right", () => field.IsMoving);
            yield return HoldPad(field, "Pad_Up", () => field.IsMoving);
            Assert.AreEqual(new Vector2Int(3, 2), field.PlayerCell);
            Assert.IsFalse(field.IsInBattle, "길에서는 조우하지 않음");
            yield return HoldPad(field, "Pad_Up", () => field.IsMoving);
            yield return WaitFor(() => field.Battle.IsRunning);
            Assert.IsTrue(field.Battle.IsRunning, "풀숲에 들어서면 전투");

            // 4) 전투 승리 → '계속 탐험' → 같은 자리로 복귀
            yield return PlayUntilResult(field.Battle, answerCorrectly: true);
            Assert.AreEqual("승리!", field.Battle.ResultTitle);
            StringAssert.Contains("계속 탐험", AllText(FindButton(field.Battle.transform, "ResultButton_Primary")));
            FindButton(field.Battle.transform, "ResultButton_Primary").onClick.Invoke();
            yield return WaitFor(() => !field.IsInBattle);

            Assert.IsFalse(field.IsInBattle);
            Assert.IsFalse(field.Battle.IsRunning);
            Assert.AreEqual(new Vector2Int(3, 3), field.PlayerCell);
            Assert.AreEqual(1, session.Record.BattlesWon);
            Assert.IsTrue(session.World.TryGetPosition("test", out var saved));
            Assert.AreEqual(new Vector2Int(3, 3), saved, "위치가 세이브 상태에 기록됨");
            Assert.IsTrue(session.World.IsChestOpened("test:2,2"));

            // 5) 다시 움직일 수 있다
            yield return HoldPad(field, "Pad_Left", () => field.IsMoving);
            Assert.AreEqual(new Vector2Int(2, 3), field.PlayerCell);

            Object.Destroy(field.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpawnsAtSavedPositionAndDefeatReturnsToFountain()
        {
            var session = GameSession.NewGame(new[] { Hero(10, 1) }, 1);
            session.World.SetPosition("test", new Vector2Int(3, 2));
            var field = CreateField(session, Enemy(500, 50, 500));
            yield return null;
            yield return null;
            Assert.AreEqual(new Vector2Int(3, 2), field.PlayerCell, "저장된 위치에서 시작");

            yield return HoldPad(field, "Pad_Up", () => field.IsMoving);
            yield return WaitFor(() => field.Battle.IsRunning);
            yield return PlayUntilResult(field.Battle, answerCorrectly: false);
            Assert.AreEqual("패배…", field.Battle.ResultTitle);

            FindButton(field.Battle.transform, "ResultButton_Primary").onClick.Invoke();
            yield return WaitFor(() => !field.IsInBattle);

            Assert.AreEqual(new Vector2Int(2, 1), field.PlayerCell, "시작 위치(회복의 샘 앞)로 돌아감");
            Assert.AreEqual(session.Party[0].Stats.MaxHp, session.Party[0].CurrentHp, "파티 회복");
            StringAssert.Contains("돌아왔다", field.ToastMessage);
            Assert.AreEqual(1, session.Record.BattlesLost);

            Object.Destroy(field.gameObject);
            yield return null;
        }
    }
}
