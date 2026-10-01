using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WordRPG.Battle;
using WordRPG.Field;
using WordRPG.Game;
using WordRPG.Monsters;
using WordRPG.UI;
using WordRPG.Words;
using static WordRPG.Tests.UiDriver;

namespace WordRPG.Tests
{
    // 실제 UI 버튼의 onClick을 눌러서 전투 한 판이 끝까지 진행되는지 확인하는 통합 테스트
    public class BattleScreenTests
    {
        private static T Set<T>(T obj, string field, object value) where T : class
        {
            var info = obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            info.SetValue(obj, value);
            return obj;
        }

        private static EncounterTable Table(MonsterSpecies enemy)
        {
            var table = ScriptableObject.CreateInstance<EncounterTable>();
            Set(table, "entries", new List<EncounterTable.Entry> { new EncounterTable.Entry(enemy, 1, 1, 1) });
            Set(table, "minGroupSize", 1);
            Set(table, "maxGroupSize", 1);
            return table;
        }

        private static WordDatabase Words()
        {
            var db = ScriptableObject.CreateInstance<WordDatabase>();
            db.ReplaceWords(TestData.SampleWords());
            return db;
        }

        private BattleScreen CreateScreen(MonsterSpecies hero, MonsterSpecies enemy, int partySize)
        {
            var species = new MonsterSpecies[partySize];
            for (int i = 0; i < partySize; i++) species[i] = hero;

            var go = new GameObject("BattleScreenUnderTest");
            var screen = go.AddComponent<BattleScreen>();
            screen.Configure(Table(enemy), Words(), GameSession.NewGame(species, 1), animScale: 0.01f);
            return screen;
        }

        [UnityTest]
        public IEnumerator WinningABattleThroughTheUi()
        {
            LogAssert.ignoreFailingMessages = false;
            var strike = TestData.Skill("strike", SkillKind.Damage, SkillTarget.SingleEnemy, 40);
            var hero = TestData.Species("hero", new MonsterStats(100, 30, 10), new MonsterStats(0, 0, 0), strike);
            var enemy = TestData.Species("enemy", new MonsterStats(40, 1, 50), new MonsterStats(0, 0, 0), strike)
                .Set("expReward", 20).Set("goldReward", 7);
            var screen = CreateScreen(hero, enemy, partySize: 2);

            yield return null;
            yield return null;
            yield return PlayUntilResult(screen, answerCorrectly: true);

            Assert.IsTrue(screen.IsResultVisible, "결과 패널이 떠야 함");
            Assert.AreEqual("승리!", screen.ResultTitle);
            Assert.AreEqual(BattlePhase.Victory, screen.Engine.Phase);
            Assert.Greater(screen.Engine.CorrectAnswers, 0);
            Assert.Greater(screen.Party[0].Exp + screen.Party[0].Level, 1, "경험치가 지급돼야 함");
            Assert.Greater(screen.Vocabulary.Entries.Count, 0, "맞힌 단어가 학습 기록에 남아야 함");

            // 결과 버튼을 누르면 다음 전투가 시작된다
            var next = ActiveButton(screen.transform, "ResultButton_Primary");
            Assert.IsNotNull(next);
            var firstEngine = screen.Engine;
            next.onClick.Invoke();
            for (int i = 0; i < 30 && screen.Engine == firstEngine; i++) yield return null;
            Assert.AreNotSame(firstEngine, screen.Engine);
            Assert.IsFalse(screen.IsResultVisible);

            Object.Destroy(screen.gameObject);
        }

