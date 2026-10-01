using System.Collections.Generic;
using WordRPG.Items;

namespace WordRPG.Monsters
{
    public enum EvolutionStatus
    {
        Ready,          // 진화 가능
        NoEvolution,    // 진화형이 없는 종
        LevelTooLow,
        NotEnoughItems
    }

    // 진화 = 레벨 조건 + 재료 소모. 탐험 → 재료 → 진화가 단어 공부의 게임 내 목표가 된다
    public static class Evolution
    {
        public static EvolutionStatus Check(MonsterInstance monster, Inventory inventory)
        {
            var species = monster.Species;
            if (species.EvolvesTo == null) return EvolutionStatus.NoEvolution;
            if (monster.Level < species.EvolveLevel) return EvolutionStatus.LevelTooLow;
            if (species.EvolveItem != null && inventory.GetCount(species.EvolveItem) < species.EvolveItemCount)
                return EvolutionStatus.NotEnoughItems;
            return EvolutionStatus.Ready;
        }

        // 이 재료로 진화하는 파티 몬스터 (소지품 화면의 '쓰는 곳'). 없으면 null
        public static MonsterInstance FindUser(IReadOnlyList<MonsterInstance> party, ItemData item)
        {
            if (party == null || item == null) return null;
            foreach (var monster in party)
            {
                var species = monster.Species;
                if (species.EvolvesTo != null && species.EvolveItem == item) return monster;
            }
            return null;
        }

        // 진화로 새로 생기는 기술 (진화 전에는 없던 것)
        public static List<SkillData> NewSkills(MonsterSpecies from, MonsterSpecies to)
        {
            var added = new List<SkillData>();
            if (from == null || to == null) return added;
            foreach (var skill in to.Skills)
            {
                bool had = false;
                foreach (var old in from.Skills)
                {
                    if (old == skill) had = true;
                }
                if (!had && skill != null) added.Add(skill);
            }
            return added;
        }

        // 진화하면 레벨은 유지, 종이 바뀌고 HP 완전 회복
        public static bool TryEvolve(MonsterInstance monster, Inventory inventory)
        {
            if (Check(monster, inventory) != EvolutionStatus.Ready) return false;

            var species = monster.Species;
            if (species.EvolveItem != null && !inventory.TryRemove(species.EvolveItem, species.EvolveItemCount))
                return false;

            monster.ChangeSpecies(species.EvolvesTo);
            return true;
        }
    }
}
