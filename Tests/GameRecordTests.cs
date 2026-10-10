namespace Chess.Tests;

using Chess.ChessEngine;
using static TestHelpers;

public class GameRecordTests {
    [Fact]
    public void WritesStandardFen() {
        Board board = new Board("");
        Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", board.Fen);

        Play(board, "e2e4");
        Assert.Equal("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", board.Fen);
        Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", board.StartFen);
    }

    [Fact]
    public void WritesPgn() {
        Board board = new Board("");
        Play(board, "f2f3", "e7e5", "g2g4", "d8h4");

        string pgn = Chess.Core.Pgn.Write(board, "Human", "Bot (Hard)", "0-1");

        Assert.Contains("[White \"Human\"]", pgn);
        Assert.Contains("[Result \"0-1\"]", pgn);
        Assert.DoesNotContain("[FEN", pgn);
        Assert.EndsWith("1. f3 e5 2. g4 Qh4# 0-1" + Environment.NewLine, pgn);
    }

    [Fact]
    public void WritesPgnFromASetUpPositionWithBlackToMove() {
        Board board = new Board("R6K/8/8/8/8/8/8/7k b - - 0 1");
        Play(board, "h8g8", "a1a8");

        string pgn = Chess.Core.Pgn.Write(board, "Bot (Hard)", "Human", "*");

        Assert.Contains("[SetUp \"1\"]", pgn);
        Assert.Contains("[FEN \"7k/8/8/8/8/8/8/R6K b - - 0 1\"]", pgn);
        Assert.EndsWith("1... Kg8 2. Ra8+ *" + Environment.NewLine, pgn);
    }
}
