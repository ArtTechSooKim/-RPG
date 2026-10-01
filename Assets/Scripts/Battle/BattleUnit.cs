using System.Collections.Generic;
using WordRPG.Monsters;

namespace WordRPG.Battle
{
    // 전투 중인 몬스터. HP는 MonsterInstance에 직접 반영되어 전투 후에도 유지되고, 보호막만 전투 전용
    public class BattleUnit
    {
        public MonsterInstance Monster { get; }
        public bool IsPlayerSide { get; }
        public int Index { get; } // 같은 편 안에서의 위치 (UI 배치용)
        public int Shield { get; private set; }

        public string DisplayName => Monster.DisplayName;
        public int Hp => Monster.CurrentHp;
        public int MaxHp => Monster.Stats.MaxHp;
        public int Attack => Monster.Stats.Attack;
        public int Defense => Monster.Stats.Defense;
        public IReadOnlyList<SkillData> Skills => Monster.Species.Skills;
        public bool IsDefeated => Monster.IsFainted;

        public BattleUnit(MonsterInstance monster, bool isPlayerSide, int index)
        {
            Monster = monster;
            IsPlayerSide = isPlayerSide;
            Index = index;
        }

        // 보호막이 먼저 흡수하고 남은 만큼 HP가 깎인다
        internal void ReceiveDamage(int amount, out int hpDamage, out int absorbed)
        {
            absorbed = amount < Shield ? amount : Shield;
            Shield -= absorbed;
            hpDamage = Monster.TakeDamage(amount - absorbed);
        }

        internal int ReceiveHeal(int amount) => Monster.Heal(amount);

        internal void AddShield(int amount) => Shield += amount;

        internal void ClearShield() => Shield = 0;
    }
}
