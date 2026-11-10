using System.ComponentModel;
using System.Drawing.Drawing2D;
using SudokuGame.Engine;

namespace SudokuGame.UI;

/// <summary>Draws the 9x9 grid and turns mouse and keyboard input into cell edits.</summary>
internal sealed class BoardControl : Control
{
    private const int Inset = 8;

    private static readonly Color GivenBack = Color.FromArgb(232, 234, 238);
    private static readonly Color PeerBack = Color.FromArgb(240, 246, 255);
    private static readonly Color SelectedBack = Color.FromArgb(187, 222, 251);
    private static readonly Color EntryInk = Color.FromArgb(21, 101, 192);
    private static readonly Color HintInk = Color.FromArgb(123, 31, 162);
    private static readonly Color ConflictInk = Color.FromArgb(198, 40, 40);

    private Game? _game;
    private int _selected = -1;

    public BoardControl()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        TabStop = true;
        BackColor = Color.White;
    }

    /// <summary>Raised when the player types a digit; 0 means erase.</summary>
    public event Action<int>? DigitEntered;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Game? Game
    {
        get => _game;
        set
        {
            _game = value;
            _selected = -1;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selected;
        set
        {
            _selected = value;
            Invalidate();
        }
    }

    protected override bool IsInputKey(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        return key is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        (float left, float top, float cell) = GridLayout();
        if (cell <= 0)
        {
            return;
        }

        int column = (int)Math.Floor((e.X - left) / cell);
        int row = (int)Math.Floor((e.Y - top) / cell);
        if (row >= 0 && row < 9 && column >= 0 && column < 9)
        {
            SelectedIndex = row * 9 + column;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Control || e.Alt)
        {
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Left:
                MoveSelection(0, -1);
                break;
            case Keys.Right:
                MoveSelection(0, 1);
                break;
            case Keys.Up:
                MoveSelection(-1, 0);
                break;
            case Keys.Down:
                MoveSelection(1, 0);
                break;
            case Keys.Delete:
            case Keys.Back:
            case Keys.Space:
                DigitEntered?.Invoke(0);
                break;
            default:
                if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
                {
                    DigitEntered?.Invoke(e.KeyCode - Keys.D0);
                }
                else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
                {
                    DigitEntered?.Invoke(e.KeyCode - Keys.NumPad0);
                }
                else
                {
                    return;
                }

                break;
        }

        e.Handled = true;
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        (float left, float top, float cell) = GridLayout();
        if (cell < 12)
        {
            return;
        }

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        float size = cell * 9;

        if (_game != null)
        {
            PaintCells(g, left, top, cell);
        }

        using var thin = new Pen(Color.FromArgb(176, 182, 190), 1f);
        using var thick = new Pen(Color.FromArgb(38, 50, 56), 2f);
        for (int line = 0; line <= 9; line++)
        {
            Pen pen = line % 3 == 0 ? thick : thin;
            float offset = line * cell;
            g.DrawLine(pen, left + offset, top, left + offset, top + size);
            g.DrawLine(pen, left, top + offset, left + size, top + offset);
        }
    }

    private void PaintCells(Graphics g, float left, float top, float cell)
    {
        Game game = _game!;

        using var givenFont = new Font("Segoe UI", cell * 0.5f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var entryFont = new Font("Segoe UI", cell * 0.5f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        int selectedRow = _selected < 0 ? -1 : _selected / 9;
        int selectedColumn = _selected < 0 ? -1 : _selected % 9;
        int selectedDigit = _selected < 0 ? 0 : game.ValueAt(_selected);

        for (int index = 0; index < 81; index++)
        {
            int row = index / 9;
            int column = index % 9;
            var bounds = new RectangleF(left + column * cell, top + row * cell, cell, cell);
            int digit = game.ValueAt(index);

            Color back = game.IsGiven(index) ? GivenBack : Color.White;
            if (index == _selected)
            {
                back = SelectedBack;
            }
            else if (_selected >= 0)
            {
                bool sameBox = row / 3 == selectedRow / 3 && column / 3 == selectedColumn / 3;
                if (selectedDigit != 0 && digit == selectedDigit)
                {
                    back = SelectedBack;
                }
                else if (!game.IsGiven(index) && (row == selectedRow || column == selectedColumn || sameBox))
                {
                    back = PeerBack;
                }
            }

            using (var brush = new SolidBrush(back))
            {
                g.FillRectangle(brush, bounds);
            }

            if (digit == 0)
            {
                continue;
            }

            Color ink;
            if (game.HasConflict(index))
            {
                ink = ConflictInk;
            }
            else if (game.IsGiven(index))
            {
                ink = Color.Black;
            }
            else if (game.IsHinted(index))
            {
                ink = HintInk;
            }
            else
            {
                ink = EntryInk;
            }

            using var inkBrush = new SolidBrush(ink);
            g.DrawString(
                digit.ToString(),
                game.IsGiven(index) ? givenFont : entryFont,
                inkBrush,
                bounds,
                format);
        }
    }

    private void MoveSelection(int rowStep, int columnStep)
    {
        if (_selected < 0)
        {
            SelectedIndex = 0;
            return;
        }

        int row = Math.Clamp(_selected / 9 + rowStep, 0, 8);
        int column = Math.Clamp(_selected % 9 + columnStep, 0, 8);
        SelectedIndex = row * 9 + column;
    }

    /// <summary>The grid is the largest square that fits, centred in the control.</summary>
    private (float Left, float Top, float Cell) GridLayout()
    {
        float size = Math.Min(ClientSize.Width, ClientSize.Height) - Inset * 2;
        float cell = (float)Math.Floor(size / 9f);
        float left = (ClientSize.Width - cell * 9) / 2f;
        float top = (ClientSize.Height - cell * 9) / 2f;
        return (left, top, cell);
    }
}
