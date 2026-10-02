using System;
using System.Collections.Generic;
using UnityEngine;
using WordRPG.Items;
using WordRPG.Monsters;

namespace WordRPG.Heroes
{
    // 강화 한 단계의 비용 (+0→+1, +1→+2, …)
    [Serializable]
    public class RelicUpgradeCost
    {
        [SerializeField] private int itemCount = 1;
        [SerializeField] private int gold = 30;

        public int ItemCount => itemCount;
        public int Gold => gold;

        private RelicUpgradeCost() { } // Unity 직렬화용

        public RelicUpgradeCost(int itemCount, int gold)
        {
            this.itemCount = itemCount;
            this.gold = gold;
        }
    }

    // 성유물: 주인공이 끼우는 유물. 끼우면 기술 1개 + 능력치 보너스. 제단에서 재료·골드로 +5까지 강화,
    // awakenLevel(+3)에 '각성'하면 기술이 더 강한 기술로 바뀐다. 여러 개를 모아 3칸에 골라 끼운다
    [CreateAssetMenu(fileName = "NewRelic", menuName = "WordRPG/Relic", order = 12)]
    public class RelicData : ScriptableObject
    {
        public const int MaxLevel = 5;

        [Header("기본")]
        [Tooltip("세이브에 기록되는 id. 정한 뒤에는 바꾸지 말 것")]
        [SerializeField] private string relicId;
        [SerializeField] private string displayName; // 예: "깃펜"
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private MonsterRole role;
        [Tooltip("비워 두면 Resources/Art/NinjaAdventure/Relics/{relicId}.png")]
        [SerializeField] private Sprite icon;
        [Tooltip("그림이 없을 때 쓰는 임시 색")]
        [SerializeField] private Color placeholderColor = Color.white;

        [Header("기술 (각성 단계부터 awakenedSkill로 바뀜)")]
        [SerializeField] private SkillData skill;
        [SerializeField] private SkillData awakenedSkill;
        [SerializeField] private int awakenLevel = 3;

        [Header("능력치 보너스 = 기본 + 단계당 x 강화 단계")]
        [SerializeField] private MonsterStats baseBonus;
        [SerializeField] private MonsterStats bonusPerLevel;

        [Header("강화 (+1 ~ +5)")]
        [SerializeField] private ItemData upgradeItem;
        [Tooltip("위에서부터 +0→+1, +1→+2, … 비용. 5개")]
        [SerializeField] private List<RelicUpgradeCost> upgradeCosts = new List<RelicUpgradeCost>();

        public string RelicId => relicId;
        public string DisplayName => displayName;
        public string Description => description;
        public MonsterRole Role => role;
        public Sprite Icon => icon;
        public Color PlaceholderColor => placeholderColor;
        public SkillData Skill => skill;
        public SkillData AwakenedSkill => awakenedSkill;
        public int AwakenLevel => awakenLevel;
        public MonsterStats BaseBonus => baseBonus;
        public MonsterStats BonusPerLevel => bonusPerLevel;
        public ItemData UpgradeItem => upgradeItem;
        public IReadOnlyList<RelicUpgradeCost> UpgradeCosts => upgradeCosts;

        public bool HasAwakening => awakenedSkill != null && awakenLevel > 0;

        public SkillData SkillAt(int level) => HasAwakening && level >= awakenLevel ? awakenedSkill : skill;

        public MonsterStats BonusAt(int level) => baseBonus + bonusPerLevel * Mathf.Clamp(level, 0, MaxLevel);

        // level → level+1 강화 비용. 최대 단계이거나 비용표가 모자라면 null
        public RelicUpgradeCost CostFrom(int level)
        {
            if (level < 0 || level >= MaxLevel || level >= upgradeCosts.Count) return null;
            return upgradeCosts[level];
        }

        // level → level+1 강화로 각성하는지
        public bool AwakensWhenUpgradedFrom(int level) => HasAwakening && level + 1 == awakenLevel;
    }
}
