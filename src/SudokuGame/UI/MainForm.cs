using System.Drawing.Printing;
using SudokuGame.Engine;

namespace SudokuGame.UI;

/// <summary>The game window: menu, board, number pad and status bar.</summary>
internal sealed class MainForm : Form
{
    private const string FileFilter = "Sudoku games (*.sud)|*.sud|All files (*.*)|*.*";

    private readonly AppSettings _settings = AppSettings.Load();
    private readonly BoardControl _board = new() { Dock = DockStyle.Fill };
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Dictionary<Difficulty, ToolStripMenuItem> _levelItems = new();
    private readonly ToolStripMenuItem _undoItem;
    private readonly ToolStripMenuItem _redoItem;

    private Game _game;
    private bool _finished;

    public MainForm()
    {
        Text = "Sudoku";
        ClientSize = new Size(540, 680);
        MinimumSize = new Size(380, 500);
        StartPosition = FormStartPosition.CenterScreen;

        _undoItem = MenuItem("&Undo", Keys.Control | Keys.Z, Undo);
        _redoItem = MenuItem("&Redo", Keys.Control | Keys.Y, Redo);

        var statusBar = new StatusStrip();
        statusBar.Items.Add(_status);

        // Docking is applied from the last control added to the first, so the
        // board (Fill) goes in first and takes whatever space is left over.
        Controls.Add(_board);
        Controls.Add(BuildNumberPad());
        Controls.Add(statusBar);
        MenuStrip menu = BuildMenu();
        Controls.Add(menu);
        MainMenuStrip = menu;

        _board.DigitEntered += EnterDigit;

        _game = Game.Create(_settings.Difficulty);
        ShowGame("New " + _settings.Difficulty.ToString().ToLowerInvariant() + " puzzle.");
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _board.Focus();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _settings.Save();
        base.OnFormClosed(e);
    }

    private static ToolStripMenuItem MenuItem(string text, Keys shortcut, Action action)
    {
        var item = new ToolStripMenuItem(text) { ShortcutKeys = shortcut };
        item.Click += (_, _) => action();
        return item;
    }

    private MenuStrip BuildMenu()
    {
        var game = new ToolStripMenuItem("&Game");
        game.DropDownItems.Add(MenuItem("&New game", Keys.Control | Keys.N, NewGame));
        game.DropDownItems.Add(new ToolStripSeparator());
        game.DropDownItems.Add(MenuItem("&Open...", Keys.Control | Keys.O, OpenGame));
        game.DropDownItems.Add(MenuItem("&Save...", Keys.Control | Keys.S, SaveGame));
        game.DropDownItems.Add(new ToolStripSeparator());
        game.DropDownItems.Add(MenuItem("&Print puzzles...", Keys.Control | Keys.P, PrintPuzzles));
        game.DropDownItems.Add(new ToolStripSeparator());
        game.DropDownItems.Add(MenuItem("E&xit", Keys.None, Close));

        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.Add(_undoItem);
        edit.DropDownItems.Add(_redoItem);

        var level = new ToolStripMenuItem("&Level");
        foreach (Difficulty difficulty in Enum.GetValues<Difficulty>())
        {
            Difficulty choice = difficulty;
            ToolStripMenuItem item = MenuItem(choice.ToString(), Keys.None, () => ChangeLevel(choice));
            item.Checked = choice == _settings.Difficulty;
            _levelItems[choice] = item;
            level.DropDownItems.Add(item);
        }

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(MenuItem("&Hint", Keys.Control | Keys.H, GiveHint));
        help.DropDownItems.Add(MenuItem("&Solve puzzle", Keys.None, SolvePuzzle));

        var menu = new MenuStrip();
        menu.Items.AddRange(new ToolStripItem[] { game, edit, level, help });
        return menu;
    }

