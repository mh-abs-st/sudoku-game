using System.Text;

namespace SudokuGame.Engine;

/// <summary>
/// One game in progress: the fixed starting cells, what the player has
/// entered, and the undo history.
/// </summary>
public sealed class Game
{
    private const string FileHeader = "SUDOKU 1";

    private readonly int[] _givens;
    private readonly int[] _values;
    private readonly int[] _solution;
    private readonly bool[] _hinted = new bool[81];
    private readonly Stack<Move> _undo = new();
    private readonly Stack<Move> _redo = new();

    private Game(int[] givens, int[] values, int[] solution, Difficulty difficulty)
    {
        _givens = (int[])givens.Clone();
        _values = (int[])values.Clone();
        _solution = (int[])solution.Clone();
        Difficulty = difficulty;
    }

    private readonly record struct Move(int Index, int Before, int After);

    public Difficulty Difficulty { get; }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public static Game Create(Difficulty difficulty, Random? rng = null)
    {
        Puzzle puzzle = PuzzleGenerator.Create(difficulty, rng);
        return new Game(puzzle.Cells, puzzle.Cells, puzzle.Solution, difficulty);
    }

    public int ValueAt(int index) => _values[index];

    public bool IsGiven(int index) => _givens[index] != 0;

    public bool IsHinted(int index) => _hinted[index];

    /// <summary>True when another cell in the same row, column or box holds the same digit.</summary>
    public bool HasConflict(int index)
    {
        int digit = _values[index];
        if (digit == 0)
        {
            return false;
        }

        int row = index / 9;
        int column = index % 9;
        int box = (row / 3) * 3 + column / 3;

        for (int other = 0; other < 81; other++)
        {
            if (other == index || _values[other] != digit)
            {
                continue;
            }

            int otherRow = other / 9;
            int otherColumn = other % 9;
            if (otherRow == row || otherColumn == column || (otherRow / 3) * 3 + otherColumn / 3 == box)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsSolved()
    {
        for (int index = 0; index < 81; index++)
        {
            if (_values[index] == 0 || HasConflict(index))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Enters a digit (0 clears). Returns false for a fixed cell or no change.</summary>
    public bool SetCell(int index, int digit)
    {
        if (digit < 0 || digit > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(digit));
        }

        if (IsGiven(index) || _values[index] == digit)
        {
            return false;
        }

        _undo.Push(new Move(index, _values[index], digit));
        _redo.Clear();
        _values[index] = digit;
        _hinted[index] = false;
        return true;
    }

    /// <summary>Reverts the last entry. Returns the changed cell, or -1.</summary>
    public int Undo()
    {
        if (_undo.Count == 0)
        {
            return -1;
        }

        Move move = _undo.Pop();
        _redo.Push(move);
        _values[move.Index] = move.Before;
        _hinted[move.Index] = false;
        return move.Index;
    }

    /// <summary>Reapplies the last undone entry. Returns the changed cell, or -1.</summary>
    public int Redo()
    {
        if (_redo.Count == 0)
        {
            return -1;
        }

        Move move = _redo.Pop();
        _undo.Push(move);
        _values[move.Index] = move.After;
        _hinted[move.Index] = false;
        return move.Index;
    }

    /// <summary>
    /// Fills one cell with its correct digit: the preferred cell if it is
    /// empty or wrong, otherwise a random one. Returns the cell, or -1.
    /// </summary>
    public int Hint(int preferredIndex, Random? rng = null)
    {
        var open = new List<int>();
        for (int index = 0; index < 81; index++)
        {
            if (!IsGiven(index) && _values[index] != _solution[index])
            {
                open.Add(index);
            }
        }

        if (open.Count == 0)
        {
            return -1;
        }

        int target = open.Contains(preferredIndex)
            ? preferredIndex
            : open[(rng ?? Random.Shared).Next(open.Count)];

        SetCell(target, _solution[target]);
        _hinted[target] = true;
        return target;
    }

    /// <summary>Fills in the whole solution and drops the undo history.</summary>
    public void RevealSolution()
    {
        for (int index = 0; index < 81; index++)
        {
            if (_values[index] != _solution[index])
            {
                _values[index] = _solution[index];
                _hinted[index] = true;
            }
        }

        _undo.Clear();
        _redo.Clear();
    }

    public string Serialize()
    {
        var text = new StringBuilder();
        text.AppendLine(FileHeader);
        text.AppendLine("level=" + (int)Difficulty);
        text.AppendLine("givens=" + string.Concat(_givens));
        text.AppendLine("values=" + string.Concat(_values));
        return text.ToString();
    }

    /// <summary>Reads a saved game. Throws <see cref="FormatException"/> if the text is not one.</summary>
    public static Game Deserialize(string text)
    {
        string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0 || lines[0] != FileHeader)
        {
            throw new FormatException("This is not a saved Sudoku game.");
        }

        var fields = new Dictionary<string, string>();
        foreach (string line in lines.Skip(1))
        {
            int split = line.IndexOf('=');
            if (split > 0)
            {
                fields[line[..split]] = line[(split + 1)..];
            }
        }

        if (!fields.TryGetValue("givens", out string? givensText) ||
            !fields.TryGetValue("values", out string? valuesText))
        {
            throw new FormatException("The saved game is missing its grid.");
        }

        int[] givens = ParseGrid(givensText);
        int[] values = ParseGrid(valuesText);
        for (int index = 0; index < 81; index++)
        {
            if (givens[index] != 0 && values[index] != givens[index])
            {
                throw new FormatException("The saved game does not match its starting grid.");
            }
        }

        int[] solution = SudokuSolver.Solve(givens)
            ?? throw new FormatException("The saved puzzle has no solution.");

        var difficulty = Difficulty.Easy;
        if (fields.TryGetValue("level", out string? levelText) &&
            int.TryParse(levelText, out int level) &&
            Enum.IsDefined(typeof(Difficulty), level))
        {
            difficulty = (Difficulty)level;
        }

        return new Game(givens, values, solution, difficulty);
    }

    private static int[] ParseGrid(string text)
    {
        if (text.Length != 81)
        {
            throw new FormatException("A saved grid must have 81 cells.");
        }

        int[] cells = new int[81];
        for (int index = 0; index < 81; index++)
        {
            char symbol = text[index];
            if (symbol < '0' || symbol > '9')
            {
                throw new FormatException("A saved grid may only contain the digits 0-9.");
            }

            cells[index] = symbol - '0';
        }

        return cells;
    }
}
