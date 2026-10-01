using System;

namespace WordRPG.Battle
{
    public static class BattleFormulas
    {
        // 공격력 = 방어력이면 위력의 절반. 최소 1
        public static int Damage(int power, int attack, int defense, bool critical, BattleConfig config, Random rng)
        {
            double value = power * (double)attack / Math.Max(1, attack + defense);
            return Finish(value, critical, config, rng);
        }

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
