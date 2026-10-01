using NUnit.Framework;
using WordRPG.Items;
using WordRPG.Monsters;

namespace WordRPG.Tests
{
    public class MonsterTests
    {
        private MonsterSpecies species;

        [SetUp]
        public void SetUp()
        {
            species = TestData.Species("nib", new MonsterStats(30, 10, 8), new MonsterStats(4, 3, 1));
        }

        [Test]
        public void StatsGrowWithLevel()
        {
            var stats = new MonsterInstance(species, 5).Stats;

            Assert.AreEqual(30 + 4 * 4, stats.MaxHp);
            Assert.AreEqual(10 + 3 * 4, stats.Attack);
            Assert.AreEqual(8 + 1 * 4, stats.Defense);
        }

        [Test]
        public void GainExpLevelsUpAndCarriesRemainder()
        {
            var monster = new MonsterInstance(species, 1);
            // Lv1→2: 20, Lv2→3: 30
            int gained = monster.GainExp(20 + 30 + 7);

            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, monster.Level);
            Assert.AreEqual(7, monster.Exp);
        }

        [Test]
        public void LevelUpRaisesCurrentHpByMaxHpIncrease()
        {
            var monster = new MonsterInstance(species, 1, currentHp: 10);
            monster.GainExp(LevelCurve.ExpToNextLevel(1));

            Assert.AreEqual(10 + 4, monster.CurrentHp);
        }

        [Test]
        public void ExpStopsAtMaxLevel()
        {
            var monster = new MonsterInstance(species, LevelCurve.MaxLevel - 1);
            monster.GainExp(1_000_000);

            Assert.AreEqual(LevelCurve.MaxLevel, monster.Level);
            Assert.AreEqual(0, monster.Exp);
            Assert.AreEqual(0, monster.GainExp(100));
        }

        [Test]
        public void HealDoesNotReviveFaintedMonster()
        {
            var monster = new MonsterInstance(species, 1);
            monster.TakeDamage(999);

            Assert.IsTrue(monster.IsFainted);
            Assert.AreEqual(0, monster.Heal(50));
            monster.RestoreFully();
            Assert.AreEqual(monster.Stats.MaxHp, monster.CurrentHp);
        }
    }

    public class EvolutionTests
    {
        private ItemData ink;
        private MonsterSpecies baby;
        private MonsterSpecies evolved;
        private Inventory inventory;

        [SetUp]
        public void SetUp()
        {
            ink = TestData.Item("shiny_ink");
            evolved = TestData.Species("quill_knight", new MonsterStats(40, 20, 11), new MonsterStats(5, 4, 2));
            baby = TestData.Species("nib", new MonsterStats(30, 14, 8), new MonsterStats(4, 3, 1))
                .Set("evolvesTo", evolved).Set("evolveLevel", 5).Set("evolveItem", ink).Set("evolveItemCount", 3);
            inventory = new Inventory();
        }

        [Test]
        public void SpeciesWithoutEvolutionCannotEvolve()
        {
            var monster = new MonsterInstance(evolved, 10);
            Assert.AreEqual(EvolutionStatus.NoEvolution, Evolution.Check(monster, inventory));
        }

        [Test]
        public void NeedsLevelAndItems()
        {
            inventory.Add(ink, 3);
            Assert.AreEqual(EvolutionStatus.LevelTooLow, Evolution.Check(new MonsterInstance(baby, 4), inventory));

            var ready = new MonsterInstance(baby, 5);
            inventory.TryRemove(ink, 1);
            Assert.AreEqual(EvolutionStatus.NotEnoughItems, Evolution.Check(ready, inventory));
            Assert.IsFalse(Evolution.TryEvolve(ready, inventory));
            Assert.AreEqual(2, inventory.GetCount(ink), "실패하면 재료를 쓰지 않는다");
        }

        [Test]
        public void EvolvingChangesSpeciesConsumesItemsKeepsLevelAndHeals()
        {
            inventory.Add(ink, 4);
            var monster = new MonsterInstance(baby, 6);
            monster.TakeDamage(10);

            Assert.IsTrue(Evolution.TryEvolve(monster, inventory));

            Assert.AreSame(evolved, monster.Species);
            Assert.AreEqual(6, monster.Level);
            Assert.AreEqual(1, inventory.GetCount(ink));
            Assert.AreEqual(monster.Stats.MaxHp, monster.CurrentHp);
        }
    }

    public class InventoryTests
    {
        [Test]
        public void AddAndRemoveItems()
        {
            var inventory = new Inventory();
            inventory.Add("ink", 2);
            inventory.Add("ink", 3);

            Assert.AreEqual(5, inventory.GetCount("ink"));
            Assert.IsTrue(inventory.TryRemove("ink", 5));
            Assert.AreEqual(0, inventory.GetCount("ink"));
            Assert.AreEqual(0, inventory.Stacks.Count, "0개가 된 칸은 지운다");
        }

        [Test]
        public void RemovingTooManyFailsWithoutChange()
        {
            var inventory = new Inventory();
            inventory.Add("ink", 2);

            Assert.IsFalse(inventory.TryRemove("ink", 3));
            Assert.IsFalse(inventory.TryRemove("missing", 1));
            Assert.AreEqual(2, inventory.GetCount("ink"));
        }

        [Test]
        public void Gold()
        {
            var inventory = new Inventory();
            inventory.AddGold(50);

            Assert.IsFalse(inventory.TrySpendGold(60));
            Assert.IsTrue(inventory.TrySpendGold(20));
            Assert.AreEqual(30, inventory.Gold);
        }
    }
}
