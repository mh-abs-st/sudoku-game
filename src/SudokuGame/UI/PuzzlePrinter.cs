using SudokuGame.Engine;

namespace SudokuGame.UI;

/// <summary>Lays out up to four puzzles on one printed page, two across and two down.</summary>
internal static class PuzzlePrinter
{
    public static void DrawPage(Graphics g, Rectangle area, IReadOnlyList<Puzzle> puzzles)
    {
        const float Gap = 30f;

        float tileWidth = area.Width / 2f;
        float tileHeight = area.Height / 2f;
        float cell = (float)Math.Floor((Math.Min(tileWidth, tileHeight) - Gap) / 9f);
        if (cell < 4)
        {
            return;
        }

        float size = cell * 9;

        using var font = new Font("Segoe UI", cell * 0.55f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var thin = new Pen(Color.Black, 1f);
        using var thick = new Pen(Color.Black, 2.5f);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        for (int tile = 0; tile < puzzles.Count && tile < 4; tile++)
        {
            float left = area.Left + (tile % 2) * tileWidth + (tileWidth - size) / 2f;
            float top = area.Top + (tile / 2) * tileHeight + (tileHeight - size) / 2f;

            for (int line = 0; line <= 9; line++)
            {
                Pen pen = line % 3 == 0 ? thick : thin;
                float offset = line * cell;
                g.DrawLine(pen, left + offset, top, left + offset, top + size);
                g.DrawLine(pen, left, top + offset, left + size, top + offset);
            }

            int[] cells = puzzles[tile].Cells;
            for (int index = 0; index < 81; index++)
            {
                if (cells[index] == 0)
                {
                    continue;
                }

                var bounds = new RectangleF(left + (index % 9) * cell, top + (index / 9) * cell, cell, cell);
                g.DrawString(cells[index].ToString(), font, Brushes.Black, bounds, format);
            }
        }
    }
}
