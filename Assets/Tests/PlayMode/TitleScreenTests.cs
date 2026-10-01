using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WordRPG.Game;
using WordRPG.Items;
using WordRPG.Monsters;
using WordRPG.UI;
using static WordRPG.Tests.UiDriver;

namespace WordRPG.Tests
{
    // 타이틀: 저장 없음 → [시작하기], 저장 있음 → [이어하기] + 요약, [처음부터]는 확인 창, 설정에서 저장 지우기
    public class TitleScreenTests
    {
        private string dir;
        private GameDatabase database;
        private MonsterSpecies hero;

        [SetUp]
        public void SetUp()
        {
            dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WordRPG_Title_" + System.Guid.NewGuid().ToString("N"));
            GameManager.SaveDirectoryOverride = dir;
            hero = TestData.Species("hero", new MonsterStats(30, 10, 5), new MonsterStats(3, 2, 1),
                TestData.Skill("poke", SkillKind.Damage, SkillTarget.SingleEnemy, 10)).Set("displayName", "펜촉이");
            database = ScriptableObject.CreateInstance<GameDatabase>();
            database.ReplaceContents(new[] { hero }, new ItemData[0]);
        }

        [TearDown]
        public void TearDown()
        {
            if (GameManager.Instance != null) Object.DestroyImmediate(GameManager.Instance.gameObject);
            GameManager.SaveDirectoryOverride = null;
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
        }

        private GameManager StartManager()
        {
            var managerGo = new GameObject("GameManager");
            managerGo.SetActive(false);
            managerGo.AddComponent<GameManager>().Configure(database, new[] { hero, hero }, 2);
            managerGo.SetActive(true);
            return GameManager.Instance;
        }

        private static TitleScreen MakeTitle(System.Action<bool> onStart)
        {
            var title = new GameObject("TitleUnderTest").AddComponent<TitleScreen>();
            title.Configure(onStart);
            return title;
        }

        [UnityTest]
        public IEnumerator NoSaveShowsStartOnly()
        {
            var manager = StartManager();
            bool? started = null;
            var title = MakeTitle(newGame => started = newGame);
            yield return null;
            yield return null;
            var root = title.transform;

            Assert.IsFalse(manager.HasSave);
            Assert.IsNull(ActiveButton(root, "ContinueButton"), "저장이 없으면 이어하기 없음");
            Assert.AreEqual("시작하기", UiKit.LabelOf(FindButton(root, "NewGameButton")).text);

            FindButton(root, "NewGameButton").onClick.Invoke();
            Assert.AreEqual(true, started, "확인 창 없이 바로 새 게임");
            Assert.IsTrue(manager.HasSave, "시작하면 바로 저장 → 다음에 켜면 이어하기");

            Object.Destroy(title.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SavedGameContinuesOrRestartsAfterConfirm()
        {
            var manager = StartManager();
            manager.MarkPlaying();
            manager.Session.Inventory.AddGold(77);
            manager.Save();

            // 이어하기
            bool? started = null;
            var title = MakeTitle(newGame => started = newGame);
            yield return null;
            yield return null;
            var root = title.transform;
            Assert.IsNotNull(ActiveButton(root, "ContinueButton"));
            Assert.AreEqual("처음부터", UiKit.LabelOf(FindButton(root, "NewGameButton")).text);
            var summary = AllText(root.Find("TitleCanvas/SafeArea/SaveCard"));
            StringAssert.Contains("펜촉이 Lv2 외 1마리", summary);
            StringAssert.Contains("발견한 단어 0", summary);
            FindButton(root, "ContinueButton").onClick.Invoke();
            Assert.AreEqual(false, started);
            Assert.AreEqual(77, manager.Session.Inventory.Gold, "이어하기는 기록 그대로");
            Object.Destroy(title.gameObject);
            yield return null;

            // 처음부터: 확인 창에서 취소 → 그대로, [처음부터] → 지우고 새 게임
            started = null;
            title = MakeTitle(newGame => started = newGame);
            yield return null;
            yield return null;
            root = title.transform;
            var dialog = root.Find("TitleCanvas/SafeArea/ConfirmDialog").gameObject;
            FindButton(root, "NewGameButton").onClick.Invoke();
            Assert.IsTrue(dialog.activeSelf);
            Assert.IsNull(started, "확인 전에는 시작하지 않음");
            FindButton(dialog.transform, "DialogCancelButton").onClick.Invoke();
            Assert.AreEqual(77, manager.Session.Inventory.Gold);

            FindButton(root, "NewGameButton").onClick.Invoke();
            FindButton(dialog.transform, "DialogConfirmButton").onClick.Invoke();
            Assert.AreEqual(true, started);
            Assert.AreEqual(0, manager.Session.Inventory.Gold, "새 게임");
            Assert.IsTrue(manager.HasSave);

            Object.Destroy(title.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeletingSaveInSettingsShowsStartAgain()
        {
            var manager = StartManager();
            manager.MarkPlaying();
            manager.Save();

            var title = MakeTitle(_ => { });
            yield return null;
            yield return null;
            var root = title.transform;
            Assert.IsNotNull(ActiveButton(root, "ContinueButton"));

            FindButton(root, "SettingsButton").onClick.Invoke();
            Assert.IsTrue(title.IsSettingsOpen);
            var settings = root.Find("TitleCanvas/SafeArea/SettingsView");
            FindButton(settings, "DeleteSaveButton").onClick.Invoke();
            FindButton(settings.Find("ConfirmDialog"), "DialogConfirmButton").onClick.Invoke();

            Assert.IsFalse(manager.HasSave, "저장 파일 삭제");
            Assert.IsFalse(title.IsSettingsOpen);
            Assert.IsNull(ActiveButton(root, "ContinueButton"));
            Assert.AreEqual("시작하기", UiKit.LabelOf(FindButton(root, "NewGameButton")).text);

            // 지운 뒤 그냥 꺼도 빈 세이브를 만들지 않음 (시작하기 전에는 저장 안 함)
            manager.Save();
            Assert.IsFalse(manager.HasSave);

            Object.Destroy(title.gameObject);
            yield return null;
        }
    }
}
