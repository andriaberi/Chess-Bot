namespace Chess.Tests;

using Chess.API;
using Chess.ChessEngine;
using static TestHelpers;

public class BoardTests {
    [Fact]
    public void EmptyFen_LoadsStartingPosition() {
        Board board = new Board("");

        Assert.True(board.IsWhiteTurn);
        Assert.Equal(0b1111, board.CastlingRights);
        Assert.Equal(-1, board.EnPassantSquare);
        Assert.Equal(32, board.Occupied.Count());
        Assert.Equal(16, board.Type[Piece.Pawn].Count());

        Assert.True(board.Square[Square("e1")].IsKing && board.Square[Square("e1")].IsWhite);
        Assert.True(board.Square[Square("e8")].IsKing && board.Square[Square("e8")].IsBlack);
        Assert.True(board.Square[Square("d1")].IsQueen && board.Square[Square("d1")].IsWhite);
    }

    [Fact]
    public void Fen_ParsesSideToMoveAndEnPassantSquare() {
        // Position after 1. e4
        Board board = new Board("RNBQKBNR/PPPP1PPP/8/4P3/8/8/pppppppp/rnbqkbnr b KQkq e3 0 1");

        Assert.False(board.IsWhiteTurn);
        Assert.Equal(Square("e3"), board.EnPassantSquare);
        Assert.True(board.Square[Square("e4")].IsPawn);
        Assert.True(board.Square[Square("e2")].IsNone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("R3K2R/PPPBBPPP/2N2Q1p/1p2P3/3PN3/bn2pnp1/p1ppqpb1/r3k2r w KQkq - 0 20")]
    [InlineData("8/4P1P1/8/1R3p1k/KP5r/3p4/2p5/8 w - - 0 40")]
    [InlineData("R2Q1RK1/Pp1P2PP/q4N2/BBP1P3/nP6/1b3nbN/Pppp1ppp/r3k2r w kq - 0 1")]
    public void MakeThenUnmake_RestoresTheBoard(string fen) {
        Board board = new Board(fen);
        string before = Snapshot(board);

        foreach (Move move in MoveGenerator.GenerateMoves(board)) {
            board.MakeMove(move);
            board.UnmakeMove(move);

            Assert.Equal(before, Snapshot(board));
        }
    }

    [Fact]
    public void ZobristKey_IsTheSameForTransposedMoveOrders() {
        Board first = new Board("");
        Play(first, "e2e4", "e7e5", "g1f3");

        Board second = new Board("");
        Play(second, "g1f3", "e7e5", "e2e4");

        Assert.Equal(first.ZobristKey, second.ZobristKey);
    }

    [Fact]
    public void ZobristKey_DependsOnSideToMove() {
        Board board = new Board("");
        ulong whiteToMove = board.ZobristKey;

        Play(board, "g1f3", "g8f6", "f3g1");

        Assert.NotEqual(whiteToMove, board.ZobristKey);
    }

    [Fact]
    public void MovingPiecesBackAndForth_RepeatsThePosition() {
        Board board = new Board("");

        Play(board, "g1f3", "g8f6", "f3g1", "f6g8");

        Assert.Equal(2, board.CountZobristKeys(board.ZobristKey));
    }
}
