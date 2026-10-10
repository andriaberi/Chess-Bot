namespace Chess.Tests;

using Chess.ChessEngine;
using static TestHelpers;

public class NotationTests {
    private static List<string> Played(string fen, params string[] moves) {
        Board board = new Board(fen);
        Play(board, moves);
        return board.MovesNotation;
    }

    [Fact]
    public void RecordsPawnAndPieceMovesWithMate() {
        Assert.Equal(new[] { "f3", "e5", "g4", "Qh4#" }, Played("", "f2f3", "e7e5", "g2g4", "d8h4"));
    }

    [Fact]
    public void RecordsCapturesChecksAndCastling() {
        Assert.Equal(new[] { "e4", "d5", "exd5", "Qxd5", "Nf3", "Qe4+" }, Played("", "e2e4", "d7d5", "e4d5", "d8d5", "g1f3", "d5e4"));
        Assert.Equal(new[] { "e4", "e5", "Nf3", "Nc6", "Bc4", "Nf6", "O-O" }, Played("", "e2e4", "e7e5", "g1f3", "b8c6", "f1c4", "g8f6", "e1g1"));
    }

    [Fact]
    public void NamesTheSourceWhenTwoPiecesCanReachTheSquare() {
        Assert.Equal(new[] { "Rad1" }, Played("R6R/4K3/8/8/8/8/8/4k3 w - - 0 1", "a1d1"));
        Assert.Equal(new[] { "R1a3" }, Played("R7/4K3/8/8/R7/8/8/4k3 w - - 0 1", "a1a3"));
    }

    [Fact]
    public void RecordsPromotion() {
        Assert.Equal(new[] { "a8=Q+" }, Played("K7/8/8/8/8/8/P7/7k w - - 0 1", "a7a8q"));
    }
}
