using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
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
}
