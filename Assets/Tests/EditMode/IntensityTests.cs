using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WordRPG.Battle;
using WordRPG.Monsters;
using WordRPG.Words;

namespace WordRPG.Tests
{
    // 공격 기술 강도: 단어 n개를 연속으로 맞히면 ×1.2·×1.5·×2, 하나라도 틀리면 이번 턴 공격 실패
    public class IntensityTests
    {
        private const int Correct = 0; // FakeQuizProvider는 항상 0번이 정답
        private const int Wrong = 1;
        private const float Normal = 5f;
        private const float Fast = 1f;

        private BattleConfig config;
        private FakeQuizProvider quiz;
        private SkillData strike; // 단일 공격 위력 40 → 기본 피해 20
        private SkillData heal;
        private MonsterSpecies hero;
        private MonsterSpecies dummy;

        [SetUp]
        public void SetUp()
        {
            config = new BattleConfig(10f, 3f, 1.5f, variance: 0f, expPerCorrectAnswer: 2, comboBonusPerStep: 0.05f);
            quiz = new FakeQuizProvider();
            strike = TestData.Skill("strike", SkillKind.Damage, SkillTarget.SingleEnemy, 40, QuizDirection.MeaningToEnglish);
            heal = TestData.Skill("heal", SkillKind.Heal, SkillTarget.Self, 10);
            hero = TestData.Species("hero", new MonsterStats(100, 10, 10), new MonsterStats(0, 0, 0), strike, heal);
            dummy = TestData.Species("dummy", new MonsterStats(9999, 1, 10), new MonsterStats(0, 0, 0),
                TestData.Skill("poke", SkillKind.Damage, SkillTarget.SingleEnemy, 1));
        }

        private BattleEngine Start(int startStreak = 0) =>
            new BattleEngine(new ICombatant[] { new MonsterInstance(hero, 1) }, new[] { new MonsterInstance(dummy, 1) },
                quiz, config, new Random(1), startStreak);

        private static BattleEvent PlayerHit(IReadOnlyList<BattleEvent> events) =>
            events.FirstOrDefault(e => e.Type == BattleEventType.Damage && e.Actor.IsPlayerSide);

        [Test]
        public void DefaultIntensitiesAreOneToTwoTimes()
        {
            var defaults = new BattleConfig();
            Assert.AreEqual(4, defaults.MaxIntensity);
            Assert.AreEqual(1f, defaults.IntensityMultiplier(1));
            Assert.AreEqual(1.2f, defaults.IntensityMultiplier(2));
            Assert.AreEqual(1.5f, defaults.IntensityMultiplier(3));
            Assert.AreEqual(2f, defaults.IntensityMultiplier(4), "최대 2배");
        }

        [Test]
        public void ChainAsksEveryWordBeforeTheSkillFires()
        {
            var battle = Start();
            var enemy = battle.Enemies[0];
            int hpBefore = enemy.Hp;
            battle.SelectSkill(strike, enemy, 3);

            for (int i = 1; i <= 2; i++)
            {
                var events = battle.SubmitAnswer(Correct, Normal);
                Assert.AreEqual(BattlePhase.AnsweringQuiz, battle.Phase, "아직 같은 차례 — 다음 단어");
                Assert.IsNotNull(battle.CurrentQuestion);
                Assert.AreEqual(i, battle.ChainCorrect);
                Assert.AreEqual(3, battle.Intensity);
                Assert.IsNull(PlayerHit(events), "다 맞히기 전엔 공격하지 않음");
                Assert.AreEqual(hpBefore, enemy.Hp);
            }

            var last = battle.SubmitAnswer(Correct, Normal);
            var hit = PlayerHit(last);
            Assert.IsNotNull(hit);
            // 기본 20 × 강도 1.5 × (1 + 3연속 콤보 +10%) = 33
            Assert.AreEqual(33, hit.Amount);
            Assert.AreEqual(1.5f, last.Single(e => e.Type == BattleEventType.SkillUsed && e.Actor.IsPlayerSide).Multiplier);
            Assert.AreEqual(3, battle.Streak, "맞힌 단어는 하나하나 콤보");
            Assert.AreEqual(3, battle.CorrectAnswers);
            Assert.AreEqual(3, quiz.AskedDirections.Count, "단어 3개");
            Assert.IsTrue(quiz.AskedDirections.All(d => d == QuizDirection.MeaningToEnglish), "기술의 문제 유형 그대로");
            Assert.AreEqual(BattlePhase.ChoosingSkill, battle.Phase, "적이 행동하고 다음 차례");
        }

