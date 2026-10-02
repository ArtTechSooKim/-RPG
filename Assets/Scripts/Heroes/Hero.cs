using System;
using System.Collections.Generic;
using WordRPG.Monsters;

namespace WordRPG.Heroes
{
    // 모은 성유물 하나 + 강화 단계
    public class OwnedRelic
    {
        public RelicData Data { get; }
        public int Level { get; internal set; }

        public SkillData Skill => Data.SkillAt(Level);
        public MonsterStats Bonus => Data.BonusAt(Level);
        public bool IsMaxLevel => Level >= RelicData.MaxLevel;
        public bool IsAwakened => Data.HasAwakening && Level >= Data.AwakenLevel;

        public OwnedRelic(RelicData data, int level = 0)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Level = Math.Max(0, Math.Min(level, RelicData.MaxLevel));
        }
    }

    // 혼자 싸우는 주인공. 능력치 = 레벨 능력치 + 끼운 성유물 보너스, 기술 = 기본 기술 + 끼운 성유물마다 1개.
    // 성유물은 여러 개 모으고(Relics) 그중 3개까지 칸에 끼운다(Slots)
    public class Hero : ICombatant
    {
        private readonly List<OwnedRelic> relics = new List<OwnedRelic>();
        private readonly OwnedRelic[] slots = new OwnedRelic[HeroData.SlotCount];

        public HeroData Data { get; }
        public int Level { get; private set; }
        public int Exp { get; private set; } // 현재 레벨에서 쌓은 경험치
        public int CurrentHp { get; private set; }

        public string DisplayName => Data.DisplayName;
        public bool IsFainted => CurrentHp <= 0;
        public bool IsMaxLevel => Level >= LevelCurve.MaxLevel;
        public int ExpToNextLevel => IsMaxLevel ? 0 : LevelCurve.ExpToNextLevel(Level) - Exp;

        public IReadOnlyList<OwnedRelic> Relics => relics;
        public int SlotCount => slots.Length;

        public Hero(HeroData data, int level, int exp = 0)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Level = Math.Max(1, Math.Min(level, LevelCurve.MaxLevel));
            Exp = IsMaxLevel ? 0 : Math.Max(0, exp);
            CurrentHp = Stats.MaxHp;
        }

        // ------------------------------------------------------------------ 능력치·기술

        public MonsterStats LevelStats => Data.GetStats(Level);

        public MonsterStats RelicBonus
        {
            get
            {
                var sum = new MonsterStats(0, 0, 0);
                foreach (var relic in slots) if (relic != null) sum = sum + relic.Bonus;
                return sum;
            }
        }

        public MonsterStats Stats => LevelStats + RelicBonus;

        // 기본 기술 + 끼운 순서대로 성유물 기술 (같은 기술은 한 번만)
        public IReadOnlyList<SkillData> Skills
        {
            get
            {
                var list = new List<SkillData>();
                if (Data.BasicSkill != null) list.Add(Data.BasicSkill);
                foreach (var relic in slots)
                {
                    var skill = relic?.Skill;
                    if (skill != null && !list.Contains(skill)) list.Add(skill);
                }
                return list;
            }
        }

        // 이 기술을 주는 끼운 성유물 (기본 기술이면 null)
        public OwnedRelic SourceOf(SkillData skill)
        {
            foreach (var relic in slots)
            {
                if (relic != null && relic.Skill == skill) return relic;
            }
            return null;
        }

        // ------------------------------------------------------------------ 성유물 모으기·끼우기

        public OwnedRelic Find(RelicData data) => relics.Find(r => r.Data == data);

        public bool Owns(RelicData data) => Find(data) != null;

        // 새 성유물을 얻는다. 빈 칸이 있으면 바로 끼운다. 이미 가진 것이면 null
        public OwnedRelic AddRelic(RelicData data, int level = 0)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (Owns(data)) return null;
            var relic = new OwnedRelic(data, level);
            relics.Add(relic);
            Equip(relic);
            return relic;
        }

        // 저장된 성유물을 되살릴 때: 칸 번호를 지정 (-1 = 안 끼움)
        public OwnedRelic RestoreRelic(RelicData data, int level, int slot)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var relic = Find(data);
            if (relic == null)
            {
                relic = new OwnedRelic(data, level);
                relics.Add(relic);
            }
            if (slot >= 0 && slot < slots.Length && slots[slot] == null && !IsEquipped(relic)) slots[slot] = relic;
            ClampHp();
            return relic;
        }

        public OwnedRelic SlotAt(int slot) => slot >= 0 && slot < slots.Length ? slots[slot] : null;

        public int SlotOf(OwnedRelic relic) => Array.IndexOf(slots, relic);

        public bool IsEquipped(OwnedRelic relic) => relic != null && SlotOf(relic) >= 0;

        public bool HasEmptySlot => Array.IndexOf(slots, null) >= 0;

        public int EquippedCount
        {
            get
            {
                int count = 0;
                foreach (var relic in slots) if (relic != null) count++;
                return count;
            }
        }

        // 첫 빈 칸에 끼운다. 이미 끼웠거나 칸이 가득 찼으면 false
        public bool Equip(OwnedRelic relic)
        {
            if (relic == null || !relics.Contains(relic) || IsEquipped(relic)) return false;
            int empty = Array.IndexOf(slots, null);
            if (empty < 0) return false;
            slots[empty] = relic;
            return true;
        }

        // 칸에서 뺀다. 최대 HP가 줄면 현재 HP도 맞춘다
        public bool Unequip(OwnedRelic relic)
        {
            int slot = SlotOf(relic);
            if (slot < 0) return false;
            slots[slot] = null;
            ClampHp();
            return true;
        }

        // ------------------------------------------------------------------ 성장·HP

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

        public int TakeDamage(int amount)
        {
            int dealt = Math.Min(Math.Max(0, amount), CurrentHp);
            CurrentHp -= dealt;
            return dealt;
        }

        public int Heal(int amount)
        {
            if (IsFainted) return 0;
            int healed = Math.Min(Math.Max(0, amount), Stats.MaxHp - CurrentHp);
            CurrentHp += healed;
            return healed;
        }

        public void RestoreFully() => CurrentHp = Stats.MaxHp;

        // 저장된 HP로 맞출 때 (0 ~ 최대 HP)
        public void SetHp(int hp) => CurrentHp = Math.Max(0, Math.Min(hp, Stats.MaxHp));

        private void ClampHp()
        {
            if (CurrentHp > Stats.MaxHp) CurrentHp = Stats.MaxHp;
        }
    }
}
