using System;
using NUnit.Framework;
using WordRPG.Words;

namespace WordRPG.Tests
{
    public class VocabularyProgressTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        private MasteryRules rules;
        private VocabularyProgress progress;

        [SetUp]
        public void SetUp()
        {
            rules = new MasteryRules();
            progress = new VocabularyProgress();
        }

        // 단어를 복습 시기마다 맞혀서 원하는 단계까지 올린다. 반환: 마지막으로 답한 시각
        private DateTime RaiseTo(string id, MasteryLevel target)
        {
            var now = T0;
            progress.RecordAnswer(id, true, now, rules);
            while (progress.GetLevel(id) < target)
            {
                now = progress.Find(id).NextReviewUtc;
                progress.RecordAnswer(id, true, now, rules);
            }
            return now;
        }

        [Test]
        public void UnseenWordIsNew()
        {
            Assert.AreEqual(MasteryLevel.New, progress.GetLevel("abandon"));
            Assert.IsNull(progress.Find("abandon"));
        }

        [Test]
        public void FirstCorrectAnswerMovesNewToLearning()
        {
            var change = progress.RecordAnswer("abandon", true, T0, rules);

            Assert.AreEqual(MasteryLevel.New, change.Before);
            Assert.AreEqual(MasteryLevel.Learning, change.After);
            Assert.AreEqual(T0 + rules.GetReviewInterval(MasteryLevel.Learning), progress.Find("abandon").NextReviewUtc);
        }

        [Test]
        public void CorrectBeforeReviewTimeDoesNotPromote()
        {
            progress.RecordAnswer("abandon", true, T0, rules);
            var change = progress.RecordAnswer("abandon", true, T0.AddMinutes(1), rules);

            Assert.AreEqual(MasteryLevel.Learning, change.After);
            Assert.AreEqual(2, progress.Find("abandon").CorrectCount);
        }

        [Test]
        public void CorrectWhenDuePromotesOneLevel()
        {
            progress.RecordAnswer("abandon", true, T0, rules);
            var due = progress.Find("abandon").NextReviewUtc;

            var change = progress.RecordAnswer("abandon", true, due, rules);

            Assert.AreEqual(MasteryLevel.Reviewing, change.After);
            Assert.IsTrue(change.LeveledUp);
        }

        [Test]
        public void WrongAnswerDemotesAndGoesToWrongNote()
        {
            var last = RaiseTo("abandon", MasteryLevel.Proficient);
            var now = last.AddMinutes(1);

            var change = progress.RecordAnswer("abandon", false, now, rules);

            Assert.AreEqual(MasteryLevel.Reviewing, change.After);
            var p = progress.Find("abandon");
            Assert.IsTrue(p.InWrongNote);
            Assert.IsTrue(p.IsDue(now), "틀린 단어는 바로 다시 출제 대상");
        }

        [Test]
        public void WrongAnswerNeverDropsBelowLearning()
        {
            progress.RecordAnswer("abandon", false, T0, rules);
            progress.RecordAnswer("abandon", false, T0, rules);

            Assert.AreEqual(MasteryLevel.Learning, progress.GetLevel("abandon"));
            Assert.AreEqual(2, progress.Find("abandon").WrongCount);
        }

        [Test]
        public void CorrectRetryAfterWrongClearsWrongNoteWithoutPromotion()
        {
            RaiseTo("abandon", MasteryLevel.Reviewing);
            progress.RecordAnswer("abandon", false, T0.AddDays(5), rules); // → 학습

            var change = progress.RecordAnswer("abandon", true, T0.AddDays(5), rules);

            Assert.AreEqual(MasteryLevel.Learning, change.After);
            Assert.IsFalse(progress.Find("abandon").InWrongNote);
        }

        [Test]
        public void MasteredIsTheCap()
        {
            var last = RaiseTo("abandon", MasteryLevel.Mastered);
            var due = progress.Find("abandon").NextReviewUtc;
            Assert.Greater(due, last);

            var change = progress.RecordAnswer("abandon", true, due, rules);

            Assert.AreEqual(MasteryLevel.Mastered, change.After);
            Assert.IsFalse(change.LeveledUp);
        }
    }
}
