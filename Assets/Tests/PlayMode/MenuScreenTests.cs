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
        //   y=2  #C..#   ← 보물상자 (1,2)
        //   y=1  #.P.#   ← 시작 (2,1)
        private const string Map = "#####\n#C..#\n#.P.#\n#####";

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
            Assert.IsFalse(bag.transform.Find("DetailBox").gameObject.activeSelf, "처음엔 몬스터 탭 (재료 상세는 숨김)");

            // 재료 탭: 잉크(선택) · 표지, 나머지는 빈 칸. 징표는 재료 칸에 안 나옴
            FindButton(bag.transform, "TabMaterials").onClick.Invoke();
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
        public IEnumerator BagMonsterTabShowsStatsSkillsAndEvolution()
        {
            var splash = TestData.Skill("ink_splash", SkillKind.Damage, SkillTarget.AllEnemies, 14, QuizDirection.MeaningToEnglish)
                .Set("displayName", "잉크 뿌리기").Set("description", "적 전체에 잉크를 뿌린다.");
            var shell = TestData.Species("bookshell", new MonsterStats(40, 8, 14), new MonsterStats(6, 1, 3), splash)
                .Set("displayName", "책껍질").Set("description", "책을 등껍질 삼아 지고 다니는 거북.").Set("role", MonsterRole.Defender);
            // 펜촉이(잉크 3개로 진화 가능) + 책껍질(진화 없음, HP 20, 경험치 10) 파티
            var nib = session.Party[0].Species;
            session = GameSession.NewGame(new[] { nib, shell }, 5);
            session.Inventory.Add(ink, 3);
            session.Party[1].TakeDamage(session.Party[1].Stats.MaxHp - 20);
            session.Party[1].GainExp(10);
            var field = MakeField();
            yield return null;
            yield return null;
            var hud = field.transform.Find("FieldHud/SafeArea");
            var bag = hud.Find("InventoryView").gameObject;

            // HUD의 두 번째 파티 배지를 누르면 가방 > 몬스터 탭에서 그 몬스터가 골라진 채로
            FindButton(hud, "Badge_1").onClick.Invoke();
            Assert.IsTrue(bag.activeSelf);
            var detail = bag.transform.Find("MonstersPage/MonsterDetail");
            var text = AllText(detail);
            StringAssert.Contains("책껍질", text);
            StringAssert.Contains("방어형 · Lv5", text);
            StringAssert.Contains("책을 등껍질 삼아", text, "몬스터 설명");
            StringAssert.Contains($"20 / {shell.GetStats(5).MaxHp}", text, "현재 HP / 최대 HP");
            StringAssert.Contains(shell.GetStats(5).Defense.ToString(), text);
            StringAssert.Contains("잉크 뿌리기", text);
            StringAssert.Contains("한→영 · 어려움", text, "기술의 문제 유형");
            StringAssert.Contains("공격 14 · 적 전체", text);
            StringAssert.Contains($"다음 레벨까지 {LevelCurve.ExpToNextLevel(5) - 10}", text);
            StringAssert.Contains("최종 모습이에요", text, "진화형이 없는 몬스터");

            // 첫 번째 카드(펜촉이 Lv5 + 잉크 3개) → 진화 조건
            FindButton(bag.transform, "MonsterTab_0").onClick.Invoke();
            text = AllText(detail);
            StringAssert.Contains("펜촉이", text);
            StringAssert.Contains("진화  깃펜기사", text);
            StringAssert.Contains("지금 진화할 수 있어요", text);
            StringAssert.Contains("poke", text, "기술 이름");
            Assert.IsFalse(FindButton(bag.transform, "MonsterTab_2").gameObject.activeSelf, "파티가 2마리면 세 번째 카드는 숨김");

            FindButton(bag.transform, "InventoryCloseButton").onClick.Invoke();
            Assert.IsFalse(field.IsPanelOpen);
            Object.Destroy(field.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MinimapFollowsPlayerAndOpensFullMap()
        {
            var field = MakeField();
            yield return null;
            yield return null;
            var hud = field.transform.Find("FieldHud/SafeArea");

            // 미니맵은 맵 데이터 크기대로 (한 칸 = 한 점), 내 위치 점이 시작 칸에
            var texture = field.Minimap.Texture;
            Assert.AreEqual(5, texture.width);
            Assert.AreEqual(4, texture.height);
            Assert.AreEqual((Color)MinimapArt.Chest, (Color)texture.GetPixel(1, 2), "보물상자 점");
            var before = field.Minimap.Picture.PlayerAnchor;

            // 위로 한 칸 걸으면 점도 따라 움직인다
            yield return HoldPad(field, "Pad_Up", () => field.IsMoving);
            Assert.AreEqual(new Vector2Int(2, 2), field.PlayerCell);
            Assert.Greater(field.Minimap.Picture.PlayerAnchor.y, before.y);

            // 미니맵을 누르면 큰 지도
            FindButton(hud, "Minimap").onClick.Invoke();
            var map = hud.Find("MapView").gameObject;
            Assert.IsTrue(map.activeSelf);
            Assert.IsTrue(field.IsPanelOpen, "지도가 열려 있는 동안은 걷지 않음");
            var text = AllText(map.transform);
            StringAssert.Contains("지도 · 초원", text);
            StringAssert.Contains("남은 보물상자 1개", text);
            FindButton(map.transform, "MapCloseButton").onClick.Invoke();
            Assert.IsFalse(map.activeSelf);

            Object.Destroy(field.gameObject);
            yield return null;
        }

        // 탐험 안개: 멀리 있는 상자는 처음엔 안 보이다가 가까이 걸어가면 미니맵에 나타난다
        [UnityTest]
        public IEnumerator WalkingRevealsFogOnMinimap()
        {
            //   y=1  #P..........C#   ← 시작 (1,1), 상자 (12,1)
            area.Set("map", "##############\n#P..........C#\n##############");
            var field = MakeField();
            yield return null;
            yield return null;
            var chest = new Vector2Int(12, 1);
            Assert.AreEqual((Color)MinimapArt.Fog, (Color)field.Minimap.Texture.GetPixel(chest.x, chest.y), "처음엔 안개");
            Assert.IsFalse(session.World.IsExplored("meadow", chest));

            yield return HoldPad(field, "Pad_Right", () => field.PlayerCell.x >= 9, 8f);
            Assert.IsTrue(session.World.IsExplored("meadow", chest), "3칸 안으로 다가가면 밝혀짐");
            Assert.AreEqual((Color)MinimapArt.Chest, (Color)field.Minimap.Texture.GetPixel(chest.x, chest.y), "미니맵에 상자가 나타남");

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
