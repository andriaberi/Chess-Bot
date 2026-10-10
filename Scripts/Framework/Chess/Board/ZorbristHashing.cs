namespace Chess.ChessEngine;

using Chess.API;

class ZobristHashing {
    // Zobrist hashing is a technique used to generate a unique hash for a chess position
    // It uses random numbers to represent each piece on each square and combines them using XOR
    // This allows for fast comparison of positions and is used in transposition tables
    public static ulong CalculateZobristKey(Board board) {
        ulong key = 0;
        if (!board.IsWhiteTurn) key ^= blackToMoveKey; // XOR with the key for black to move
        for (int i = 0; i < 64; i++) {
            key ^= PieceKey(board.Square[i], i);
        }
        return key ^ StateKey(board);
    }

    // Key for the castling rights and the en passant file
    // Positions with the same pieces but different rights are different positions, for repetition and for the transposition table
    public static ulong StateKey(Board board) {
        ulong key = castlingKeys[board.CastlingRights];
        if (CanCaptureEnPassant(board)) key ^= enPassantFileKeys[BoardHelper.ColumnIndex(board.EnPassantSquare)];
        return key;
    }

    // An en passant square only changes the position if a pawn of the side to move stands next to the pawn that just moved
    static bool CanCaptureEnPassant(Board board) {
        if (board.EnPassantSquare == -1) return false;

        int pushedPawn = board.EnPassantSquare + (board.IsWhiteTurn ? -8 : 8);
        int file = BoardHelper.ColumnIndex(pushedPawn);
        Bitboard capturers = board.Type[Piece.Pawn] & board.Color[board.IsWhiteTurn];

        return (file > 0 && capturers.Contains(pushedPawn - 1)) || (file < 7 && capturers.Contains(pushedPawn + 1));
    }

    // Key for a single piece on a single square (0 for an empty square)
    // XOR-ing it in and out lets the board update its key incrementally
    public static ulong PieceKey(Piece piece, int square) {
        if (piece.IsNone) return 0;
        int pieceIndex = (piece.Color / 8 - 1) * 6 + (piece.Type - 1); // 0-11 for 12 pieces
        return ZobristTable[square, pieceIndex];
    }

    public static ulong BlackToMoveKey => blackToMoveKey;

    private static readonly ulong[,] ZobristTable = new ulong[64, 12]; // 64 squares, 12 piece types (6 colors * 2 types)
    private static readonly ulong blackToMoveKey = 0x1UL; // Unique key for black to move
    private static readonly ulong[] castlingKeys = new ulong[16]; // One per combination of the 4 castling rights
    private static readonly ulong[] enPassantFileKeys = new ulong[8];

    static ZobristHashing() {
        Random random = new Random();
        blackToMoveKey = (ulong) random.NextInt64(long.MaxValue);
        for (int i = 0; i < 64; i++) {
            for (int j = 0; j < 12; j++) {
                ZobristTable[i, j] = (ulong) random.NextInt64(long.MaxValue);
            }
        }
        for (int i = 0; i < 16; i++) castlingKeys[i] = (ulong) random.NextInt64(long.MaxValue);
        for (int i = 0; i < 8; i++) enPassantFileKeys[i] = (ulong) random.NextInt64(long.MaxValue);
    }
}