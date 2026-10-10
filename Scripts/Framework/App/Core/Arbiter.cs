namespace Chess.Core;

using Chess.API;
using Chess.ChessEngine;

class Arbiter {
    public static bool IsCheckmate(Board board) {
        // Two requirements for checkmate:
        // 1. The king is in check
        // 2. There are no legal moves to get out of check
        if (!MoveHelper.IsInCheck(board, board.IsWhiteTurn)) return false;
        if (MoveGenerator.GenerateMoves(board).Count > 0) return false;
        return true;
    }

    public static bool IsStalemate(Board board) {
        // Two requirements for stalemate:
        // 1. The king is not in check
        // 2. There are no legal moves
        if (MoveHelper.IsInCheck(board, board.IsWhiteTurn)) return false;
        if (MoveGenerator.GenerateMoves(board).Count > 0) return false;
        return true;
    }

    public static bool IsDraw(Board board) => DrawReason(board) != "";

    // Why the game is drawn (other than stalemate), or "" when it is not
    public static string DrawReason(Board board, double? whiteTime = null, double? blackTime = null) {
        if (board.CountZobristKeys(board.ZobristKey) >= 3) return "Repetition";
        if (board.HalfMoveClock >= 100) return "50-move rule";
        if (IsDeadPosition(board)) return "Insufficient material";
        // Out of time, but the opponent has nothing to win with
        if ((whiteTime == 0 && HasOnlyKing(board, false)) || (blackTime == 0 && HasOnlyKing(board, true))) return "Insufficient material";
        return "";
    }

    // No sequence of legal moves can end in checkmate. Mate stays possible (with help) in
    // positions like K+N+N vs K, K+B vs K+N or bishops on both colors, so those play on
    static bool IsDeadPosition(Board board) {
        if (!board.Type[Piece.Rook].IsEmpty) return false;
        if (!board.Type[Piece.Queen].IsEmpty) return false;
        if (!board.Type[Piece.Pawn].IsEmpty) return false;

        int knights = board.Type[Piece.Knight].Count();
        Bitboard bishops = board.Type[Piece.Bishop];

        // K vs K, or K+N vs K
        if (bishops.IsEmpty) return knights <= 1;
        if (knights > 0) return false;

        // Any number of bishops, all on the same color of square, for either side
        Bitboard lightBishops = bishops & LightSquares;
        return lightBishops.IsEmpty || lightBishops.Count() == bishops.Count();
    }

    // Only a bare king can never checkmate, so running out of time against one is a draw
    static bool HasOnlyKing(Board board, bool isWhite) {
        return (board.Color[isWhite] & ~board.Type[Piece.King]).IsEmpty;
    }

    static readonly Bitboard LightSquares = new Bitboard(0x55AA55AA55AA55AAUL);

    public static string Status(Board board, double? whiteTime = null, double? blackTime = null) {
        if (IsCheckmate(board)) return "Checkmate";
        if (IsStalemate(board)) return "Stalemate";
        if (IsDraw(board)) return "Draw";
        if (whiteTime != null && whiteTime == 0) return HasOnlyKing(board, false) ? "Draw" : "Time Out";
        if (blackTime != null && blackTime == 0) return HasOnlyKing(board, true) ? "Draw" : "Time Out";

        return "";
    }
}