using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WordRPG.Words
{
    // 4지선다 문제 생성. 오답 보기는 같은 품사를 우선으로 고르고,
    // 정답과 뜻이 겹치는 단어(acquire/obtain = "얻다")는 정답이 둘이 되므로 제외한다
    public static class QuizGenerator
    {
        public const int DefaultChoiceCount = 4;

        private static readonly Regex ParenthesisNote = new Regex(@"\([^)]*\)");

        public static QuizQuestion Create(WordEntry target, QuizDirection direction, IReadOnlyList<WordEntry> pool,
            Random rng, bool isNewWord = false, int choiceCount = DefaultChoiceCount)
        {
            var samePos = new List<WordEntry>();
            var otherPos = new List<WordEntry>();
            foreach (var candidate in pool)
            {
                if (candidate.Id == target.Id) continue;
                if (string.Equals(candidate.English, target.English, StringComparison.OrdinalIgnoreCase)) continue;
                if (MeaningsOverlap(candidate, target)) continue;

                if (candidate.PartOfSpeech == target.PartOfSpeech) samePos.Add(candidate);
                else otherPos.Add(candidate);
            }
            Shuffle(samePos, rng);
            Shuffle(otherPos, rng);

            var distractors = new List<WordEntry>();
            foreach (var candidate in Concat(samePos, otherPos))
            {
                if (distractors.Count >= choiceCount - 1) break;
                if (distractors.Exists(d => MeaningsOverlap(d, candidate))) continue;
                distractors.Add(candidate);
            }

            var choiceWords = new List<WordEntry>(distractors) { target };
            Shuffle(choiceWords, rng);

            var choices = new List<string>(choiceWords.Count);
            int correctIndex = 0;
            for (int i = 0; i < choiceWords.Count; i++)
            {
                choices.Add(direction == QuizDirection.EnglishToMeaning ? choiceWords[i].Meaning : choiceWords[i].English);
                if (choiceWords[i] == target) correctIndex = i;
            }

            string prompt = direction == QuizDirection.EnglishToMeaning ? target.English : target.Meaning;
            return new QuizQuestion(target, direction, isNewWord, prompt, choices, correctIndex);
        }

        // 뜻 조각 중 하나라도 같으면 겹침. "(가격이) 적당한"과 "적당한"도 같은 뜻으로 본다
        public static bool MeaningsOverlap(WordEntry a, WordEntry b)
        {
            var partsA = a.GetMeaningParts();
            var partsB = b.GetMeaningParts();
            foreach (var pa in partsA)
            {
                var na = Normalize(pa);
                if (na.Length == 0) continue;
                foreach (var pb in partsB)
                {
                    if (na == Normalize(pb)) return true;
                }
            }
            return false;
        }

        private static string Normalize(string meaningPart)
        {
            return ParenthesisNote.Replace(meaningPart, "").Replace(" ", "");
        }

        private static IEnumerable<WordEntry> Concat(List<WordEntry> first, List<WordEntry> second)
        {
            foreach (var w in first) yield return w;
            foreach (var w in second) yield return w;
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
