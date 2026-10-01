using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WordRPG.Field;
using WordRPG.Game;
using WordRPG.Items;
using WordRPG.Monsters;
using WordRPG.UI;
using WordRPG.Words;
using static WordRPG.Tests.UiDriver;

namespace WordRPG.Tests
{
    // 필드 오른쪽 메뉴: 가방(소지품) · 설정(소리·진동·저장 데이터 지우기) — 실제 버튼을 눌러 확인
    public class MenuScreenTests
    {
        private const string Map = "#####\n#...#\n#.P.#\n#####";

        private ItemData ink, cover, keepsake;
        private FieldArea area;
        private GameDatabase database;
        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            ink = TestData.Item("shiny_ink").Set("displayName", "빛나는 잉크").Set("description", "펜촉이를 진화시키는 잉크.");
            cover = TestData.Item("hard_cover").Set("displayName", "단단한 표지");
            keepsake = TestData.Item("keepsake_meadow").Set("displayName", "네잎클로버 책갈피").Set("kind", ItemKind.Keepsake);
            var poke = TestData.Skill("poke", SkillKind.Damage, SkillTarget.SingleEnemy, 20);
            var knight = TestData.Species("quill_knight", new MonsterStats(40, 20, 11), new MonsterStats(5, 4, 2), poke)
                .Set("displayName", "깃펜기사");
            var nib = TestData.Species("nib", new MonsterStats(30, 14, 8), new MonsterStats(4, 3, 1), poke)
                .Set("displayName", "펜촉이")
                .Set("evolvesTo", knight).Set("evolveLevel", 5).Set("evolveItem", ink).Set("evolveItemCount", 3);

            var words = ScriptableObject.CreateInstance<WordDatabase>()
                .Set("regionId", "meadow").Set("regionName", "초원").Set("completionKeepsake", keepsake).Set("completionGold", 500);
            words.ReplaceWords(TestData.SampleWords());
            var table = ScriptableObject.CreateInstance<EncounterTable>()
                .Set("entries", new List<EncounterTable.Entry> { new EncounterTable.Entry(nib, 1, 1, 1) });
            area = ScriptableObject.CreateInstance<FieldArea>()
                .Set("areaId", "meadow").Set("displayName", "초원").Set("map", Map)
                .Set("encounters", table).Set("words", words);
            database = ScriptableObject.CreateInstance<GameDatabase>();
            database.ReplaceContents(new[] { nib, knight }, new[] { ink, cover, keepsake }, new[] { area });

            session = GameSession.NewGame(new[] { nib }, 5);
            session.Inventory.Add(ink, 3);
            session.Inventory.Add(cover, 1);
            session.Inventory.Add(keepsake);
            session.Record.MarkRegionClaimed("meadow");
            session.Inventory.AddGold(90);
        }

        private FieldScreen MakeField(GameSettings settings = null, System.Action changed = null, System.Action delete = null)
        {
            var go = new GameObject("MenuUnderTest");
            var field = go.AddComponent<FieldScreen>();
            field.Configure(area, session, step: 0.05f, animScale: 0.01f, gameDatabase: database,
                gameSettings: settings ?? new GameSettings(), onSettingsChanged: changed, onDeleteSave: delete);
            return field;
        }

