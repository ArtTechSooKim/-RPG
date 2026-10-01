using System;
using System.Collections.Generic;
using NUnit.Framework;
using WordRPG.Words;

namespace WordRPG.Tests
{
    public class WordSelectorTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        private List<WordEntry> words;
        private VocabularyProgress progress;
        private MasteryRules rules;

        [SetUp]
        public void SetUp()
        {
            words = TestData.SampleWords();
            progress = new VocabularyProgress();
            rules = new MasteryRules();
        }

        [Test]
        public void WrongNoteWordComesFirst()
        {
            progress.RecordAnswer("budget", false, T0, rules);
            var selector = new WordSelector();

            var picked = selector.SelectNext(words, progress, T0.AddSeconds(1), new Random(1));

            Assert.AreEqual("budget", picked.Id);
        }

        [Test]
        public void DueReviewComesBeforeNewWords()
        {
            progress.RecordAnswer("submit", true, T0, rules);
            var selector = new WordSelector();
            var later = T0 + rules.GetReviewInterval(MasteryLevel.Learning);

            var picked = selector.SelectNext(words, progress, later, new Random(1));

            Assert.AreEqual("submit", picked.Id);
        }

        [Test]
        public void RecentWordsAreNotRepeated()
        {
            var selector = new WordSelector(recentHistorySize: 4);
            var rng = new Random(7);
            var seen = new List<string>();

            for (int i = 0; i < 5; i++)
            {
                var picked = selector.SelectNext(words, progress, T0, rng);
                CollectionAssert.DoesNotContain(seen, picked.Id);
                seen.Add(picked.Id);
                progress.RecordAnswer(picked.Id, true, T0, rules);
            }
        }

        [Test]
        public void StopsIntroducingNewWordsWhenTooManyAreBeingLearned()
        {
            progress.RecordAnswer("abandon", true, T0, rules);
            progress.RecordAnswer("budget", true, T0, rules);
            var selector = new WordSelector(recentHistorySize: 0, maxWordsInLearning: 2);

            var picked = selector.SelectNext(words, progress, T0.AddSeconds(1), new Random(3));

            CollectionAssert.Contains(new[] { "abandon", "budget" }, picked.Id);
        }

        [Test]
        public void TinyPoolStillReturnsAWord()
        {
            var single = new List<WordEntry> { words[0] };
            var selector = new WordSelector(recentHistorySize: 4);
            var rng = new Random(1);

            Assert.AreEqual("abandon", selector.SelectNext(single, progress, T0, rng).Id);
            Assert.AreEqual("abandon", selector.SelectNext(single, progress, T0, rng).Id);
        }
    }
}
