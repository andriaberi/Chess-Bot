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
        Board board = new Board("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1");

        Assert.False(board.IsWhiteTurn);
        Assert.Equal(Square("e3"), board.EnPassantSquare);
        Assert.True(board.Square[Square("e4")].IsPawn);
        Assert.True(board.Square[Square("e2")].IsNone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 20")]
    [InlineData("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 40")]
    [InlineData("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1")]
    [InlineData("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1")]
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
    public void UnmakeMove_RestoresEnPassantSquare() {
        Board board = new Board("");
        Play(board, "e2e4");
        int enPassantSquare = board.EnPassantSquare;

        var move = FindMove(board, "a7a6");
        board.MakeMove(move);
        board.UnmakeMove(move);

        Assert.Equal(enPassantSquare, board.EnPassantSquare);
    }

    [Fact]
    public void QuietMove_IncrementsHalfMoveClock() {
        Board board = new Board("");
        Play(board, "g1f3");

        Assert.Equal(1, board.HalfMoveClock);
    }

    [Fact]
    public void CaptureAndPawnMoves_ResetHalfMoveClock() {
        Board board = new Board("");

        Play(board, "g1f3", "b8c6");
        Assert.Equal(2, board.HalfMoveClock);

        Play(board, "e2e4");
        Assert.Equal(0, board.HalfMoveClock);

        Play(board, "c6d4", "f3d4");
        Assert.Equal(0, board.HalfMoveClock);
    }

    [Theory]
    [InlineData("a1")]
    [InlineData("e2")]
    [InlineData("h8")]
    public void SquareNameFromIndex_MatchesSquareIndexFromName(string name) {
        Assert.Equal(name, BoardHelper.SquareNameFromIndex(Square(name)));
        Assert.Equal(Square(name), BoardHelper.IndexFromCoord(BoardHelper.CoordFromIndex(Square(name))));
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
    public void ZobristKey_DependsOnCastlingRights() {
        // Both kings walk out and back, so the pieces end where they started but castling is gone
        Board board = new Board("");
        ulong canCastle = board.ZobristKey;

        Play(board, "e2e4", "e7e5", "e1e2", "e8e7", "e2e1", "e7e8");

        Board sameWithoutKingMoves = new Board("");
        Play(sameWithoutKingMoves, "e2e4", "e7e5");

        Assert.NotEqual(canCastle, board.ZobristKey);
        Assert.NotEqual(sameWithoutKingMoves.ZobristKey, board.ZobristKey);
    }

    [Fact]
    public void ZobristKey_CountsEnPassantOnlyWhenACaptureIsPossible() {
        // 1. e4 with no black pawn next to e4: the en passant square makes no difference
        Board noCapture = new Board("");
        Play(noCapture, "e2e4");
        Assert.Equal(ZobristHashing.CalculateZobristKey(new Board("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1")), noCapture.ZobristKey);

        // Black pawn on d4 can take e3 en passant, so the same pieces without the en passant square are a different position
        Board capture = new Board("rnbqkbnr/ppp1pppp/8/8/3pP3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1");
        Board noEnPassant = new Board("rnbqkbnr/ppp1pppp/8/8/3pP3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1");
        Assert.NotEqual(noEnPassant.ZobristKey, capture.ZobristKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 20")]
    [InlineData("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1")]
    public void IncrementalZobristKey_MatchesFullRecalculation(string fen) {
        // Walks two plies of every line, which covers castling, en passant and rook captures
        Board board = new Board(fen);

        foreach (Move move in MoveGenerator.GenerateMoves(board)) {
            board.MakeMove(move);
            Assert.Equal(ZobristHashing.CalculateZobristKey(board), board.ZobristKey);

            foreach (Move reply in MoveGenerator.GenerateMoves(board)) {
                board.MakeMove(reply);
                Assert.Equal(ZobristHashing.CalculateZobristKey(board), board.ZobristKey);
                board.UnmakeMove(reply);
            }

            board.UnmakeMove(move);
        }
    }

    [Fact]
    public void MovingPiecesBackAndForth_RepeatsThePosition() {
        Board board = new Board("");

        Play(board, "g1f3", "g8f6", "f3g1", "f6g8");

        Assert.Equal(2, board.CountZobristKeys(board.ZobristKey));
    }
}
