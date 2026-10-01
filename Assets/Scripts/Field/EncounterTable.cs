using System;
using System.Collections.Generic;
using UnityEngine;
using WordRPG.Monsters;

namespace WordRPG.Field
{
    // 지역별 야생 몬스터 출현표. 풀숲을 밟을 때 여기서 적 무리를 뽑는다
    [CreateAssetMenu(fileName = "NewEncounterTable", menuName = "WordRPG/Encounter Table", order = 30)]
    public class EncounterTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [SerializeField] private MonsterSpecies species;
            [SerializeField] private int minLevel = 1;
            [SerializeField] private int maxLevel = 3;
            [Tooltip("출현 가중치. 클수록 자주 나옴")]
            [SerializeField] private int weight = 10;

            public MonsterSpecies Species => species;
            public int MinLevel => minLevel;
            public int MaxLevel => maxLevel;
            public int Weight => weight;

            private Entry() { } // Unity 직렬화용

            public Entry(MonsterSpecies species, int minLevel, int maxLevel, int weight)
            {
                this.species = species;
                this.minLevel = minLevel;
                this.maxLevel = maxLevel;
                this.weight = weight;
            }
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        [SerializeField] private int minGroupSize = 1;
        [SerializeField] private int maxGroupSize = 2;

        public IReadOnlyList<Entry> Entries => entries;
        public int MinGroupSize => minGroupSize;
        public int MaxGroupSize => maxGroupSize;

        public List<MonsterInstance> Roll(System.Random rng)
        {
            int totalWeight = 0;
            foreach (var entry in entries)
            {
                if (entry.Species != null) totalWeight += Math.Max(0, entry.Weight);
            }
            if (totalWeight == 0) throw new InvalidOperationException($"{name}: 출현 몬스터가 없습니다");

            int groupSize = rng.Next(minGroupSize, maxGroupSize + 1);
            var group = new List<MonsterInstance>(groupSize);
            for (int i = 0; i < groupSize; i++)
            {
                var entry = PickWeighted(totalWeight, rng);
                group.Add(new MonsterInstance(entry.Species, rng.Next(entry.MinLevel, entry.MaxLevel + 1)));
            }
            return group;
        }

        private Entry PickWeighted(int totalWeight, System.Random rng)
        {
            int roll = rng.Next(totalWeight);
            foreach (var entry in entries)
            {
                if (entry.Species == null || entry.Weight <= 0) continue;
                if (roll < entry.Weight) return entry;
                roll -= entry.Weight;
            }
            throw new InvalidOperationException("가중치 계산 오류");
        }
    }
}
