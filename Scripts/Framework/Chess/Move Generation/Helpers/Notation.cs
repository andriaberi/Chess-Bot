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
