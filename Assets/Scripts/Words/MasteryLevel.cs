namespace WordRPG.Words
{
    // PRD 5단계 숙련도
    public enum MasteryLevel
    {
        New = 0,        // 처음 만남 (아직 본 적 없음)
        Learning = 1,   // 학습
        Reviewing = 2,  // 복습
        Proficient = 3, // 숙련
        Mastered = 4    // 완전 숙련
    }

    public static class MasteryLevelExtensions
    {
        public static string DisplayName(this MasteryLevel level)
        {
            switch (level)
            {
                case MasteryLevel.New: return "처음 만남";
                case MasteryLevel.Learning: return "학습";
                case MasteryLevel.Reviewing: return "복습";
                case MasteryLevel.Proficient: return "숙련";
                case MasteryLevel.Mastered: return "완전 숙련";
                default: return level.ToString();
            }
        }
    }
}
