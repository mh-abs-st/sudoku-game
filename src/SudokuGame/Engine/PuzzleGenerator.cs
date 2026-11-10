namespace SudokuGame.Engine;

/// <summary>A generated puzzle: the starting cells and the full solution.</summary>
public sealed record Puzzle(int[] Cells, int[] Solution);

public static class PuzzleGenerator
{
    /// <summary>
    /// Builds a random full grid, then removes cells in mirrored pairs for as
    /// long as the puzzle still has exactly one solution.
    /// </summary>
    public static Puzzle Create(Difficulty difficulty, Random? rng = null)
    {
        rng ??= Random.Shared;

        int[] solution = SudokuSolver.Solve(new int[81], rng)
            ?? throw new InvalidOperationException("Could not build a full grid.");
        int[] cells = (int[])solution.Clone();

        int[] order = new int[81];
        for (int i = 0; i < 81; i++)
        {
            order[i] = i;
        }

        for (int i = 80; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        int target = difficulty.ClueTarget();
        int clues = 81;

        foreach (int index in order)
        {
            if (clues <= target)
            {
                break;
            }

            int mirror = 80 - index;
            if (cells[index] == 0)
            {
                continue;
            }

            int removed = index == mirror ? 1 : 2;
            if (clues - removed < target)
            {
                continue;
            }

            int first = cells[index];
            int second = cells[mirror];
            cells[index] = 0;
            cells[mirror] = 0;

            if (SudokuSolver.CountSolutions(cells, limit: 2) == 1)
            {
                clues -= removed;
            }
            else
            {
                cells[index] = first;
                cells[mirror] = second;
            }
        }

        return new Puzzle(cells, solution);
    }
}