        [UnityTest]
        public IEnumerator BagShowsMaterialsAndKeepsakeShelf()
        {
            var field = MakeField();
            yield return null;
            yield return null;
            var hud = field.transform.Find("FieldHud/SafeArea");
            Assert.AreEqual(Music.Meadow, Sound.CurrentMusic, "초원에 들어가면 초원 음악");

            FindButton(hud, "BagButton").onClick.Invoke();
            var bag = hud.Find("InventoryView").gameObject;
            Assert.IsTrue(bag.activeSelf, "가방 버튼 → 소지품");
            Assert.IsTrue(field.IsPanelOpen, "소지품이 열려 있는 동안은 걷지 않음");

            // 재료 탭: 잉크(선택) · 표지, 나머지는 빈 칸. 징표는 재료 칸에 안 나옴
            var text = AllText(bag.transform);
            StringAssert.Contains("보유 골드  90G", text);
            StringAssert.Contains("× 3", text);
            StringAssert.Contains("× 1", text);
            StringAssert.Contains("빛나는 잉크", text);
            StringAssert.Contains("펜촉이 → 깃펜기사", text);
            StringAssert.Contains("지금 진화할 수 있어요", text);
            Assert.IsFalse(FindButton(bag.transform, "ItemSlot_2").interactable, "세 번째 칸은 빈 칸");

            FindButton(bag.transform, "ItemSlot_1").onClick.Invoke();
            StringAssert.Contains("단단한 표지", AllText(bag.transform.Find("DetailBox")));
            StringAssert.Contains("진화하는 몬스터가 없어요", AllText(bag.transform.Find("DetailBox")));

            // 다른 메뉴는 겹쳐 열리지 않음
            FindButton(hud, "SettingsButton").onClick.Invoke();
            Assert.IsFalse(hud.Find("SettingsView").gameObject.activeSelf);

            // 징표 탭: 초원 징표 획득 + 다음 지역 자리
            FindButton(bag.transform, "TabKeepsakes").onClick.Invoke();
            var shelfText = AllText(bag.transform.Find("KeepsakesPage"));
            StringAssert.Contains("네잎클로버 책갈피", shelfText);
            StringAssert.Contains("다음 지역", shelfText);
            StringAssert.Contains("500 골드", AllText(bag.transform.Find("DetailBox")));

            FindButton(bag.transform, "InventoryCloseButton").onClick.Invoke();
            Assert.IsFalse(bag.activeSelf);
            Assert.IsFalse(field.IsPanelOpen);

            Object.Destroy(field.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SettingsSaveChangesAndDeleteNeedsConfirm()
        {
            var settings = new GameSettings();
            int saved = 0, deleted = 0;
            var field = MakeField(settings, () => saved++, () => deleted++);
            yield return null;
            yield return null;
            var hud = field.transform.Find("FieldHud/SafeArea");

            FindButton(hud, "SettingsButton").onClick.Invoke();
            var view = hud.Find("SettingsView").gameObject;
            Assert.IsTrue(view.activeSelf, "설정 버튼 → 설정");
            Assert.AreEqual(0, saved, "열기만 해서는 저장하지 않음");

            FindButton(view.transform, "VibrationSwitch").onClick.Invoke();
            Assert.IsFalse(settings.Vibration);
            Assert.AreEqual(1, saved);

            var music = view.GetComponentsInChildren<Slider>(true).First(s => s.name == "MusicSlider");
            Assert.AreEqual(GameSettings.DefaultMusicVolume, music.value, 0.001f, "현재 값으로 열림");
            music.value = 0.2f;
            Assert.AreEqual(0.2f, settings.MusicVolume, 0.001f);
            Assert.AreEqual(2, saved);

            // 저장 데이터 지우기: 확인 창에서 취소하면 아무 일도 없음
            var dialog = view.transform.Find("ConfirmDialog").gameObject;
            FindButton(view.transform, "DeleteSaveButton").onClick.Invoke();
            Assert.IsTrue(dialog.activeSelf);
            StringAssert.Contains("되돌릴 수 없어요", AllText(dialog.transform));
            FindButton(dialog.transform, "DialogCancelButton").onClick.Invoke();
            Assert.IsFalse(dialog.activeSelf);
            Assert.AreEqual(0, deleted);

            // [지우기]를 눌러야 지운다
            FindButton(view.transform, "DeleteSaveButton").onClick.Invoke();
            FindButton(dialog.transform, "DialogConfirmButton").onClick.Invoke();
            Assert.AreEqual(1, deleted);
            Assert.IsFalse(view.activeSelf, "지운 뒤 설정 창은 닫힘");

            Object.Destroy(field.gameObject);
            yield return null;
        }
    }
}
