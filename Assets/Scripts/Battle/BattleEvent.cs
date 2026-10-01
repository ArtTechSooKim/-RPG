using WordRPG.Monsters;
using WordRPG.Words;

namespace WordRPG.Battle
{
    public enum BattleEventType
    {
        QuizAnswered, // Correct, Mastery
        SkillUsed,    // Actor, Skill
        SkillFailed,  // Actor, Skill (오답이라 기술이 빗나감)
        Damage,       // Actor, Target, Amount(HP 피해), Absorbed(보호막 흡수), IsCritical
        Heal,         // Actor, Target, Amount, IsCritical
        Shield,       // Actor, Target, Amount, IsCritical
        Defeated,     // Target
        RoundStarted, // Round
        Victory,
        Defeat
    }

    // 전투 로직이 만들어내는 사건 기록. UI는 이 목록을 순서대로 연출만 한다
    public class BattleEvent
    {
        public BattleEventType Type { get; private set; }
        public BattleUnit Actor { get; private set; }
        public BattleUnit Target { get; private set; }
        public SkillData Skill { get; private set; }
        public int Amount { get; private set; }
        public int Absorbed { get; private set; }
        public bool IsCritical { get; private set; }
        public bool Correct { get; private set; }
        public MasteryChange Mastery { get; private set; }
        public int Round { get; private set; }

        private BattleEvent(BattleEventType type) { Type = type; }

        public static BattleEvent QuizAnswered(BattleUnit actor, bool correct, MasteryChange mastery) =>
            new BattleEvent(BattleEventType.QuizAnswered) { Actor = actor, Correct = correct, Mastery = mastery };

        public static BattleEvent SkillUsed(BattleUnit actor, SkillData skill) =>
            new BattleEvent(BattleEventType.SkillUsed) { Actor = actor, Skill = skill };

        public static BattleEvent SkillFailed(BattleUnit actor, SkillData skill) =>
            new BattleEvent(BattleEventType.SkillFailed) { Actor = actor, Skill = skill };

        public static BattleEvent Damage(BattleUnit actor, BattleUnit target, SkillData skill, int amount, int absorbed, bool critical) =>
            new BattleEvent(BattleEventType.Damage) { Actor = actor, Target = target, Skill = skill, Amount = amount, Absorbed = absorbed, IsCritical = critical };

        public static BattleEvent Heal(BattleUnit actor, BattleUnit target, SkillData skill, int amount, bool critical) =>
            new BattleEvent(BattleEventType.Heal) { Actor = actor, Target = target, Skill = skill, Amount = amount, IsCritical = critical };

        public static BattleEvent Shield(BattleUnit actor, BattleUnit target, SkillData skill, int amount, bool critical) =>
            new BattleEvent(BattleEventType.Shield) { Actor = actor, Target = target, Skill = skill, Amount = amount, IsCritical = critical };

        public static BattleEvent Defeated(BattleUnit target) =>
            new BattleEvent(BattleEventType.Defeated) { Target = target };

        public static BattleEvent RoundStarted(int round) =>
            new BattleEvent(BattleEventType.RoundStarted) { Round = round };

        public static BattleEvent Victory() => new BattleEvent(BattleEventType.Victory);

        public static BattleEvent Defeat() => new BattleEvent(BattleEventType.Defeat);
    }
}
