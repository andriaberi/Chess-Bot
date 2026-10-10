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
        Board board = new Board("8/8/8/8/8/6Q1/5K2/7k b - - 0 1");

        Assert.True(Arbiter.IsStalemate(board));
        Assert.Equal("Stalemate", Arbiter.Status(board));
    }

    [Fact]
    public void BareKings_IsDraw() {
        Assert.True(Arbiter.IsDraw(new Board("K7/8/8/8/8/8/8/7k w - - 0 1")));
    }

    [Fact]
    public void KingAndRook_IsNotADraw() {
        Assert.False(Arbiter.IsDraw(new Board("KR6/8/8/8/8/8/8/7k w - - 0 1")));
    }

    [Theory]
    [InlineData("KB6/8/8/8/8/8/8/7k w - - 0 1")] // K+B vs K
    [InlineData("KN6/8/8/8/8/8/8/7k w - - 0 1")] // K+N vs K
    [InlineData("KB6/b7/8/8/8/8/8/7k w - - 0 1")] // Bishops on the same color
    public void NoPossibleMate_IsDraw(string fen) {
        Assert.True(Arbiter.IsDraw(new Board(fen)));
    }

    [Theory]
    [InlineData("KNN5/8/8/8/8/8/8/7k w - - 0 1")] // K+N+N vs K
    [InlineData("KB6/8/8/8/8/8/8/6nk w - - 0 1")] // K+B vs K+N
    [InlineData("KBb5/8/8/8/8/8/8/7k w - - 0 1")] // Bishops on opposite colors
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
        Board board = new Board("KR6/8/8/8/8/8/8/7k w - - 0 1");

        Assert.Equal("Draw", Arbiter.Status(board, whiteTime: 0, blackTime: 100));
        Assert.Equal("Time Out", Arbiter.Status(board, whiteTime: 100, blackTime: 0));
    }
}
