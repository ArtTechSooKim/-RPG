using System;
using System.Collections.Generic;
using WordRPG.Items;
using WordRPG.Monsters;

namespace WordRPG.Battle
{
    public class RewardItem
    {
        public ItemData Item { get; }
        public int Count { get; internal set; }

        public RewardItem(ItemData item, int count)
        {
            Item = item;
            Count = count;
        }
    }

    public class BattleReward
    {
        public int Exp { get; }
        public int Gold { get; }
        public IReadOnlyList<RewardItem> Items { get; }

        public BattleReward(int exp, int gold, IReadOnlyList<RewardItem> items)
        {
            Exp = exp;
            Gold = gold;
            Items = items;
        }
    }

    public static class BattleRewardCalculator
    {
        // 경험치 = Σ(적 기본 경험치 x 레벨) + 정답 수 x 정답 보너스
        public static BattleReward Calculate(IReadOnlyList<MonsterInstance> defeatedEnemies, int correctAnswers,
            BattleConfig config, Random rng)
        {
            int exp = correctAnswers * config.ExpPerCorrectAnswer;
            int gold = 0;
            var items = new List<RewardItem>();

            foreach (var enemy in defeatedEnemies)
            {
                exp += enemy.Species.ExpReward * enemy.Level;
                gold += enemy.Species.GoldReward * enemy.Level;

                foreach (var drop in enemy.Species.Drops)
                {
                    if (drop.Item == null || rng.NextDouble() >= drop.Chance) continue;
                    var existing = items.Find(i => i.Item == drop.Item);
                    if (existing != null) existing.Count += drop.Count;
                    else items.Add(new RewardItem(drop.Item, drop.Count));
                }
            }

            return new BattleReward(exp, gold, items);
        }

        // 파티 전원(기절한 몬스터 포함)이 같은 경험치를 받는다. 반환: 몬스터별 오른 레벨 수
        public static int[] Apply(BattleReward reward, IReadOnlyList<MonsterInstance> party, Inventory inventory)
        {
            var levelsGained = new int[party.Count];
            for (int i = 0; i < party.Count; i++) levelsGained[i] = party[i].GainExp(reward.Exp);

            if (reward.Gold > 0) inventory.AddGold(reward.Gold);
            foreach (var item in reward.Items) inventory.Add(item.Item, item.Count);
            return levelsGained;
        }
    }
}