    private Control BuildNumberPad()
    {
        var pad = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            ColumnCount = 10,
            RowCount = 1,
            Padding = new Padding(6, 4, 6, 6),
        };
        pad.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        for (int column = 0; column < 10; column++)
        {
            pad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f));

            int digit = column == 9 ? 0 : column + 1;
            var button = new Button
            {
                Text = digit == 0 ? "Erase" : digit.ToString(),
                Dock = DockStyle.Fill,
                TabStop = false,
                Font = new Font("Segoe UI", digit == 0 ? 9f : 13f, FontStyle.Bold),
                Margin = new Padding(2),
            };
            button.Click += (_, _) =>
            {
                EnterDigit(digit);
                _board.Focus();
            };
            pad.Controls.Add(button, column, 0);
        }

        return pad;
    }

    private void ShowGame(string message)
    {
        _finished = _game.IsSolved();
        _board.Game = _game;
        RefreshView(message);
    }

    private void RefreshView(string message)
    {
        _undoItem.Enabled = _game.CanUndo;
        _redoItem.Enabled = _game.CanRedo;
        _status.Text = message;
        _board.Invalidate();
    }

    private void EnterDigit(int digit)
    {
        int index = _board.SelectedIndex;
        if (index < 0)
        {
            RefreshView("Select a cell first.");
            return;
        }

        if (_game.IsGiven(index))
        {
            RefreshView("That cell is part of the puzzle and cannot be changed.");
            return;
        }

        if (!_game.SetCell(index, digit))
        {
            return;
        }

        RefreshView(_game.HasConflict(index)
            ? "That digit already appears in the same row, column or box."
            : string.Empty);
        CheckForWin();
    }

    private void CheckForWin()
    {
        if (_finished || !_game.IsSolved())
        {
            return;
        }

        _finished = true;
        RefreshView("Solved!");

        DialogResult answer = MessageBox.Show(
            this,
            "Congratulations, you solved it!" + Environment.NewLine + "Start a new game?",
            "Sudoku",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);
        if (answer == DialogResult.Yes)
        {
            NewGame();
        }
    }

    private void NewGame()
    {
        Cursor = Cursors.WaitCursor;
        try
        {
            _game = Game.Create(_settings.Difficulty);
        }
        finally
        {
            Cursor = Cursors.Default;
        }

        ShowGame("New " + _settings.Difficulty.ToString().ToLowerInvariant() + " puzzle.");
    }

    private void ChangeLevel(Difficulty difficulty)
    {
        if (difficulty == _settings.Difficulty)
        {
            return;
        }

        _settings.Difficulty = difficulty;
        foreach ((Difficulty key, ToolStripMenuItem item) in _levelItems)
        {
            item.Checked = key == difficulty;
        }

        DialogResult answer = MessageBox.Show(
            this,
            "The level changed. Start a new game now?",
            "Sudoku",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer == DialogResult.Yes)
        {
            NewGame();
        }
    }

    private void Undo()
    {
        int index = _game.Undo();
        if (index >= 0)
        {
            _board.SelectedIndex = index;
            RefreshView(string.Empty);
        }
    }

    private void Redo()
    {
        int index = _game.Redo();
        if (index >= 0)
        {
            _board.SelectedIndex = index;
            RefreshView(string.Empty);
            CheckForWin();
        }
    }

    private void GiveHint()
    {
        int index = _game.Hint(_board.SelectedIndex);
        if (index < 0)
        {
            RefreshView("There is nothing left to hint.");
            return;
        }

        _board.SelectedIndex = index;
        _finished = _game.IsSolved();
        RefreshView(_finished ? "Solved with a little help." : "Filled in one cell for you.");
    }

    private void SolvePuzzle()
    {
        _game.RevealSolution();
        _finished = true;
        RefreshView("Here is the solution.");
    }

    private void SaveGame()
    {
        using var dialog = new SaveFileDialog { Filter = FileFilter, DefaultExt = "sud" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            File.WriteAllText(dialog.FileName, _game.Serialize());
            RefreshView("Game saved.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            ShowError("The game could not be saved." + Environment.NewLine + error.Message);
        }
    }

    private void OpenGame()
    {
        using var dialog = new OpenFileDialog { Filter = FileFilter };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _game = Game.Deserialize(File.ReadAllText(dialog.FileName));
            ShowGame("Game loaded.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or FormatException)
        {
            ShowError("The game could not be opened." + Environment.NewLine + error.Message);
        }
    }

    private void PrintPuzzles()
    {
        using var document = new PrintDocument { DocumentName = "Sudoku puzzles" };
        using var dialog = new PrintDialog { Document = document, UseEXDialog = true };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var puzzles = new List<Puzzle>();
        for (int i = 0; i < 4; i++)
        {
            puzzles.Add(PuzzleGenerator.Create(_settings.Difficulty));
        }

        document.PrintPage += (_, e) =>
        {
            if (e.Graphics != null)
            {
                PuzzlePrinter.DrawPage(e.Graphics, e.MarginBounds, puzzles);
            }

            e.HasMorePages = false;
        };

        try
        {
            document.Print();
            RefreshView("Sent four puzzles to the printer.");
        }
        catch (Exception error) when (error is InvalidPrinterException or System.ComponentModel.Win32Exception)
        {
            ShowError("Printing failed." + Environment.NewLine + error.Message);
        }
    }

    private void ShowError(string message)
    {
        MessageBox.Show(this, message, "Sudoku", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
