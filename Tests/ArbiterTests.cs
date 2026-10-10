namespace Chess.Tests;

using Chess.ChessEngine;
using Chess.Core;
using static TestHelpers;

public class ArbiterTests {
    [Fact]
    public void StartingPosition_GameIsInProgress() {
        Assert.Equal("", Arbiter.Status(new Board("")));
    }

    [Fact]
    public void FoolsMate_IsCheckmate() {
        Board board = new Board("");
        Play(board, "f2f3", "e7e5", "g2g4", "d8h4");

        Assert.True(Arbiter.IsCheckmate(board));
        Assert.Equal("Checkmate", Arbiter.Status(board));
    }

    [Fact]
    public void KingWithNoMovesAndNotInCheck_IsStalemate() {
        // Black king h8, white king f7, white queen g6, black to move
        Board board = new Board("7k/5K2/6Q1/8/8/8/8/8 b - - 0 1");

        Assert.True(Arbiter.IsStalemate(board));
        Assert.Equal("Stalemate", Arbiter.Status(board));
    }

    [Fact]
    public void BareKings_IsDraw() {
        Assert.True(Arbiter.IsDraw(new Board("7k/8/8/8/8/8/8/K7 w - - 0 1")));
    }

    [Fact]
    public void KingAndRook_IsNotADraw() {
        Assert.False(Arbiter.IsDraw(new Board("7k/8/8/8/8/8/8/KR6 w - - 0 1")));
    }

    [Theory]
    [InlineData("7k/8/8/8/8/8/8/KB6 w - - 0 1")] // K+B vs K
    [InlineData("7k/8/8/8/8/8/8/KN6 w - - 0 1")] // K+N vs K
    [InlineData("7k/8/8/8/8/8/b7/KB6 w - - 0 1")] // Bishops on the same color
    public void NoPossibleMate_IsDraw(string fen) {
        Assert.True(Arbiter.IsDraw(new Board(fen)));
    }

    [Theory]
    [InlineData("7k/8/8/8/8/8/8/KNN5 w - - 0 1")] // K+N+N vs K
    [InlineData("6nk/8/8/8/8/8/8/KB6 w - - 0 1")] // K+B vs K+N
    [InlineData("7k/8/8/8/8/8/8/KBb5 w - - 0 1")] // Bishops on opposite colors
    public void MateStillPossible_IsNotADraw(string fen) {
        Assert.False(Arbiter.IsDraw(new Board(fen)));
    }

    [Fact]
    public void ThreefoldRepetition_IsDraw() {
        Board board = new Board("");
        Play(board, "g1f3", "g8f6", "f3g1", "f6g8", "g1f3", "g8f6", "f3g1", "f6g8");

        Assert.True(Arbiter.IsDraw(board));
    }

    [Fact]
    public void ClockAtZero_IsTimeOut() {
        Assert.Equal("Time Out", Arbiter.Status(new Board(""), whiteTime: 0, blackTime: 100));
    }

    [Fact]
    public void ClockAtZeroAgainstABareKing_IsDraw() {
        // White has a rook, black only a king: black cannot win on time, white can
        Board board = new Board("7k/8/8/8/8/8/8/KR6 w - - 0 1");

        Assert.Equal("Draw", Arbiter.Status(board, whiteTime: 0, blackTime: 100));
        Assert.Equal("Time Out", Arbiter.Status(board, whiteTime: 100, blackTime: 0));
    }
}
