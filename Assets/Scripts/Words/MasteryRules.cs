using System;
using UnityEngine;

namespace WordRPG.Words
{
    // 숙련도별 다음 복습까지의 간격 (라이트너 상자 방식). 플레이테스트로 조정할 값들
    [Serializable]
    public class MasteryRules
    {
        [SerializeField] private int learningIntervalMinutes = 5;
        [SerializeField] private int reviewingIntervalMinutes = 60 * 24;
        [SerializeField] private int proficientIntervalMinutes = 60 * 24 * 3;
        [SerializeField] private int masteredIntervalMinutes = 60 * 24 * 7;

        public TimeSpan GetReviewInterval(MasteryLevel level)
        {
            switch (level)
            {
                case MasteryLevel.Learning: return TimeSpan.FromMinutes(learningIntervalMinutes);
                case MasteryLevel.Reviewing: return TimeSpan.FromMinutes(reviewingIntervalMinutes);
                case MasteryLevel.Proficient: return TimeSpan.FromMinutes(proficientIntervalMinutes);
                case MasteryLevel.Mastered: return TimeSpan.FromMinutes(masteredIntervalMinutes);
                default: return TimeSpan.Zero;
            }
        }
    }
}
