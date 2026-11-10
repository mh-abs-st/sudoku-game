namespace SudokuGame.Engine;

public enum Difficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3,
    Expert = 4,
}

public static class DifficultyExtensions
{
    /// <summary>How many filled cells a new puzzle of this level aims to keep.</summary>
    public static int ClueTarget(this Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => 42,
        Difficulty.Medium => 34,
        Difficulty.Hard => 28,
        Difficulty.Expert => 24,
        _ => 42,
    };
}
