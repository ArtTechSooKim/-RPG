using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WordRPG.Battle;
using WordRPG.Items;
using WordRPG.Monsters;
using WordRPG.Words;

namespace WordRPG.Tests
{
    public class BattleEngineTests
    {
        private const int Correct = 0; // FakeQuizProvider는 항상 0번이 정답
        private const int Wrong = 1;
        private const float Normal = 5f; // 크리티컬(3초)보다 느리고 제한시간(10초)보다 빠름
        private const float Fast = 1f;

        private BattleConfig config;
        private FakeQuizProvider quiz;
        private SkillData strike;     // 단일 공격 위력 20
        private SkillData blast;      // 전체 공격 위력 20 (한→영)
        private SkillData heal;       // 단일 회복 위력 10
        private SkillData guard;      // 아군 전체 보호막 위력 10
        private SkillData enemyBite;  // 적 단일 공격 위력 10
        private MonsterSpecies hero;  // 30 / 10 / 10
        private MonsterSpecies slime; // 20 / 10 / 10

        [SetUp]
        public void SetUp()
        {
            config = new BattleConfig(answerTimeLimitSeconds: 10f, criticalTimeSeconds: 3f, criticalMultiplier: 1.5f,
                variance: 0f, expPerCorrectAnswer: 2);
            quiz = new FakeQuizProvider();
            strike = TestData.Skill("strike", SkillKind.Damage, SkillTarget.SingleEnemy, 20);
            blast = TestData.Skill("blast", SkillKind.Damage, SkillTarget.AllEnemies, 20, QuizDirection.MeaningToEnglish);
            heal = TestData.Skill("heal", SkillKind.Heal, SkillTarget.SingleAlly, 10);
            guard = TestData.Skill("guard", SkillKind.Guard, SkillTarget.AllAllies, 10);
            enemyBite = TestData.Skill("bite", SkillKind.Damage, SkillTarget.SingleEnemy, 10);
            hero = TestData.Species("hero", new MonsterStats(30, 10, 10), new MonsterStats(0, 0, 0), strike, blast, heal, guard);
            slime = TestData.Species("slime", new MonsterStats(20, 10, 10), new MonsterStats(0, 0, 0), enemyBite)
                .Set("expReward", 5).Set("goldReward", 3);
        }

        private BattleEngine Start(IList<MonsterInstance> party, IList<MonsterInstance> enemies)
        {
            return new BattleEngine(party.ToList(), enemies.ToList(), quiz, config, new Random(1));
        }

        private MonsterInstance Hero(int? hp = null) => new MonsterInstance(hero, 1, currentHp: hp);
        private MonsterInstance Slime(int? hp = null) => new MonsterInstance(slime, 1, currentHp: hp);

        [Test]
        public void CorrectAnswerFiresSkill()
        {
            var battle = Start(new[] { Hero(), Hero() }, new[] { Slime() });

            battle.SelectSkill(strike, battle.Enemies[0]);
            var events = battle.SubmitAnswer(Correct, Normal);

            // 20 x 10 / (10 + 10) = 10
            Assert.AreEqual(10, battle.Enemies[0].Hp);
            var hit = events.Single(e => e.Type == BattleEventType.Damage);
            Assert.AreEqual(10, hit.Amount);
            Assert.IsFalse(hit.IsCritical);
            Assert.AreEqual(1, battle.CorrectAnswers);
        }

        [Test]
        public void FastAnswerIsCritical()
        {
            var battle = Start(new[] { Hero(), Hero() }, new[] { Slime() });

            battle.SelectSkill(strike, battle.Enemies[0]);
            var events = battle.SubmitAnswer(Correct, Fast);

            Assert.AreEqual(5, battle.Enemies[0].Hp);
            Assert.IsTrue(events.Single(e => e.Type == BattleEventType.Damage).IsCritical);
        }

        [Test]
        public void WrongAnswerMakesSkillFail()
        {
            var battle = Start(new[] { Hero(), Hero() }, new[] { Slime() });

            battle.SelectSkill(strike, battle.Enemies[0]);
            var events = battle.SubmitAnswer(Wrong, Fast);

            Assert.AreEqual(20, battle.Enemies[0].Hp);
            Assert.IsTrue(events.Any(e => e.Type == BattleEventType.SkillFailed));
            Assert.IsFalse(events.Any(e => e.Type == BattleEventType.Damage));
            Assert.AreEqual(1, battle.WrongAnswers);
            CollectionAssert.AreEqual(new[] { false }, quiz.Submitted, "오답이 단어 시스템에 기록돼야 함");
        }

