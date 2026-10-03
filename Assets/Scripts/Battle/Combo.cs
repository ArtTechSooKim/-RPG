using System;

namespace WordRPG.Battle
{
    // 연속 정답 콤보 (사용자 아이디어, 2026-10-03): 정답을 이어서 맞히면 단계가 오르고 아군 기술 피해가 조금씩 늘어난다.
    //   2연속 Combo! → 3 Good! → 4 Very Good! → 5 Excellent! → 6 Outstanding! → 7연속부터 Exceptional!
    //   단계마다 피해 +BattleConfig.ComboBonusPerStep (기본 5%, 최대 +30%)
    // 오답·시간 초과면 0으로. 상처약은 문제가 없으므로 끊기지 않고, 전투가 끝나도 이어진다 (GameSession.ComboStreak)
    public static class Combo
    {
        public static readonly string[] Labels = { "Combo!", "Good!", "Very Good!", "Excellent!", "Outstanding!", "Exceptional!" };

        // 이 횟수만큼 연속으로 맞히면 첫 단계
        public const int FirstStreak = 2;

        public static int MaxStep => Labels.Length;

        // 0 = 콤보 아님, 1~6 = 단계
        public static int Step(int streak) => streak < FirstStreak ? 0 : Math.Min(streak - FirstStreak + 1, Labels.Length);

        public static string Label(int streak)
        {
            int step = Step(streak);
            return step > 0 ? Labels[step - 1] : null;
        }

        public static float DamageBonus(int streak, float bonusPerStep) => Step(streak) * bonusPerStep;
    }
}
