using System.Collections.Generic;

namespace WordRPG.Monsters
{
    // 전투에 나설 수 있는 것: 적 몬스터(MonsterInstance)와 주인공(Hero). HP는 여기에 직접 반영되어 전투 후에도 유지된다
    public interface ICombatant
    {
        string DisplayName { get; }
        int Level { get; }
        int CurrentHp { get; }
        MonsterStats Stats { get; }
        IReadOnlyList<SkillData> Skills { get; }
        bool IsFainted { get; }

        // 반환값: 실제로 깎인 HP
        int TakeDamage(int amount);

        // 반환값: 실제로 회복된 HP. 쓰러진 상태에서는 회복되지 않는다
        int Heal(int amount);
    }
}
