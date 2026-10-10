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
    public List<string> MovesNotation = new List<string>(); // Algebraic notation of MovesMade, for the move history
    public int FirstPly; // Plies played before this position, from its move number and side to move (e.g. 1 when black moves first)

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
        FirstPly = (MoveCount - 1) * 2 + (IsWhiteTurn ? 0 : 1);

        ZobristKey = ZobristHashing.CalculateZobristKey(this);
        PastZobristKeys = new List<ulong> { ZobristKey }; // Initialize with the current key
    }

    public void SwitchTurn() => IsWhiteTurn = !IsWhiteTurn;

    public void MakeMove(Move move, bool record = false) {
        // Notation depends on the position before the move (disambiguation) and after it (check)
        if (record) MovesNotation.Add(Notation.ToSan(this, move));

        // Update the Zobrist key incrementally: XOR out the pieces that leave their squares, XOR in the ones that arrive
        // The castling and en passant state is XOR-ed out here and the new state back in after the move
        ulong key = ZobristKey ^ ZobristHashing.BlackToMoveKey ^ ZobristHashing.StateKey(this);
        int captureSquare = move.IsEnPassant ? (IsWhiteTurn ? move.Target - 8 : move.Target + 8) : move.Target;

        key ^= ZobristHashing.PieceKey(Square[move.Source], move.Source);
        key ^= ZobristHashing.PieceKey(Square[captureSquare], captureSquare);

        if (move.IsCastling) {
            int rookSource = move.Target + (move.Target == 62 || move.Target == 6 ? 1 : -2);
            int rookTarget = move.Target + (move.Target == 62 || move.Target == 6 ? -1 : 1);
            key ^= ZobristHashing.PieceKey(Square[rookSource], rookSource) ^ ZobristHashing.PieceKey(Square[rookSource], rookTarget);
        }

        MoveUtility.MakeMove(this, move);
        SwitchTurn();
        if (IsWhiteTurn) MoveCount++;

        key ^= ZobristHashing.PieceKey(Square[move.Target], move.Target); // Handles promotions too
        key ^= ZobristHashing.StateKey(this);
        ZobristKey = key;
        System.Diagnostics.Debug.Assert(ZobristKey == ZobristHashing.CalculateZobristKey(this));
        PastZobristKeys.Add(ZobristKey);

        if (record) MovesMade.Add(move);
    }

    public void UnmakeMove(Move move) {
        if (IsWhiteTurn) MoveCount--;
        SwitchTurn();
        MoveUtility.UnmakeMove(this, move);

        PastZobristKeys.RemoveAt(PastZobristKeys.Count - 1);
        ZobristKey = PastZobristKeys[^1]; // The previous position's key is already in the history
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
