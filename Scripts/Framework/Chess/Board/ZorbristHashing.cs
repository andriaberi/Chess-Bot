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
        return key;
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

    static ZobristHashing() {
        Random random = new Random();
        blackToMoveKey = (ulong) random.NextInt64(long.MaxValue);
        for (int i = 0; i < 64; i++) {
            for (int j = 0; j < 12; j++) {
                ZobristTable[i, j] = (ulong) random.NextInt64(long.MaxValue);
            }
        }
    }
}