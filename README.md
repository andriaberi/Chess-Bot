# ♟️ Chess Bot

Chess Bot is a C#-based chess engine capable of playing chess against a human or another engine. It features move generation, evaluation, and an iterative deepening alpha-beta search with transposition tables and time management for real-time play.

![alt text](<Resources/Media/Demo.png>)

# Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Setup Instructions](#setup-instructions)
- [Testing Guide](#testing-guide)
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

Without `make`, use `dotnet run -c Release` and `dotnet test -c Release`.

# Testing Guide

* After running the program, a window will open with the board set up—human plays as White by default.
* Drag and drop pieces to make your move.
* The bot will respond automatically, typically within 2 seconds.
* Monitor the timer to see the remaining time for each side.
* The game status (win/draw) will be displayed on the left once the game ends.
* Use the three buttons at the bottom-left corner to switch game modes: Play as White, Play as Black, or AI vs AI.
* To verify the engine, run the unit tests: `make test`
  * Tests live in the `Tests` folder and cover move generation (perft), the board, game rules, and the bot.
  * Every push and pull request runs the full suite on GitHub Actions.

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