        [Test]
        public void MissInTheMiddleMeansNoDamageThisTurn()
        {
            var battle = Start(startStreak: 3);
            var enemy = battle.Enemies[0];
            int hpBefore = enemy.Hp;
            int round = battle.Round;
            battle.SelectSkill(strike, enemy, 4);
            battle.SubmitAnswer(Correct, Normal);
            battle.SubmitAnswer(Correct, Normal);
            Assert.AreEqual(5, battle.Streak);

            var events = battle.SubmitAnswer(Wrong, Normal);
            Assert.IsNull(PlayerHit(events), "틀리면 이번 턴 공격 실패");
            Assert.IsTrue(events.Any(e => e.Type == BattleEventType.SkillFailed));
            Assert.AreEqual(hpBefore, enemy.Hp);
            Assert.AreEqual(0, battle.Streak, "콤보도 끊김");
            Assert.AreEqual(round + 1, battle.Round, "차례가 넘어감");
            Assert.AreEqual(BattlePhase.ChoosingSkill, battle.Phase);
            Assert.AreEqual(1, battle.Intensity, "다음 기술은 다시 강도 1부터");
        }

        [Test]
        public void TimeoutInTheChainAlsoFails()
        {
            var battle = Start();
            battle.SelectSkill(strike, battle.Enemies[0], 2);
            battle.SubmitAnswer(Correct, Normal);
            var events = battle.SubmitAnswer(-1, 10f);
            Assert.IsNull(PlayerHit(events));
            Assert.AreEqual(0, battle.Streak);
        }

        [Test]
        public void CriticalOnlyWhenEveryWordWasFast()
        {
            var slow = Start();
            slow.SelectSkill(strike, slow.Enemies[0], 3);
            slow.SubmitAnswer(Correct, Fast);
            slow.SubmitAnswer(Correct, Fast);
            Assert.IsTrue(slow.CanStillCrit);
            var last = slow.SubmitAnswer(Correct, Normal);
            Assert.IsFalse(PlayerHit(last).IsCritical, "하나라도 느리면 크리티컬 아님");

            var fast = Start();
            fast.SelectSkill(strike, fast.Enemies[0], 2);
            fast.SubmitAnswer(Correct, Fast);
            var hit = PlayerHit(fast.SubmitAnswer(Correct, Fast));
            Assert.IsTrue(hit.IsCritical);
            // 20 × 1.2 × (1 + 2연속 +5%) × 크리티컬 1.5 = 37.8 → 38
            Assert.AreEqual(38, hit.Amount);
        }

        [Test]
        public void EstimateMatchesActualDamage()
        {
            var battle = Start(startStreak: 1);
            var enemy = battle.Enemies[0];
            int estimate = battle.EstimateDamage(strike, enemy, 4);
            battle.SelectSkill(strike, enemy, 4);
            IReadOnlyList<BattleEvent> events = null;
            for (int i = 0; i < 4; i++) events = battle.SubmitAnswer(Correct, Normal);
            // 20 × 2 × (1 + 5연속 +20%) = 48
            Assert.AreEqual(48, estimate);
            Assert.AreEqual(estimate, PlayerHit(events).Amount);
        }

        [Test]
        public void OnlyAttackSkillsCanBeCharged()
        {
            var battle = Start();
            Assert.IsTrue(BattleEngine.CanChooseIntensity(strike));
            Assert.IsFalse(BattleEngine.CanChooseIntensity(heal));
            Assert.Throws<ArgumentException>(() => battle.SelectSkill(heal, null, 2), "회복 기술은 강도 없음");
            Assert.Throws<ArgumentOutOfRangeException>(() => battle.SelectSkill(strike, battle.Enemies[0], 5), "최대 4");
            Assert.Throws<ArgumentOutOfRangeException>(() => battle.SelectSkill(strike, battle.Enemies[0], 0));
            Assert.AreEqual(BattlePhase.ChoosingSkill, battle.Phase, "잘못 고르면 아무 일도 없음");
        }
    }
}
