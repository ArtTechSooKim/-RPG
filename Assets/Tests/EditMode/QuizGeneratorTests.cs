using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WordRPG.Words;

namespace WordRPG.Tests
{
    public class QuizGeneratorTests
    {
        private List<WordEntry> words;

        [SetUp]
        public void SetUp() => words = TestData.SampleWords();

        private WordEntry Word(string id) => words.First(w => w.Id == id);

        [Test]
        public void EnglishToMeaningHasFourDistinctChoicesIncludingAnswer()
        {
            var q = QuizGenerator.Create(Word("abandon"), QuizDirection.EnglishToMeaning, words, new Random(1));

            Assert.AreEqual("abandon", q.Prompt);
            Assert.AreEqual(4, q.Choices.Count);
            Assert.AreEqual(4, q.Choices.Distinct().Count());
            Assert.AreEqual("버리다, 포기하다", q.CorrectAnswer);
            Assert.IsTrue(q.IsCorrect(q.CorrectIndex));
        }

        [Test]
        public void MeaningToEnglishAsksMeaningAndOffersEnglishWords()
        {
            var q = QuizGenerator.Create(Word("budget"), QuizDirection.MeaningToEnglish, words, new Random(1));

            Assert.AreEqual("예산", q.Prompt);
            Assert.AreEqual("budget", q.CorrectAnswer);
            var english = words.Select(w => w.English).ToList();
            foreach (var choice in q.Choices) CollectionAssert.Contains(english, choice);
        }

        [Test]
        public void WordWithOverlappingMeaningIsNeverADistractor()
        {
            // acquire("얻다, 획득하다")와 obtain("손에 넣다, 얻다")이 같이 나오면 정답이 둘
            for (int seed = 0; seed < 200; seed++)
            {
                var q = QuizGenerator.Create(Word("acquire"), QuizDirection.MeaningToEnglish, words, new Random(seed));
                CollectionAssert.DoesNotContain(q.Choices, "obtain", $"seed {seed}");
            }
        }

        [Test]
        public void ParenthesisNoteIsIgnoredWhenComparingMeanings()
        {
            Assert.IsTrue(QuizGenerator.MeaningsOverlap(Word("affordable"), Word("reasonable")));
            Assert.IsFalse(QuizGenerator.MeaningsOverlap(Word("budget"), Word("deadline")));
        }

        [Test]
        public void PrefersSamePartOfSpeech()
        {
            // 명사는 budget(정답), deadline, colleague 3개뿐 → 셋 다 보기에 들어가고 나머지 1개만 다른 품사
            var q = QuizGenerator.Create(Word("budget"), QuizDirection.MeaningToEnglish, words, new Random(5));
            var nouns = words.Where(w => w.PartOfSpeech == "n").Select(w => w.English).ToList();
            int nounChoices = q.Choices.Count(c => nouns.Contains(c));
            Assert.AreEqual(Math.Min(4, nouns.Count), nounChoices);
        }

        [Test]
        public void SmallPoolGivesFewerChoices()
        {
            var pool = words.Take(2).ToList(); // abandon, acquire
            var q = QuizGenerator.Create(pool[0], QuizDirection.EnglishToMeaning, pool, new Random(1));

            Assert.AreEqual(2, q.Choices.Count);
            Assert.AreEqual("버리다, 포기하다", q.CorrectAnswer);
        }
    }

    public class WordQuizServiceTests
    {
        [Test]
        public void NewWordIsAskedEnglishToMeaningAndFlaggedNew()
        {
            var service = new WordQuizService(TestData.SampleWords(), new VocabularyProgress(), new MasteryRules(), new Random(1));

            var q = service.NextQuestion(QuizDirection.MeaningToEnglish);

            Assert.IsTrue(q.IsNewWord);
            Assert.AreEqual(QuizDirection.EnglishToMeaning, q.Direction);
        }

        [Test]
        public void SubmitAnswerRecordsProgress()
        {
            var progress = new VocabularyProgress();
            var service = new WordQuizService(TestData.SampleWords(), progress, new MasteryRules(), new Random(1));
            var q = service.NextQuestion(QuizDirection.EnglishToMeaning);

            var change = service.SubmitAnswer(q, true);

            Assert.AreEqual(MasteryLevel.Learning, change.After);
            Assert.AreEqual(MasteryLevel.Learning, progress.GetLevel(q.Word.Id));
        }

        [Test]
        public void SeenWordUsesSkillDirection()
        {
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var words = TestData.SampleWords().Take(1).ToList();
            var progress = new VocabularyProgress();
            progress.RecordAnswer(words[0].Id, true, now, new MasteryRules());
            var service = new WordQuizService(words, progress, new MasteryRules(), new Random(1), utcNow: () => now);

            var q = service.NextQuestion(QuizDirection.MeaningToEnglish);

            Assert.IsFalse(q.IsNewWord);
            Assert.AreEqual(QuizDirection.MeaningToEnglish, q.Direction);
        }
    }
}
