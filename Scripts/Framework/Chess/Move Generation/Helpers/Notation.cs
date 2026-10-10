namespace Chess.ChessEngine;

using Chess.API;

class Notation {
    // Standard algebraic notation (e.g. Nf3, exd5, O-O, e8=Q+, Qh4#) of a move played from the given position
    public static string ToSan(Board board, Move move) {
        string san = MoveText(board, move);

        board.MakeMove(move);
        if (MoveHelper.IsInCheck(board, board.IsWhiteTurn)) san += MoveGenerator.GenerateMoves(board).Count == 0 ? "#" : "+";
        board.UnmakeMove(move);

        return san;
    }

    // Long algebraic notation as UCI uses it: source and target square, plus the promotion piece (e.g. e2e4, e7e8q)
    // A null move is written 0000
    public static string ToUci(Move move) {
        if (move.IsNull) return "0000";
        return move.SourceCoord.ToString() + move.TargetCoord.ToString() + Letter(move.PromotingTo).ToLower();
    }

    // The legal move written as in ToUci, or a null move when there is none
    public static Move FromUci(Board board, string uci) {
        foreach (Move move in MoveGenerator.GenerateMoves(board)) {
            if (ToUci(move) == uci) return move;
        }
        return Move.NullMove;
    }

    private static string MoveText(Board board, Move move) {
        if (move.IsCastling) return move.TargetCoord.ColumnIndex == 6 ? "O-O" : "O-O-O";

        Piece piece = board.Square[move.Source];
        bool capture = !board.Square[move.Target].IsNone || move.IsEnPassant;
        string target = move.TargetCoord.ToString();

        if (piece.IsPawn) {
            string pawnMove = capture ? $"{File(move.Source)}x{target}" : target;
            if (move.IsPromotion) pawnMove += "=" + Letter(move.PromotingTo);
            return pawnMove;
        }

        return Letter(piece.Type) + Disambiguation(board, move, piece) + (capture ? "x" : "") + target;
    }

    // When another piece of the same kind can reach the same square, name the source file, rank, or both
    private static string Disambiguation(Board board, Move move, Piece piece) {
        bool ambiguous = false, sameFile = false, sameRank = false;

        foreach (Move other in MoveGenerator.GenerateMoves(board)) {
            if (other.Target != move.Target || other.Source == move.Source) continue;
            if (board.Square[other.Source].Type != piece.Type) continue;

            ambiguous = true;
            if (other.SourceCoord.ColumnIndex == move.SourceCoord.ColumnIndex) sameFile = true;
            if (other.SourceCoord.RowIndex == move.SourceCoord.RowIndex) sameRank = true;
        }

        if (!ambiguous) return "";
        if (!sameFile) return File(move.Source).ToString();
        if (!sameRank) return Rank(move.Source).ToString();
        return move.SourceCoord.ToString();
    }

    private static char File(int square) => (char) ('a' + BoardHelper.ColumnIndex(square));
    private static char Rank(int square) => (char) ('1' + BoardHelper.RowIndex(square));

    private static string Letter(int pieceType) => pieceType switch {
        Piece.Knight => "N",
        Piece.Bishop => "B",
        Piece.Rook => "R",
        Piece.Queen => "Q",
        Piece.King => "K",
        _ => ""
    };
}