        [UnityTest]
        public IEnumerator LosingABattleThroughTheUi()
        {
            var weak = TestData.Skill("weak", SkillKind.Damage, SkillTarget.SingleEnemy, 1);
            var smash = TestData.Skill("smash", SkillKind.Damage, SkillTarget.SingleEnemy, 500);
            var hero = TestData.Species("hero", new MonsterStats(10, 5, 1), new MonsterStats(0, 0, 0), weak);
            var enemy = TestData.Species("enemy", new MonsterStats(500, 50, 50), new MonsterStats(0, 0, 0), smash);
            var screen = CreateScreen(hero, enemy, partySize: 1);

            yield return null;
            yield return null;
            yield return PlayUntilResult(screen, answerCorrectly: false);

            Assert.IsTrue(screen.IsResultVisible);
            Assert.AreEqual("패배…", screen.ResultTitle);
            Assert.Greater(screen.Engine.WrongAnswers, 0);
            var wrongWord = screen.Vocabulary.Entries[0];
            Assert.IsTrue(wrongWord.InWrongNote, "틀린 단어는 오답 노트에 있어야 함");

            // 재도전하면 파티가 회복된다
            ActiveButton(screen.transform, "ResultButton_Primary").onClick.Invoke();
            for (int i = 0; i < 30 && screen.IsResultVisible; i++) yield return null;
            Assert.AreEqual(screen.Party[0].Stats.MaxHp, screen.Party[0].CurrentHp);

            Object.Destroy(screen.gameObject);
        }

        // 실제 흐름: GameManager가 있는 상태로 전투 → 자동 저장 → 앱 재시작(GameManager 새로 생성) → 이어하기
        [UnityTest]
        public IEnumerator ProgressIsSavedAndRestoredAfterRestart()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WordRPG_PlaySave_" + System.Guid.NewGuid().ToString("N"));
            GameManager.SaveDirectoryOverride = dir;
            try
            {
                var strike = TestData.Skill("strike", SkillKind.Damage, SkillTarget.SingleEnemy, 40);
                var hero = TestData.Species("hero", new MonsterStats(100, 30, 10), new MonsterStats(5, 1, 1), strike);
                var enemy = TestData.Species("enemy", new MonsterStats(40, 1, 50), new MonsterStats(0, 0, 0), strike)
                    .Set("expReward", 20).Set("goldReward", 7);
                var database = ScriptableObject.CreateInstance<GameDatabase>();
                database.ReplaceContents(new[] { hero, enemy }, new Items.ItemData[0]);

                GameManager StartManager()
                {
                    var managerGo = new GameObject("GameManager");
                    managerGo.SetActive(false);
                    managerGo.AddComponent<GameManager>().Configure(database, new[] { hero, hero }, 1);
                    managerGo.SetActive(true); // 여기서 Awake → 세이브 읽기
                    return GameManager.Instance;
                }

                // 1회차: 새 게임 → 전투 승리
                var manager = StartManager();
                Assert.IsFalse(manager.LoadedFromSave);
                var screenGo = new GameObject("BattleScreen");
                var screen = screenGo.AddComponent<BattleScreen>();
                screen.Configure(Table(enemy), Words(), animScale: 0.01f); // 세션은 GameManager 것을 사용
                yield return null;
                yield return null;
                Assert.AreSame(manager.Session, screen.Session);

                yield return PlayUntilResult(screen, answerCorrectly: true);
                Assert.AreEqual("승리!", screen.ResultTitle);
                Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(dir, "save.json")), "자동 저장 파일");

                int discovered = manager.Session.Vocabulary.DiscoveredCount;
                int exp = manager.Session.Party[0].Exp;
                int level = manager.Session.Party[0].Level;
                int gold = manager.Session.Inventory.Gold;
                Assert.Greater(discovered, 0);
                Assert.Greater(gold, 0);

                // 앱 종료
                Object.Destroy(screenGo);
                Object.Destroy(manager.gameObject);
                yield return null;
                Assert.IsNull(GameManager.Instance);

                // 2회차: 다시 켜면 이어하기
                var restarted = StartManager();
                Assert.IsTrue(restarted.LoadedFromSave);
                StringAssert.Contains("이어하기", restarted.StatusMessage);
                Assert.AreEqual(discovered, restarted.Session.Vocabulary.DiscoveredCount);
                Assert.AreEqual(level, restarted.Session.Party[0].Level);
                Assert.AreEqual(exp, restarted.Session.Party[0].Exp);
                Assert.AreEqual(gold, restarted.Session.Inventory.Gold);
                Assert.AreEqual(1, restarted.Session.Record.BattlesWon);

