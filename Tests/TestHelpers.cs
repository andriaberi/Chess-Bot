// The engine keeps static state (move generator scratch data, make/unmake history),
// so tests must not run in parallel
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Chess.Tests;

using Chess.API;
using Chess.ChessEngine;

// Note: FEN strings in this project list rank 1 first (the reverse of standard FEN)
static class TestHelpers {
    public static int Square(string name) => BoardHelper.SquareIndexFromName(name);

    // Long algebraic notation, e.g. "e2e4" or "e7e8q"
    public static string Uci(Move move) {
        // Coord names squares correctly (BoardHelper.SquareNameFromIndex does not, see KnownBugTests)
        string text = move.SourceCoord.ToString() + move.TargetCoord.ToString();
        return move.PromotingTo switch {
            Piece.Queen => text + "q",
            Piece.Rook => text + "r",
            Piece.Bishop => text + "b",
            Piece.Knight => text + "n",
            _ => text
        };
    }

    // Finds the legal move written as "e2e4" / "e7e8q", fails the test if there is none
    public static Move FindMove(Board board, string uci) {
        Move? match = MoveGenerator.GenerateMoves(board).FirstOrDefault(move => Uci(move) == uci);
        Assert.True(match is not null, $"{uci} is not a legal move in this position");
        return match!;
    }

    public static void Play(Board board, params string[] moves) {
        foreach (string uci in moves) {
            board.MakeMove(FindMove(board, uci), record: true);
        }
    }

    public static long Perft(Board board, int depth) {
        if (depth == 0) return 1;

        long count = 0;
        foreach (Move move in MoveGenerator.GenerateMoves(board)) {
            board.MakeMove(move);
            count += Perft(board, depth - 1);
            board.UnmakeMove(move);
        }
        return count;
    }

    // Everything make/unmake is expected to restore (the halfmove clock is covered by KnownBugTests)
    public static string Snapshot(Board board) => string.Join(" | ",
        string.Concat(board.Square.Select(piece => piece.ToString())),
        string.Join(",", board.Type.OrderBy(entry => entry.Key).Select(entry => entry.Value.Value)),
        board.Color[true].Value,
        board.Color[false].Value,
        board.IsWhiteTurn,
        board.CastlingRights,
        board.EnPassantSquare,
        board.MoveCount,
        board.ZobristKey,
        board.PastZobristKeys.Count);
}
