using UnityEngine;
using WordRPG.Words;

namespace WordRPG.Monsters
{
    public enum SkillKind
    {
        Damage, // 공격
        Heal,   // 회복
        Guard   // 보호막 (이번 라운드 적 공격을 흡수)
    }

    // 사용자 기준 대상. 적이 쓰면 'Enemy'는 플레이어 파티를 가리킨다
    public enum SkillTarget
    {
        SingleEnemy,
        AllEnemies,
        SingleAlly,
        AllAllies,
        Self
    }

    [CreateAssetMenu(fileName = "NewSkill", menuName = "WordRPG/Skill", order = 11)]
    public class SkillData : ScriptableObject
    {
        [SerializeField] private string skillId;
        [SerializeField] private string displayName; // 예: "찌르기"
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private SkillKind kind;
        [SerializeField] private SkillTarget target;
        [SerializeField] private int power = 10;

        [Tooltip("이 스킬을 쓸 때 나오는 문제 유형. 강한 스킬일수록 어려운 '한→영'")]
        [SerializeField] private QuizDirection quizDirection = QuizDirection.EnglishToMeaning;

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public string Description => description;
        public SkillKind Kind => kind;
        public SkillTarget Target => target;
        public int Power => power;
        public QuizDirection QuizDirection => quizDirection;

        public bool NeedsTargetChoice => target == SkillTarget.SingleEnemy || target == SkillTarget.SingleAlly;
    }
}
