using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    // 마을: 상점 앞에서 [확인] → 재료 구매 → 진화의 제단 앞에서 [확인] → 진화 (실제 패드·버튼 사용)
    public class TownScreenTests
    {
        //   y=3  #####
        //   y=2  #ES.#   ← 진화의 제단 (1,2), 상점 (2,2)
        //   y=1  #.P.#   ← 시작 (2,1)
        //   y=0  #####
        private const string TownMap = "#####\n#ES.#\n#.P.#\n#####";

        [UnityTest]
        public IEnumerator BuyMaterialThenEvolve()
        {
            var ink = TestData.Item("shiny_ink").Set("displayName", "빛나는 잉크");
            var poke = TestData.Skill("poke", SkillKind.Damage, SkillTarget.SingleEnemy, 20);
            var storm = TestData.Skill("storm", SkillKind.Damage, SkillTarget.AllEnemies, 22).Set("displayName", "잉크 폭풍");
            var knight = TestData.Species("quill_knight", new MonsterStats(40, 20, 11), new MonsterStats(5, 4, 2), poke, storm)
                .Set("displayName", "깃펜기사");
            var nib = TestData.Species("nib", new MonsterStats(30, 14, 8), new MonsterStats(4, 3, 1), poke)
                .Set("displayName", "펜촉이")
                .Set("evolvesTo", knight).Set("evolveLevel", 5).Set("evolveItem", ink).Set("evolveItemCount", 3);
            var shop = ScriptableObject.CreateInstance<ShopData>()
                .Set("displayName", "테스트 상점")
                .Set("entries", new List<ShopEntry> { new ShopEntry(ink, 60) });
            var words = ScriptableObject.CreateInstance<WordDatabase>();
            words.ReplaceWords(TestData.SampleWords());
            var table = ScriptableObject.CreateInstance<EncounterTable>()
                .Set("entries", new List<EncounterTable.Entry> { new EncounterTable.Entry(nib, 1, 1, 1) });
            var area = ScriptableObject.CreateInstance<FieldArea>()
                .Set("areaId", "town").Set("displayName", "마을").Set("map", TownMap)
                .Set("encounters", table).Set("words", words).Set("shop", shop);

            var session = GameSession.NewGame(new[] { nib }, 5);
            session.Inventory.Add(ink, 2);
            session.Inventory.AddGold(100);
            int saves = 0;

            var go = new GameObject("TownUnderTest");
            var field = go.AddComponent<FieldScreen>();
            field.Configure(area, session, onSave: () => saves++, step: 0.05f, animScale: 0.01f);
            yield return null;
            yield return null;
            var shopView = go.transform.Find("FieldHud/SafeArea/ShopView").gameObject;
            var evolutionView = go.transform.Find("FieldHud/SafeArea/EvolutionView").gameObject;

            // 가까이 있는 제단·상점 위에 이름표 (상점은 상점 이름)
            CollectionAssert.AreEquivalent(new[] { "진화의 제단", "테스트 상점" }, field.VisibleNameTags.ToList());

            // 1) 아래(벽)를 보고 있어도 [확인]은 옆 칸의 상점을 찾아 연다
            Assert.AreEqual(Direction.Down, field.Facing);
            Assert.IsTrue(field.ConfirmReady);
            yield return PressConfirm(field);
            yield return WaitFor(() => shopView.activeSelf, 2f);
            Assert.IsTrue(shopView.activeSelf, "상점 앞에서 [확인] → 상점 화면");
            Assert.AreEqual(Direction.Up, field.Facing, "상점 쪽으로 돌아봄");
            StringAssert.Contains("테스트 상점", AllText(shopView.transform));

            // 상점이 열려 있는 동안은 움직이지 않음
            yield return HoldPad(field, "Pad_Left", () => false, maxSeconds: 0.3f);
            Assert.AreEqual(new Vector2Int(2, 1), field.PlayerCell);

            // 2) 구매: 100G → 40G, 잉크 3개. 이제 60G가 안 되므로 버튼 잠김
            FindButton(shopView.transform, "BuyButton_0").onClick.Invoke();
            Assert.AreEqual(40, session.Inventory.Gold);
            Assert.AreEqual(3, session.Inventory.GetCount(ink));
            Assert.IsFalse(FindButton(shopView.transform, "BuyButton_0").interactable);
            StringAssert.Contains("빛나는 잉크를 샀다", AllText(shopView.transform));
            Assert.Greater(saves, 0, "구매하면 저장");
            FindButton(shopView.transform, "ShopCloseButton").onClick.Invoke();
            Assert.IsFalse(shopView.activeSelf);

            // 3) 왼쪽으로 한 칸 → 위 = 진화의 제단. 부딪히기만 하면 안 열리고 [확인]으로 열림
            yield return WaitFor(() => !field.IsPanelOpen);
            yield return new WaitForSecondsRealtime(0.6f); // 창을 닫은 직후 대기 시간
            yield return HoldPad(field, "Pad_Left", () => field.IsMoving);
            Assert.AreEqual(new Vector2Int(1, 1), field.PlayerCell);
            yield return FacePad(field, "Pad_Up", Direction.Up);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsFalse(evolutionView.activeSelf, "부딪히기만 해서는 안 열림");
            yield return PressConfirm(field);
            yield return WaitFor(() => evolutionView.activeSelf, 2f);
            Assert.IsTrue(evolutionView.activeSelf, "제단 앞에서 [확인] → 진화 화면");

            // 4) 진화는 두 번 눌러야 함
            var evolve = FindButton(evolutionView.transform, "EvolveButton_0");
            Assert.IsTrue(evolve.interactable, "Lv5 + 잉크 3개 → 진화 가능");
            evolve.onClick.Invoke();
            Assert.AreSame(nib, session.Party[0].Species, "첫 번째 누름은 확인만");
            StringAssert.Contains("되돌릴 수 없어요", AllText(evolutionView.transform));
            evolve.onClick.Invoke();

            Assert.AreSame(knight, session.Party[0].Species);
            Assert.AreEqual(5, session.Party[0].Level, "레벨 유지");
            Assert.AreEqual(0, session.Inventory.GetCount(ink), "재료 3개 사용");
            var text = AllText(evolutionView.transform);
            StringAssert.Contains("펜촉이가 깃펜기사로 진화했다", text);
            StringAssert.Contains("잉크 폭풍", text);
            Assert.IsFalse(evolve.interactable, "최종 형태");

            // 진화 연출: 단어가 반짝 → 글자 고리 → 진화 성공! (능력치 변화 + 새 기술) → [좋아요!]로 닫힘
            var cutscene = evolutionView.transform.Find("EvolutionCutscene").gameObject;
            Assert.IsTrue(cutscene.activeSelf, "진화하면 연출 시작");
            yield return WaitFor(() => ActiveButton(cutscene.transform, "EvolutionOkButton") != null, 5f);
            var scene = AllText(cutscene.transform);
            StringAssert.Contains("진화 성공", scene);
            StringAssert.DoesNotContain("어라", scene, "다른 게임 대사를 쓰지 않음");
            StringAssert.Contains("잉크 폭풍", scene);
            StringAssert.Contains("Lv5", scene);
            FindButton(cutscene.transform, "EvolutionOkButton").onClick.Invoke();
            Assert.IsFalse(cutscene.activeSelf);
            Assert.IsTrue(evolutionView.activeSelf, "연출이 끝나면 제단 화면으로");

            FindButton(evolutionView.transform, "EvolutionCloseButton").onClick.Invoke();
            yield return null;
            StringAssert.Contains("깃펜기사", AllText(go.transform.Find("FieldHud/SafeArea/PartyStrip")), "HUD에 진화한 이름");

            Object.Destroy(go);
            yield return null;
        }
    }
}
