public static class SD_RunResults
{
    public static bool HasResults { get; private set; }
    public static bool Victory { get; private set; }
    public static int CompletedWaves { get; private set; }
    public static int TotalWaves { get; private set; }
    public static float RemainingHealth { get; private set; }

    public static int Points => CompletedWaves * 2;

    public static void Save(
        bool victory,
        int completedWaves,
        int totalWaves,
        float remainingHealth)
    {
        Victory = victory;
        CompletedWaves = completedWaves;
        TotalWaves = totalWaves;
        RemainingHealth = remainingHealth;
        HasResults = true;
    }

    public static void Reset()
    {
        HasResults = false;
        Victory = false;
        CompletedWaves = 0;
        TotalWaves = 0;
        RemainingHealth = 0f;
    }
}