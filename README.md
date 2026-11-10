# Sudoku

A Sudoku game for Windows, written in C# with Windows Forms.

## Features

- Random puzzles with a single solution, in four levels: Easy, Medium, Hard and Expert
- Mouse and keyboard play: click or use the arrow keys to move, type 1-9 to enter a digit, Delete to erase
- Clashing digits are shown in red
- Undo and redo
- Hints for one cell at a time, or the full solution
- Save a game to a `.sud` file and open it again later
- Print a page of four fresh puzzles
- The chosen level is remembered between runs

## Build and run

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download) on Windows.

```
dotnet run --project src/SudokuGame
```

Or open `SudokuGame.sln` in Visual Studio 2022 and press F5.

## Layout

| Path | Contents |
| --- | --- |
| `src/SudokuGame/Engine` | Solver, puzzle generator and game state. No UI code. |
| `src/SudokuGame/UI` | Main window, board control and print layout. |
| `src/SudokuGame/AppSettings.cs` | Saved preferences. |
