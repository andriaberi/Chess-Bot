namespace Chess.Tests;

using Chess.ChessEngine;
using static TestHelpers;

// Tests for bugs that are known but not fixed yet; remove the Skip once the bug is fixed
public class KnownBugTests {
    [Fact(Skip = "Known bug: MoveUtility.UnmakeMove clears the en passant square unless the undone move was en passant")]
    public void UnmakeMove_RestoresEnPassantSquare() {
        Board board = new Board("");
        Play(board, "e2e4");
        int enPassantSquare = board.EnPassantSquare;

        var move = FindMove(board, "a7a6");
        board.MakeMove(move);
        board.UnmakeMove(move);

        Assert.Equal(enPassantSquare, board.EnPassantSquare);
    }

    [Fact(Skip = "Known bug: BoardHelper.SquareNameFromIndex swaps rank and file (e2 comes out as b5); not used by the app yet")]
    public void SquareNameFromIndex_MatchesSquareIndexFromName() {
        Assert.Equal("e2", BoardHelper.SquareNameFromIndex(Square("e2")));
    }
}
