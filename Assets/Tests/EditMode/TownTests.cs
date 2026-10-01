using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WordRPG.Field;
using WordRPG.Game;
using WordRPG.Items;
using WordRPG.Monsters;
using WordRPG.UI;

namespace WordRPG.Tests
{
    public class ShopTests
    {
        private ItemData ink;

        [SetUp]
        public void SetUp() => ink = TestData.Item("shiny_ink");

        [Test]
        public void BuyingSpendsGoldAndAddsItem()
        {
            var inventory = new Inventory();
            inventory.AddGold(100);

            var result = Shop.TryBuy(inventory, new ShopEntry(ink, 60));

            Assert.AreEqual(PurchaseResult.Bought, result);
            Assert.AreEqual(40, inventory.Gold);
            Assert.AreEqual(1, inventory.GetCount(ink));
        }

        [Test]
        public void NotEnoughGoldChangesNothing()
        {
            var inventory = new Inventory();
            inventory.AddGold(59);

            Assert.AreEqual(PurchaseResult.NotEnoughGold, Shop.TryBuy(inventory, new ShopEntry(ink, 60)));
            Assert.AreEqual(59, inventory.Gold);
            Assert.AreEqual(0, inventory.GetCount(ink));
        }

        [Test]
        public void BrokenEntriesAreNotForSale()
        {
            var inventory = new Inventory();
            inventory.AddGold(1000);

            Assert.AreEqual(PurchaseResult.NotForSale, Shop.TryBuy(inventory, new ShopEntry(null, 10)));
            Assert.AreEqual(PurchaseResult.NotForSale, Shop.TryBuy(inventory, new ShopEntry(ink, 0)));
            Assert.AreEqual(PurchaseResult.NotForSale, Shop.TryBuy(inventory, null));
            Assert.AreEqual(1000, inventory.Gold);
        }
    }

    public class EvolutionPreviewTests
    {
        [Test]
        public void NewSkillsListsOnlyWhatWasNotKnown()
        {
            var poke = TestData.Skill("poke", SkillKind.Damage, SkillTarget.SingleEnemy, 20);
            var splash = TestData.Skill("splash", SkillKind.Damage, SkillTarget.AllEnemies, 14);
            var storm = TestData.Skill("storm", SkillKind.Damage, SkillTarget.AllEnemies, 22);
            var baby = TestData.Species("nib", new MonsterStats(1, 1, 1), new MonsterStats(0, 0, 0), poke, splash);
            var adult = TestData.Species("knight", new MonsterStats(1, 1, 1), new MonsterStats(0, 0, 0), poke, storm);

            CollectionAssert.AreEqual(new[] { storm }, Evolution.NewSkills(baby, adult));
            Assert.AreEqual(0, Evolution.NewSkills(adult, adult).Count);
            Assert.AreEqual(0, Evolution.NewSkills(null, adult).Count);
        }
    }

    public class JosaTests
    {
        [TestCase("펜촉이", "이", "가", "펜촉이가")]
        [TestCase("책껍질", "이", "가", "책껍질이")]
        [TestCase("깃펜기사", "으로", "로", "깃펜기사로")]
        [TestCase("백과거북", "으로", "로", "백과거북으로")]
        [TestCase("지혜등불", "으로", "로", "지혜등불로")] // ㄹ받침은 '로'
        [TestCase("빛나는 잉크", "을", "를", "빛나는 잉크를")]
        [TestCase("단단한 표지", "을", "를", "단단한 표지를")]
        [TestCase("Slime", "이", "가", "Slime가")]
        public void PicksParticleByFinalConsonant(string word, string withBatchim, string without, string expected)
        {
            Assert.AreEqual(expected, UiKit.WithJosa(word, withBatchim, without));
        }
    }

    // 실제 프로젝트의 마을 데이터 점검
    public class TownDataTests
    {
        [Test]
        public void AreasWithShopTileHaveAValidShop()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");
            var areas = AssetDatabase.FindAssets("t:FieldArea", new[] { "Assets/Data" })
                .Select(g => AssetDatabase.LoadAssetAtPath<FieldArea>(AssetDatabase.GUIDToAssetPath(g))).ToList();

            foreach (var area in areas)
            {
                var map = area.Map;
                bool hasShopTile = false;
                for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                    hasShopTile |= map.Get(new Vector2Int(x, y)) == FieldTile.Shop;
                if (!hasShopTile) continue;

                Assert.IsNotNull(area.Shop, $"{area.name}: 맵에 상점(S)이 있는데 상점 데이터가 연결되지 않음");
                Assert.Greater(area.Shop.Entries.Count, 0, $"{area.Shop.name}: 파는 물건이 없음");
                foreach (var entry in area.Shop.Entries)
                {
                    Assert.IsNotNull(entry.Item, $"{area.Shop.name}: 비어 있는 상품");
                    Assert.Greater(entry.Price, 0, $"{area.Shop.name}: {entry.Item.name} 가격이 0 이하");
                    Assert.AreSame(entry.Item, database.FindItem(entry.Item.ItemId), $"{entry.Item.name}이 GameDatabase에 없음");
                }
            }
        }

        [Test]
        public void EveryStarterCanEvolveWithItemsThatAreObtainable()
        {
            // 시작 몬스터의 진화 재료는 상점·보물상자·적 드롭 중 하나 이상에서 얻을 수 있어야 함
            var monsters = AssetDatabase.FindAssets("t:MonsterSpecies", new[] { "Assets/Data" })
                .Select(g => AssetDatabase.LoadAssetAtPath<MonsterSpecies>(AssetDatabase.GUIDToAssetPath(g))).ToList();
            var areas = AssetDatabase.FindAssets("t:FieldArea", new[] { "Assets/Data" })
                .Select(g => AssetDatabase.LoadAssetAtPath<FieldArea>(AssetDatabase.GUIDToAssetPath(g))).ToList();

            var obtainable = new HashSet<ItemData>();
            foreach (var m in monsters)
            foreach (var drop in m.Drops)
                if (drop.Item != null && drop.Chance > 0) obtainable.Add(drop.Item);
            foreach (var area in areas)
            {
                foreach (var chest in area.ChestContents)
                    if (chest.Item != null) obtainable.Add(chest.Item);
                if (area.Shop != null)
                    foreach (var entry in area.Shop.Entries)
                        if (entry.Item != null) obtainable.Add(entry.Item);
            }

            foreach (var species in monsters.Where(m => m.EvolvesTo != null && m.EvolveItem != null))
                Assert.IsTrue(obtainable.Contains(species.EvolveItem),
                    $"{species.name}의 진화 재료 {species.EvolveItem.name}을 얻을 방법이 없음");
        }
    }
}
