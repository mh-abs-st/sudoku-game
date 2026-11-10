using System.Numerics;

namespace SudokuGame.Engine;

/// <summary>
/// Backtracking solver. Cells are a flat array of 81 values in row order,
/// where 0 means empty and 1-9 are digits.
/// </summary>
public static class SudokuSolver
{
    private const int AllDigits = 0x1FF;

    /// <summary>
    /// Finds one solution. With <paramref name="rng"/> the digits are tried in
    /// random order, which turns an empty grid into a random full grid.
    /// </summary>
    public static int[]? Solve(ReadOnlySpan<int> cells, Random? rng = null)
    {
        var search = new Search(rng, limit: 1);
        if (!search.Load(cells))
        {
            return null;
        }

        search.Run();
        return search.First;
    }

    /// <summary>Counts solutions, stopping as soon as <paramref name="limit"/> is reached.</summary>
    public static int CountSolutions(ReadOnlySpan<int> cells, int limit = 2)
    {
        var search = new Search(rng: null, limit);
        if (!search.Load(cells))
        {
            return 0;
        }

        search.Run();
        return search.Found;
    }

    private sealed class Search
    {
        private readonly int[] _grid = new int[81];
        private readonly int[] _rows = new int[9];
        private readonly int[] _columns = new int[9];
        private readonly int[] _boxes = new int[9];
        private readonly Random? _rng;
        private readonly int _limit;

        public Search(Random? rng, int limit)
        {
            _rng = rng;
            _limit = limit;
        }

        public int Found { get; private set; }

        public int[]? First { get; private set; }

        /// <summary>Copies the starting cells in. Returns false if they already clash.</summary>
        public bool Load(ReadOnlySpan<int> cells)
        {
            if (cells.Length != 81)
            {
                throw new ArgumentException("A Sudoku grid has 81 cells.", nameof(cells));
            }

            for (int index = 0; index < 81; index++)
            {
                int digit = cells[index];
                if (digit == 0)
                {
                    continue;
                }

                if (digit < 1 || digit > 9 || (Used(index) & Bit(digit)) != 0)
                {
                    return false;
                }

                Place(index, digit);
            }

            return true;
        }

        public void Run()
        {
            // Always branch on the empty cell with the fewest candidates.
            int best = -1;
            int bestMask = 0;
            int bestCount = 10;

            for (int index = 0; index < 81; index++)
            {
                if (_grid[index] != 0)
                {
                    continue;
                }

                int mask = AllDigits & ~Used(index);
                int count = BitOperations.PopCount((uint)mask);
                if (count == 0)
                {
                    return;
                }

                if (count < bestCount)
                {
                    best = index;
                    bestMask = mask;
                    bestCount = count;
                    if (count == 1)
                    {
                        break;
                    }
                }
            }

            if (best < 0)
            {
                Found++;
                First ??= (int[])_grid.Clone();
                return;
            }

            Span<int> digits = stackalloc int[9];
            int total = 0;
            for (int digit = 1; digit <= 9; digit++)
            {
                if ((bestMask & Bit(digit)) != 0)
                {
                    digits[total++] = digit;
                }
            }

            if (_rng != null)
            {
                for (int i = total - 1; i > 0; i--)
                {
                    int j = _rng.Next(i + 1);
                    (digits[i], digits[j]) = (digits[j], digits[i]);
                }
            }

            for (int i = 0; i < total; i++)
            {
                Place(best, digits[i]);
                Run();
                Remove(best, digits[i]);

                if (Found >= _limit)
                {
                    return;
                }
            }
        }

        private static int Bit(int digit) => 1 << (digit - 1);

        private static int BoxOf(int index) => (index / 27) * 3 + (index % 9) / 3;

        private int Used(int index) => _rows[index / 9] | _columns[index % 9] | _boxes[BoxOf(index)];

        private void Place(int index, int digit)
        {
            int bit = Bit(digit);
            _grid[index] = digit;
            _rows[index / 9] |= bit;
            _columns[index % 9] |= bit;
            _boxes[BoxOf(index)] |= bit;
        }

        private void Remove(int index, int digit)
        {
            int bit = ~Bit(digit);
            _grid[index] = 0;
            _rows[index / 9] &= bit;
            _columns[index % 9] &= bit;
            _boxes[BoxOf(index)] &= bit;
        }
    }
}
