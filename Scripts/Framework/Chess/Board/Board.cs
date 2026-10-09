namespace Chess.ChessEngine;

using Chess.API;

class Board {
    public Piece[] Square = new Piece[64];
    public bool IsWhiteTurn = true;
    public int CastlingRights = 0b0000;
    public int EnPassantSquare = -1;
    public int HalfMoveClock = 0;
    public int MoveCount = 1;

    public List<ulong> PastZobristKeys = new List<ulong>();
    public List<Move> MovesMade = new List<Move>();

    // Bitboard for pieces, indexed by piece type
    public Bitboard[] Type = new Bitboard[7];

    // Bitboard for colors, indexed by isWhite
    public ColorBitboards Color = new ColorBitboards();

    public Bitboard Occupied => Color[true] | Color[false];
    public Bitboard Empty => ~Occupied;

    public ulong ZobristKey;

    public Move LastMove => MovesMade.Count > 0 ? MovesMade[^1] : Move.NullMove;

    public Board(string fen) {
        FenUtility.LoadFen(fen, this);
        MovesMade = new List<Move>(); // Clearing the list of moves after resetting the game

        ZobristKey = ZobristHashing.CalculateZobristKey(this);
        PastZobristKeys = new List<ulong> { ZobristKey }; // Initialize with the current key
    }

    public void SwitchTurn() => IsWhiteTurn = !IsWhiteTurn;

    public void MakeMove(Move move, bool record = false) {
        MoveUtility.MakeMove(this, move);
        SwitchTurn();
        if (IsWhiteTurn) MoveCount++;

        ZobristKey = ZobristHashing.CalculateZobristKey(this);
        PastZobristKeys.Add(ZobristKey);

        if (record) MovesMade.Add(move);
    }

    public void UnmakeMove(Move move) {
        if (IsWhiteTurn) MoveCount--;
        SwitchTurn();
        MoveUtility.UnmakeMove(this, move);

        PastZobristKeys.RemoveAt(PastZobristKeys.Count - 1);
        ZobristKey = ZobristHashing.CalculateZobristKey(this);
    }

    public int CountZobristKeys(ulong zobristKey) {
        int count = 0;
        foreach (ulong key in PastZobristKeys) {
            if (key == zobristKey) count++;
        }
        return count;
    }
}

// Two bitboards indexed by color (true = white, false = black)
// The indexer returns a reference so callers can update the bitboard in place
class ColorBitboards {
    private Bitboard white;
    private Bitboard black;

    public ref Bitboard this[bool isWhite] => ref (isWhite ? ref white : ref black);
}
