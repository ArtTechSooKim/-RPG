using System;

namespace WordRPG.Battle
{
    public static class BattleFormulas
    {
        // 공격력 = 방어력이면 위력의 절반. 최소 1. bonus: 연속 정답 콤보 추가 피해 (0.1 = +10%),
        // multiplier: 공격 기술 강도 배율 (단어 여러 개를 연속으로 맞혔을 때 1.2·1.5·2)
        public static int Damage(int power, int attack, int defense, bool critical, BattleConfig config, Random rng,
            float bonus = 0f, float multiplier = 1f)
        {
            return Finish(BaseDamage(power, attack, defense, bonus, multiplier), critical, config, rng);
        }

        // 랜덤·크리티컬을 빼고 계산한 피해 (강도 고르기 화면의 '예상 피해')
        public static int ExpectedDamage(int power, int attack, int defense, float bonus = 0f, float multiplier = 1f) =>
            Math.Max(1, (int)Math.Round(BaseDamage(power, attack, defense, bonus, multiplier)));

        private static double BaseDamage(int power, int attack, int defense, float bonus, float multiplier) =>
            power * (double)attack / Math.Max(1, attack + defense) * (1.0 + bonus) * multiplier;

        public static int Heal(int power, int attack, bool critical, BattleConfig config, Random rng)
        {
            return Finish(power + attack / 2.0, critical, config, rng);
        }

        public static int Shield(int power, int defense, bool critical, BattleConfig config, Random rng)
        {
            return Finish(power + defense / 2.0, critical, config, rng);
        }

        private static int Finish(double value, bool critical, BattleConfig config, Random rng)
        {
            if (critical) value *= config.CriticalMultiplier;
            if (config.Variance > 0f) value *= 1.0 + (rng.NextDouble() * 2.0 - 1.0) * config.Variance;
            return Math.Max(1, (int)Math.Round(value));
        }
    }
}
