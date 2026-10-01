using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WordRPG.Field;
using WordRPG.Monsters;

namespace WordRPG.Tests
{
    public class EncounterTableTests
    {
        [Test]
        public void RollRespectsGroupSizeLevelRangeAndWeights()
        {
            var common = TestData.Species("common", new MonsterStats(10, 1, 1), new MonsterStats(1, 1, 1));
            var never = TestData.Species("never", new MonsterStats(10, 1, 1), new MonsterStats(1, 1, 1));
            var table = ScriptableObject.CreateInstance<EncounterTable>()
                .Set("entries", new List<EncounterTable.Entry>
                {
                    new EncounterTable.Entry(common, 2, 4, 10),
                    new EncounterTable.Entry(never, 1, 1, 0),
                })
                .Set("minGroupSize", 1).Set("maxGroupSize", 3);
            var rng = new System.Random(42);

            for (int i = 0; i < 100; i++)
            {
                var group = table.Roll(rng);
                Assert.That(group.Count, Is.InRange(1, 3));
                foreach (var monster in group)
                {
                    Assert.AreSame(common, monster.Species);
                    Assert.That(monster.Level, Is.InRange(2, 4));
                }
            }
        }

        [Test]
        public void EmptyTableThrows()
        {
            var table = ScriptableObject.CreateInstance<EncounterTable>();
            Assert.Throws<InvalidOperationException>(() => table.Roll(new System.Random(1)));
        }
    }
}
