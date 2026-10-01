using System;

namespace WordRPG.Monsters
{
    // 실제 파티/필드에 존재하는 몬스터 한 마리. MonoBehaviour가 아니라 씬 없이 생성·시뮬레이션 가능
    public class MonsterInstance
    {
        public MonsterSpecies Species { get; private set; }
        public int Level { get; private set; }
        public int Exp { get; private set; } // 현재 레벨에서 쌓은 경험치
        public int CurrentHp { get; private set; }

        public MonsterStats Stats => Species.GetStats(Level);
        public string DisplayName => Species.DisplayName;
        public bool IsFainted => CurrentHp <= 0;
        public bool IsMaxLevel => Level >= LevelCurve.MaxLevel;

        public MonsterInstance(MonsterSpecies species, int level, int exp = 0, int? currentHp = null)
        {
            Species = species ?? throw new ArgumentNullException(nameof(species));
            Level = Math.Max(1, Math.Min(level, LevelCurve.MaxLevel));
            Exp = IsMaxLevel ? 0 : Math.Max(0, exp);
            CurrentHp = currentHp.HasValue ? Math.Max(0, Math.Min(currentHp.Value, Stats.MaxHp)) : Stats.MaxHp;
        }

        // 반환값: 오른 레벨 수. 레벨업으로 늘어난 최대 HP만큼 현재 HP도 늘어난다
        public int GainExp(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (IsMaxLevel) return 0;

            int levelsGained = 0;
            Exp += amount;
            while (!IsMaxLevel && Exp >= LevelCurve.ExpToNextLevel(Level))
            {
                int oldMaxHp = Stats.MaxHp;
                Exp -= LevelCurve.ExpToNextLevel(Level);
                Level++;
                levelsGained++;
                if (!IsFainted) CurrentHp += Stats.MaxHp - oldMaxHp;
            }
            if (IsMaxLevel) Exp = 0;
            return levelsGained;
        }

        // 반환값: 실제로 깎인 HP
        public int TakeDamage(int amount)
        {
            int dealt = Math.Min(Math.Max(0, amount), CurrentHp);
            CurrentHp -= dealt;
            return dealt;
        }

        // 반환값: 실제로 회복된 HP. 기절한 몬스터는 회복 스킬로 살릴 수 없다
        public int Heal(int amount)
        {
            if (IsFainted) return 0;
            int healed = Math.Min(Math.Max(0, amount), Stats.MaxHp - CurrentHp);
            CurrentHp += healed;
            return healed;
        }

        // 마을 휴식 등 완전 회복 (기절 포함)
        public void RestoreFully() => CurrentHp = Stats.MaxHp;

        internal void ChangeSpecies(MonsterSpecies newSpecies)
        {
            Species = newSpecies ?? throw new ArgumentNullException(nameof(newSpecies));
            RestoreFully();
        }
    }
}
