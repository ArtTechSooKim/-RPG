using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using WordRPG.Heroes;
using WordRPG.Items;
using WordRPG.Monsters;
using WordRPG.Words;

namespace WordRPG.Tests
{
    // 테스트용 SO 생성 도우미. SO 필드는 private이라 리플렉션으로 채운다 (필드명 오타는 바로 예외)
    internal static class TestData
    {
        public static T Set<T>(this T obj, string field, object value) where T : class
        {
            var type = obj.GetType();
            FieldInfo info = null;
            while (type != null && info == null)
            {
                info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                type = type.BaseType;
            }
            if (info == null) throw new MissingFieldException(obj.GetType().Name, field);
            info.SetValue(obj, value);
            return obj;
        }

        public static SkillData Skill(string id, SkillKind kind, SkillTarget target, int power,
            QuizDirection direction = QuizDirection.EnglishToMeaning)
        {
            return ScriptableObject.CreateInstance<SkillData>()
                .Set("skillId", id).Set("displayName", id).Set("kind", kind).Set("target", target)
                .Set("power", power).Set("quizDirection", direction);
        }

        public static MonsterSpecies Species(string id, MonsterStats baseStats, MonsterStats growth, params SkillData[] skills)
        {
            return ScriptableObject.CreateInstance<MonsterSpecies>()
                .Set("speciesId", id).Set("displayName", id)
                .Set("baseStats", baseStats).Set("growthPerLevel", growth)
                .Set("skills", new List<SkillData>(skills));
        }

        public static ItemData Item(string id)
        {
            return ScriptableObject.CreateInstance<ItemData>().Set("itemId", id).Set("displayName", id);
        }

        // 상처약: HP를 heal만큼 회복하는 소모품
        public static ItemData Potion(string id, int heal)
        {
            return Item(id).Set("kind", ItemKind.Consumable).Set("healAmount", heal);
        }

        // 주인공: 기본 기술 '휘두르기'(공격 18, 쉬움) + 시작 성유물. 레벨 성장은 0 (필요하면 .Set("growthPerLevel", …))
        public static HeroData Hero(MonsterStats stats, params RelicData[] startingRelics)
        {
            return ScriptableObject.CreateInstance<HeroData>()
                .Set("displayName", "주인공")
                .Set("baseStats", stats).Set("growthPerLevel", new MonsterStats(0, 0, 0)).Set("startLevel", 1)
                .Set("basicSkill", Skill("hero_swing", SkillKind.Damage, SkillTarget.SingleEnemy, 18))
                .Set("startingRelics", new List<RelicData>(startingRelics));
        }

        // 성유물: 강화 비용 +1:(1개,30G) +2:(2,60) +3:(2,90) +4:(3,120) +5:(3,150), 각성 +3
        public static RelicData Relic(string id, SkillData skill, MonsterStats bonus, ItemData material = null,
            SkillData awakened = null)
        {
            var costs = new List<RelicUpgradeCost>
            {
                new RelicUpgradeCost(1, 30), new RelicUpgradeCost(2, 60), new RelicUpgradeCost(2, 90),
                new RelicUpgradeCost(3, 120), new RelicUpgradeCost(3, 150)
            };
            return ScriptableObject.CreateInstance<RelicData>()
                .Set("relicId", id).Set("displayName", id).Set("skill", skill).Set("awakenedSkill", awakened).Set("awakenLevel", 3)
                .Set("baseBonus", bonus).Set("bonusPerLevel", new MonsterStats(0, 0, 0))
                .Set("upgradeItem", material).Set("upgradeCosts", costs);
        }

        public static List<WordEntry> SampleWords()
        {
            return new List<WordEntry>
            {
                new WordEntry("", "abandon", "버리다, 포기하다", "v"),
                new WordEntry("", "acquire", "얻다, 획득하다", "v"),
                new WordEntry("", "obtain", "손에 넣다, 얻다", "v"),
                new WordEntry("", "budget", "예산", "n"),
                new WordEntry("", "affordable", "(가격이) 적당한", "adj"),
                new WordEntry("", "reasonable", "합리적인, 적당한", "adj"),
                new WordEntry("", "deadline", "마감 기한", "n"),
                new WordEntry("", "submit", "제출하다", "v"),
                new WordEntry("", "approve", "승인하다", "v"),
                new WordEntry("", "colleague", "동료", "n"),
            };
        }
    }

    // 항상 0번이 정답인 고정 문제를 내는 가짜 퀴즈. 전투 테스트에서 단어 시스템을 떼어낸다
    internal class FakeQuizProvider : IQuizProvider
    {
        private static readonly WordEntry Word = new WordEntry("", "test", "시험", "n");

        public readonly List<QuizDirection> AskedDirections = new List<QuizDirection>();
        public readonly List<bool> Submitted = new List<bool>();

        public QuizQuestion NextQuestion(QuizDirection preferredDirection)
        {
            AskedDirections.Add(preferredDirection);
            return new QuizQuestion(Word, preferredDirection, false, Word.English,
                new[] { Word.Meaning, "오답1", "오답2", "오답3" }, 0);
        }

        public MasteryChange SubmitAnswer(QuizQuestion question, bool correct)
        {
            Submitted.Add(correct);
            return new MasteryChange(question.Word.Id, MasteryLevel.Learning, MasteryLevel.Learning, correct);
        }
    }
}
