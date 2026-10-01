using System.Collections.Generic;

namespace WordRPG.Words
{
    public enum QuizDirection
    {
        EnglishToMeaning, // "abandon" → 뜻 고르기 (쉬움)
        MeaningToEnglish  // "버리다, 포기하다" → 영단어 고르기 (어려움)
    }

    public class QuizQuestion
    {
        public WordEntry Word { get; }
        public QuizDirection Direction { get; }
        public bool IsNewWord { get; } // true면 UI가 문제 전에 '새 단어' 카드부터 보여준다
        public string Prompt { get; }
        public IReadOnlyList<string> Choices { get; }
        public int CorrectIndex { get; }

        public string CorrectAnswer => Choices[CorrectIndex];

        public QuizQuestion(WordEntry word, QuizDirection direction, bool isNewWord, string prompt,
            IReadOnlyList<string> choices, int correctIndex)
        {
            Word = word;
            Direction = direction;
            IsNewWord = isNewWord;
            Prompt = prompt;
            Choices = choices;
            CorrectIndex = correctIndex;
        }

        public bool IsCorrect(int choiceIndex) => choiceIndex == CorrectIndex;
    }
}
