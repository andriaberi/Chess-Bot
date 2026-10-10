namespace Chess.Tests;

using Chess.API;
using Chess.ChessEngine;
using static TestHelpers;

public class MoveGenerationTests {
    [Fact]
    public void StartingPosition_HasTwentyMoves() {
        Assert.Equal(20, MoveGenerator.GenerateMoves(new Board("")).Count);
    }

    [Fact]
    public void Castling_IsGeneratedOnBothSides() {
        Board board = new Board("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 20");

        Assert.True(FindMove(board, "e1g1").IsCastling);
        Assert.True(FindMove(board, "e1c1").IsCastling);
    }

    [Fact]
    public void Castling_RookIsMovedToo() {
        Board board = new Board("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 20");

        Play(board, "e1g1");

        Assert.True(board.Square[Square("g1")].IsKing);
        Assert.True(board.Square[Square("f1")].IsRook);
        Assert.True(board.Square[Square("h1")].IsNone);
    }

    [Fact]
    public void EnPassant_CapturesThePassedPawn() {
        Board board = new Board("");
        Play(board, "e2e4", "a7a6", "e4e5", "d7d5");

        Move move = FindMove(board, "e5d6");
        Assert.True(move.IsEnPassant);

        board.MakeMove(move, record: true);
        Assert.True(board.Square[Square("d6")].IsPawn);
        Assert.True(board.Square[Square("d5")].IsNone);
    }

    [Fact]
    public void Promotion_OffersAllFourPieces() {
        // White pawn on a7, kings on a1 and h8
        Board board = new Board("7k/P7/8/8/8/8/8/K7 w - - 0 1");

        string[] promotions = MoveGenerator.GenerateMoves(board)
            .Where(move => move.IsPromotion)
            .Select(Uci)
            .OrderBy(uci => uci)
            .ToArray();

        Assert.Equal(["a7a8b", "a7a8n", "a7a8q", "a7a8r"], promotions);
    }

    [Fact]
    public void PinnedPiece_CanOnlyMoveAlongThePin() {
        // White king e1, white rook e2 pinned by the black rook on e8
        Board board = new Board("k3r3/8/8/8/8/8/4R3/4K3 w - - 0 1");

        var rookMoves = MoveGenerator.GenerateMoves(board, Square("e2"));

        Assert.NotEmpty(rookMoves);
        Assert.All(rookMoves, move => Assert.Equal(BoardHelper.ColumnIndex(Square("e2")), BoardHelper.ColumnIndex(move.Target)));
    }

    [Fact]
    public void InCheck_OnlyMovesThatResolveTheCheckAreLegal() {
        Board board = new Board("");
        Play(board, "e2e4", "f7f6", "d2d4", "g7g5", "d1h5"); // Black king is in check

        foreach (Move move in MoveGenerator.GenerateMoves(board)) {
            board.MakeMove(move);
            Assert.False(MoveHelper.IsInCheck(board, false), $"{Uci(move)} leaves the king in check");
            board.UnmakeMove(move);
        }
    }

    [Fact]
    public void Move_EncodesSourceTargetAndFlag() {
        Move move = new Move(Square("e7"), Square("e8"), Move.QueenPromotion);

        Assert.Equal(Square("e7"), move.Source);
        Assert.Equal(Square("e8"), move.Target);
        Assert.True(move.IsPromotion);
        Assert.Equal(Piece.Queen, move.PromotingTo);
        Assert.True(Move.NullMove.IsNull);
    }
}
