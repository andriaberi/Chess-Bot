# ♟️ Chess Bot

Chess Bot is a C#-based chess engine capable of playing chess against a human or another engine. It features move generation, evaluation, and an iterative deepening alpha-beta search with transposition tables and time management for real-time play.

![alt text](<Resources/Media/Demo.png>)

# Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Setup Instructions](#setup-instructions)
- [Testing Guide](#testing-guide)
- [UCI and Comparing Versions](#uci-and-comparing-versions)
- [Modifying the Codebase](#modifying-the-codebase)
- [License](#license)
- [Contact Information](#contact-information)

# Overview

ChessBot is a modular engine written in C#. It supports all chess rules, evaluates board states, and searches for optimal moves using iterative deepening with alpha-beta pruning and transposition tables.

It’s fast, accurate, and easy to expand.

# Features

* Fully legal move generation (castling, en passant, promotion)
* Bitboard architecture for high performance
* Magic bitboards for sliding pieces (rooks, bishops, queens)
* Iterative deepening alpha-beta with move ordering
* Transposition tables to reuse prior calculations
* Parallel time management for smooth gameplay
* Clean, modular codebase—UI-agnostic

# Architecture

| Component            | Description                                                      |
| -------------------- | ---------------------------------------------------------------- |
| `Board`              | Manages game state using Zobrist hashing                         |
| `MoveGenerator`      | Generates legal and pseudo-legal moves using bitboards           |
| `MagicNumbers`       | Precomputed constants for fast sliding piece move calculation    |
| `Bot`                | Implements search algorithms, time management, and move decisions|
| `Evaluation`         | Performs static position evaluation with piece-square tables     |
| `TranspositionTable` | Caches positions for faster repeated state lookups               |
| `OpeningBook`        | Database of grandmaster openings to guide early-game decisions   |
| `UI`                 | Visualizes the board and user interface (specific to this app)   |


# Setup Instructions

1. **Make Sure .NET SDK is Installed**  
   - You need **.NET 8.0 or higher**  
   - Download it from: [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)  
   - After installing, verify the installation with:  
     ```bash
     dotnet --version
     ```

2. **Clone the Repository**  
   ```bash
   git clone https://github.com/AndriaBeridze/Chess-Bot.git
   cd Chess-Bot
   ```

3. **Run the Bot**
   ```bash
   make run
   ```

4. **Optional: Run the Tests**
   ```bash
   make test
   ```

Run `make` on its own to see every command:

| Command | What it does |
|---|---|
| `make install` | Restore NuGet packages |
| `make build` | Build the app and tests (Release; `CONFIG=Debug` to change) |
| `make run` | Build and play the game |
| `make publish` | Package a self-contained build into `dist/` (`RID=linux-x64\|win-x64\|osx-x64\|osx-arm64`, or pick from a menu) |
| `make clean` | Remove build artifacts |
| `make test` | Run the tests |
| `make perft` | Run the perft tests and show nodes, time and speed per position and depth |
| `make check` | Build everything and test (what CI runs) |
| `make format` | Fix whitespace to match `.editorconfig` |
| `make match` | Play two versions of the bot against each other (`OLD=v1.0`, optional `NEW=`, `GAMES=`, `TC=`) |

Without `make`, use `dotnet run -c Release` and `dotnet test -c Release`.

# Testing Guide

* After running the program, a window will open with the board set up—human plays as White by default.
* Drag and drop a piece, or click it and then click its square, to make your move.
* Right-drag between squares to draw an arrow, or right-click a square to circle it; a left click clears them.
* The bot will respond automatically, typically within 2 seconds. Its search, best move and evaluation show on the left.
* **Hint** shows the bot's best move for you as an arrow; **Resign** ends the game after a second click.
* The **Menu** starts a new game (Play White, Play Black, AI vs AI) and sets the bot's difficulty and the clock.
  It can also save the game as PGN, copy the position as FEN, and flip the board.
* To verify the engine, run the unit tests: `make test`
  * Tests live in the `Tests` folder and cover move generation (perft), the board, game rules, and the bot.
  * Every push and pull request runs the full suite on GitHub Actions.

# UCI and Comparing Versions

Started with `--uci`, the bot runs without a window as a [UCI](https://backscattering.de/chess/uci/) engine,
so chess GUIs (Arena, Cute Chess, Banksia) and tournament runners can use it:

```sh
dotnet bin/Release/net8.0/Chess-Bot.dll --uci
```

It supports `uci`, `isready`, `setoption` (`Difficulty`, `OwnBook`), `ucinewgame`, `position`, `go`
(`wtime`/`btime`/`winc`/`binc`, `movetime`, `depth`, `infinite`), `stop` and `quit`, and reports
`info depth … score … nodes … nps … time … pv …` as it searches.

Versions are git tags (`v1.0`, `v1.1`, …), and the engine reports its version as `Chess-Bot 1.1`
(`1.1+3` three commits later). To see whether a change made the bot stronger, play it against an earlier version:

```sh
make match OLD=v1.0              # your working tree against v1.0
make match OLD=v1.0 NEW=v1.1     # two tagged versions
make match OLD=v1.0 GAMES=400 TC=5+0.05
```

This needs [fastchess](https://github.com/Disservin/fastchess) or
[cutechess-cli](https://github.com/cutechess/cutechess) on your `PATH`. Both sides play the same random openings
from `Resources/Openings/Match.epd`, once with each color, and the runner reports the score and Elo difference.
Games are saved to `.match/games.pgn`.

# Modifying the Codebase

* To change the bot’s behavior, navigate to the `Scripts/Bot` folder.
* To work on move generation logic, go to `Scripts/Framework/Chess/Move Generation`.
* To modify the user interface, access `Scripts/Framework/App/UI`.
* For theme and general settings, check out `Scripts/Utilities`.

# License

This project is licensed under the MIT License. See `LICENSE`.

# Contact Information

| Name           | Phone Number      | Email               | LinkedIn                                    |
|----------------|-------------------|---------------------|---------------------------------------------|
| Andria Beridze | +1 (267) 632-6754 | andria24b@gmail.com | [linkedin.com/in/andriaberidze](https://www.linkedin.com/in/andriaberidze/) |

