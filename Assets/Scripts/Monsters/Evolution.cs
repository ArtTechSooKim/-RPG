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
