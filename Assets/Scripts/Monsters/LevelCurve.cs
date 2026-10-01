namespace WordRPG.Monsters
{
    public static class LevelCurve
    {
        public const int MaxLevel = 30;

        // 레벨 L에서 L+1로 가는 데 필요한 경험치. Lv1→2: 20, Lv4→5: 50
        public static int ExpToNextLevel(int level) => 10 + 10 * level;
    }
}
