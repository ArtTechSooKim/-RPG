using System;
using WordRPG.Items;
using WordRPG.Words;

namespace WordRPG.Game
{
    public readonly struct DexProgress
    {
        public int Discovered { get; }
        public int Total { get; }
        public int Mastered { get; }

        public bool IsComplete => Total > 0 && Discovered >= Total;
        public float Ratio => Total == 0 ? 0f : (float)Discovered / Total;

        public DexProgress(int discovered, int total, int mastered)
        {
            Discovered = discovered;
            Total = total;
            Mastered = mastered;
        }
    }

    // 지역 도감 완성 보상이 지급된 기록 (결과 화면 표시용)
    public class DexCompletion
    {
        public WordDatabase Region { get; }
        public ItemData Keepsake { get; }
        public int Gold { get; }

        public DexCompletion(WordDatabase region)
        {
            Region = region;
            Keepsake = region.CompletionKeepsake;
            Gold = region.CompletionGold;
        }
    }

    // 단어 도감 규칙. '발견' = 한 번이라도 문제로 만난 단어 (맞히든 틀리든)
    public static class Dex
    {
        public static DexProgress GetProgress(WordDatabase region, VocabularyProgress vocabulary)
        {
            int discovered = 0, mastered = 0;
            foreach (var word in region.Words)
            {
                var level = vocabulary.GetLevel(word.Id);
                if (level > MasteryLevel.New) discovered++;
                if (level == MasteryLevel.Mastered) mastered++;
            }
            return new DexProgress(discovered, region.Words.Count, mastered);
        }

        // 학습 ★☆☆☆ ... 완전 숙련 ★★★★. 미발견은 빈 문자열
        public static string Stars(MasteryLevel level)
        {
            int filled = (int)level;
            return level == MasteryLevel.New ? "" : new string('★', filled) + new string('☆', 4 - filled);
        }

        public static string DescribeNextReview(WordProgress progress, DateTime nowUtc)
        {
            if (progress == null || progress.Level == MasteryLevel.New) return "";
            if (progress.InWrongNote) return "오답 노트 — 다시 도전해 보세요";
            if (progress.IsDue(nowUtc)) return "복습할 때가 됐어요";

            var left = progress.NextReviewUtc - nowUtc;
            if (left.TotalDays >= 1) return $"{(int)left.TotalDays}일 후 복습";
            if (left.TotalHours >= 1) return $"{(int)left.TotalHours}시간 후 복습";
            return $"{Math.Max(1, (int)left.TotalMinutes)}분 후 복습";
        }
    }
}