        [Test]
        public void TimeoutCountsAsWrong()
        {
            var battle = Start(new[] { Hero(), Hero() }, new[] { Slime() });

            battle.SelectSkill(strike, battle.Enemies[0]);
            battle.SubmitAnswer(Correct, 11f);
            Assert.AreEqual(20, battle.Enemies[0].Hp);

            battle.SelectSkill(strike, battle.Enemies[0]);
            battle.SubmitAnswer(-1, 10f);
            Assert.AreEqual(2, battle.WrongAnswers);
        }

        [Test]
        public void QuizDirectionComesFromSkill()
        {
            var battle = Start(new[] { Hero(), Hero() }, new[] { Slime(hp: 20) });

            battle.SelectSkill(blast);
            Assert.AreEqual(QuizDirection.MeaningToEnglish, quiz.AskedDirections.Last());
        }

        [Test]
        public void AllEnemiesSkillHitsEveryEnemy()
        {
            var battle = Start(new[] { Hero() }, new[] { Slime(), Slime(), Slime() });

            battle.SelectSkill(blast);
            var events = battle.SubmitAnswer(Correct, Normal);

            // 아군이 1마리라 곧바로 적 차례도 이어지므로 아군 공격만 센다
            Assert.AreEqual(3, events.Count(e => e.Type == BattleEventType.Damage && e.Actor.IsPlayerSide));
            Assert.IsTrue(battle.Enemies.All(e => e.Hp == 10));
        }

        [Test]
        public void EnemiesActAfterWholePartyThenNewRoundStarts()
        {
            var battle = Start(new[] { Hero(), Hero() }, new[] { Slime() });

            battle.SelectSkill(strike, battle.Enemies[0]);
            battle.SubmitAnswer(Wrong, Normal);
            Assert.AreSame(battle.Party[1], battle.CurrentActor);
            Assert.AreEqual(1, battle.Round);

            battle.SelectSkill(strike, battle.Enemies[0]);
            var events = battle.SubmitAnswer(Wrong, Normal);

            // 적 공격: 10 x 10 / 20 = 5
            var enemyHit = events.Single(e => e.Type == BattleEventType.Damage);
            Assert.IsFalse(enemyHit.Actor.IsPlayerSide);
            Assert.AreEqual(5, enemyHit.Amount);
            Assert.AreEqual(55, battle.Party.Sum(p => p.Hp));
            Assert.AreEqual(2, battle.Round);
            Assert.AreSame(battle.Party[0], battle.CurrentActor);
            Assert.AreEqual(BattlePhase.ChoosingSkill, battle.Phase);
        }

        [Test]
        public void GuardAbsorbsEnemyDamageAndExpiresNextRound()
        {
            var battle = Start(new[] { Hero() }, new[] { Slime() });

            battle.SelectSkill(guard);
            var events = battle.SubmitAnswer(Correct, Normal);

            // 보호막 = 10 + 방어 10 / 2 = 15, 적 공격 5는 전부 흡수
            Assert.AreEqual(15, events.Single(e => e.Type == BattleEventType.Shield).Amount);
            var enemyHit = events.Single(e => e.Type == BattleEventType.Damage);
            Assert.AreEqual(0, enemyHit.Amount);
            Assert.AreEqual(5, enemyHit.Absorbed);
            Assert.AreEqual(30, battle.Party[0].Hp);
            Assert.AreEqual(0, battle.Party[0].Shield, "새 라운드에 보호막 소멸");
        }

        [Test]
        public void HealDoesNotExceedMaxHp()
        {
            var battle = Start(new[] { Hero(hp: 25), Hero() }, new[] { Slime() });

            battle.SelectSkill(heal, battle.Party[0]);
            var events = battle.SubmitAnswer(Correct, Normal);

            Assert.AreEqual(5, events.Single(e => e.Type == BattleEventType.Heal).Amount);
            Assert.AreEqual(30, battle.Party[0].Hp);
        }

        [Test]
        public void FaintedPartyMemberIsSkipped()
        {
            var battle = Start(new[] { Hero(), Hero(hp: 0), Hero() }, new[] { Slime() });

            battle.SelectSkill(strike, battle.Enemies[0]);
            battle.SubmitAnswer(Wrong, Normal);

            Assert.AreSame(battle.Party[2], battle.CurrentActor);
        }