                Object.Destroy(restarted.gameObject);
                yield return null;
            }
            finally
            {
                GameManager.SaveDirectoryOverride = null;
                if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            }
        }

        // 도감 화면: 미발견은 ???, 목록 개수, 닫기. 스킬 선택 중에만 열린다
        [UnityTest]
        public IEnumerator DexShowsUndiscoveredWordsAsHidden()
        {
            var strike = TestData.Skill("strike", SkillKind.Damage, SkillTarget.SingleEnemy, 40);
            var hero = TestData.Species("hero", new MonsterStats(100, 30, 10), new MonsterStats(0, 0, 0), strike);
            var enemy = TestData.Species("enemy", new MonsterStats(40, 1, 50), new MonsterStats(0, 0, 0), strike);
            var screen = CreateScreen(hero, enemy, partySize: 1);
            yield return null;
            yield return null;

            var open = FindButton(screen.transform, "DexButton");
            Assert.IsTrue(open.interactable, "스킬 선택 중에는 도감을 열 수 있다");
            open.onClick.Invoke();
            yield return null;

            int rowCount = 0;
            foreach (var button in screen.GetComponentsInChildren<Button>(true))
            {
                if (button.name.StartsWith("DexRow_")) rowCount++;
            }
            Assert.AreEqual(TestData.SampleWords().Count, rowCount);

            var firstRow = FindButton(screen.transform, "DexRow_0");
            StringAssert.Contains("???", AllText(firstRow));
            firstRow.onClick.Invoke();
            StringAssert.Contains("아직 발견하지 못한", AllText(screen.transform.Find("BattleCanvas/SafeArea/DexView")));

            FindButton(screen.transform, "DexCloseButton").onClick.Invoke();
            Assert.IsFalse(screen.transform.Find("BattleCanvas/SafeArea/DexView").gameObject.activeSelf);

            // 문제를 푸는 중에는 도감 버튼이 잠긴다
            ActiveButton(screen.transform, "SkillButton_0").onClick.Invoke();
            yield return null;
            Assert.IsFalse(FindButton(screen.transform, "DexButton").interactable);

            Object.Destroy(screen.gameObject);
        }

        // 단어장을 전부 발견하면 징표와 골드가 지급되고 결과 화면에 표시, 도감에서도 완료 표시
        [UnityTest]
        public IEnumerator CompletingDexGivesKeepsakeAndGold()
        {
            var strike = TestData.Skill("strike", SkillKind.Damage, SkillTarget.SingleEnemy, 40);
            var hero = TestData.Species("hero", new MonsterStats(100, 30, 10), new MonsterStats(0, 0, 0), strike);
            var enemy = TestData.Species("enemy", new MonsterStats(40, 1, 50), new MonsterStats(0, 0, 0), strike)
                .Set("goldReward", 0);
            var keepsake = TestData.Item("keepsake_test").Set("displayName", "시험 징표");
            var oneWord = ScriptableObject.CreateInstance<WordDatabase>()
                .Set("regionId", "test").Set("regionName", "시험 지역")
                .Set("completionKeepsake", keepsake).Set("completionGold", 777);
            oneWord.ReplaceWords(new List<WordEntry> { new WordEntry("", "abandon", "버리다, 포기하다", "v") });

            var session = GameSession.NewGame(new[] { hero }, 1);
            var go = new GameObject("DexCompletionTest");
            var screen = go.AddComponent<BattleScreen>();
            screen.Configure(Table(enemy), oneWord, session, animScale: 0.01f);
            yield return null;
            yield return null;
            yield return PlayUntilResult(screen, answerCorrectly: true);

            Assert.IsTrue(screen.IsResultVisible);
            Assert.AreEqual(777, session.Inventory.Gold);
            Assert.AreEqual(1, session.Inventory.GetCount(keepsake));
            var resultText = AllText(screen.transform.Find("BattleCanvas/SafeArea/Bottom/ResultPanel"));
            StringAssert.Contains("도감 완성", resultText);
            StringAssert.Contains("시험 징표", resultText);

            // 결과 화면에서 도감을 열면 '획득 완료'
            FindButton(screen.transform, "DexButton").onClick.Invoke();
            yield return null;
            StringAssert.Contains("획득 완료", AllText(screen.transform.Find("BattleCanvas/SafeArea/DexView")));
            Assert.AreEqual(777, session.Inventory.Gold, "도감을 열어도 중복 지급 없음");

            Object.Destroy(go);
        }
    }
}
