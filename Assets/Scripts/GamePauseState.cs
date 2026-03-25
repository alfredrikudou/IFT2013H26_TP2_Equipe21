public static class GamePauseState
{
    public static bool IsPaused { get; private set; }

    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
    }
}