        [Test]
        public void VictoryWhenAllEnemiesFall()
        {
            var battle = Start(new[] { Hero(), Hero() }, new[] { Slime(hp: 10) });

            battle.SelectSkill(strike, battle.Enemies[0]);
            var events = battle.SubmitAnswer(Correct, Normal);

            Assert.AreEqual(BattlePhase.Victory, battle.Phase);
            Assert.IsTrue(events.Any(e => e.Type == BattleEventType.Defeated));
            Assert.AreEqual(BattleEventType.Victory, events.Last().Type);
        }

        [Test]
        public void DefeatWhenPartyFalls()
        {
            var battle = Start(new[] { Hero(hp: 1) }, new[] { Slime() });

            battle.SelectSkill(strike, battle.Enemies[0]);
            var events = battle.SubmitAnswer(Wrong, Normal);

            Assert.AreEqual(BattlePhase.Defeat, battle.Phase);
            Assert.AreEqual(BattleEventType.Defeat, events.Last().Type);
            Assert.Throws<InvalidOperationException>(() => battle.SelectSkill(strike, battle.Enemies[0]));
        }

        [Test]
        public void InvalidChoicesThrow()
        {
            var battle = Start(new[] { Hero(), Hero(hp: 0) }, new[] { Slime() });
            var foreignSkill = TestData.Skill("foreign", SkillKind.Damage, SkillTarget.SingleEnemy, 10);

            Assert.Throws<ArgumentException>(() => battle.SelectSkill(strike, null));
            Assert.Throws<ArgumentException>(() => battle.SelectSkill(strike, battle.Party[0]));
            Assert.Throws<ArgumentException>(() => battle.SelectSkill(heal, battle.Party[1]), "기절한 아군은 회복 대상 불가");
            Assert.Throws<ArgumentException>(() => battle.SelectSkill(foreignSkill, battle.Enemies[0]));
            Assert.Throws<InvalidOperationException>(() => battle.SubmitAnswer(Correct, Normal));
        }

        [Test]
        public void BattleDamageStaysOnMonsterAfterBattle()
        {
            var partyMonster = Hero();
            var battle = Start(new[] { partyMonster }, new[] { Slime() });

            battle.SelectSkill(strike, battle.Enemies[0]);
            battle.SubmitAnswer(Wrong, Normal);

            Assert.AreEqual(25, partyMonster.CurrentHp);
        }

        [Test]
        public void RewardIncludesBonusForCorrectAnswers()
        {
            var battle = Start(new[] { Hero(), Hero() }, new[] { new MonsterInstance(slime, 3, currentHp: 10) });

            battle.SelectSkill(strike, battle.Enemies[0]);
            battle.SubmitAnswer(Correct, Normal);
            var reward = battle.CalculateReward();

            // 5 x Lv3 + 정답 1개 x 2
            Assert.AreEqual(17, reward.Exp);
            Assert.AreEqual(9, reward.Gold);
        }
    }

    public class BattleRewardTests
    {
        [Test]
        public void DropsFollowChanceAndApplyToEveryone()
        {
            var always = TestData.Item("always");
            var never = TestData.Item("never");
            var enemy = TestData.Species("slime", new MonsterStats(10, 1, 1), new MonsterStats(0, 0, 0))
                .Set("expReward", 10).Set("goldReward", 4)
                .Set("drops", new List<ItemDrop> { new ItemDrop(always, 1f, 2), new ItemDrop(never, 0f) });
            var member = TestData.Species("hero", new MonsterStats(30, 10, 10), new MonsterStats(1, 1, 1));
            var party = new List<MonsterInstance> { new MonsterInstance(member, 1), new MonsterInstance(member, 1, currentHp: 0) };
            var inventory = new Inventory();

            var reward = BattleRewardCalculator.Calculate(
                new List<MonsterInstance> { new MonsterInstance(enemy, 1), new MonsterInstance(enemy, 1) },
                correctAnswers: 0, new BattleConfig(), new Random(1));
            var levels = BattleRewardCalculator.Apply(reward, party, inventory);

            Assert.AreEqual(20, reward.Exp);
            Assert.AreEqual(8, inventory.Gold);
            Assert.AreEqual(4, inventory.GetCount(always));
            Assert.AreEqual(0, inventory.GetCount(never));
            CollectionAssert.AreEqual(new[] { 1, 1 }, levels, "기절한 몬스터도 경험치를 받는다");
        }
    }
}
