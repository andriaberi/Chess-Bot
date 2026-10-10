namespace Chess.Bot;

using System.Net;
using System.Runtime.CompilerServices;
using Chess.API;
using Chess.ChessEngine;

// Easy searches shallowly and picks among nearly equal moves at random, Medium searches a little deeper,
// Hard searches as deep as its think time allows
enum Difficulty { Easy, Medium, Hard }

class Bot {
    public static Difficulty Level = Difficulty.Hard;

    // Deepest search, longest think per move in seconds, and random noise added to move scores in centipawns
    static (int MaxDepth, double MaxThinkTime, int Noise) LevelSettings(Difficulty level) => level switch {
        Difficulty.Easy => (2, 0.5, 150),
        Difficulty.Medium => (4, 1.0, 0),
        _ => (int.MaxValue, double.MaxValue, 0)
    };

    const int positiveInfinity = 1000000000;
    const int negativeInfinity = -1000000000;
    const int checkmate = -1000000;
    const int maxMatePly = 1000; // Scores within this many plies of checkmate are mate scores

    // Piece values used for move ordering (not evaluation)
    static readonly Dictionary<int, int> PieceValue = new() {
        { Piece.Pawn, Evaluation.Pawn },
        { Piece.Knight, Evaluation.Knight },
        { Piece.Bishop, Evaluation.Bishop },
        { Piece.Rook, Evaluation.Rook },
        { Piece.Queen, Evaluation.Queen },
        { Piece.King, positiveInfinity }
    };

    static double timeLimitSeconds;
    static CancellationToken searchToken;
    static bool searchCancelled => searchToken.IsCancellationRequested;
    static long nodes;

    // Stats of the latest search, replaced after every completed iteration so the UI can show them live
    public static volatile SearchInfo? LastSearch;

    // Called after every completed iteration, on the searching thread; the UCI mode reports each one as an info line
    public static Action<SearchInfo>? IterationCompleted;

    public static Move currentBestMove = Move.NullMove;
    public static Move overallBestMove = Move.NullMove;

    // Entry point: searches for best move within a time budget, or until the caller cancels it
    // Plays at the chosen Level unless another is given, as for a hint
    // A UCI controller can also give the clock's increment, fix the think time (go movetime; infinity for go infinite)
    // or cap the depth (go depth)
    public static Move Think(Board board, double timeLeft, CancellationToken cancel = default, Difficulty? level = null,
                             double increment = 0, double? thinkTime = null, int? depthLimit = null) {
        overallBestMove = Move.NullMove;
        nodes = 0;
        var (maxDepth, maxThinkTime, noise) = LevelSettings(level ?? Level);

        // Most of each increment can be spent, as it comes back after the move, but never more than half the clock
        double clockTime = Math.Min(GetThinkTime(board, timeLeft) + increment * 0.75, Math.Max(timeLeft / 2, 0.01));
        timeLimitSeconds = thinkTime ?? Math.Min(clockTime, maxThinkTime);
        if (depthLimit != null) maxDepth = Math.Min(maxDepth, depthLimit.Value);

        // Each search gets its own timer, so a timer left over from an earlier search cannot cut this one short
        using var searchTimer = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        if (!double.IsPositiveInfinity(timeLimitSeconds)) searchTimer.CancelAfter(TimeSpan.FromSeconds(timeLimitSeconds));
        searchToken = searchTimer.Token;

        int depthSearched = 0, eval = 0;
        TranspositionTable.Clear();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        SearchInfo info = new SearchInfo(Move.NullMove, 0, 0, 0, 0, 0, timeLimitSeconds, DateTime.Now, Thinking: true);
        LastSearch = info;

        // Iterative deepening loop
        for (int depth = 1; depth <= maxDepth; depth++) {
            if (searchCancelled) break;

            currentBestMove = Move.NullMove;
            depthSearched = depth;
            eval = Search(board, depth);

            if (!currentBestMove.IsNull) {
                overallBestMove = currentBestMove;
            }

            // A cancelled iteration returns a meaningless score, so only completed ones are reported
            if (!searchCancelled) {
                int whiteEval = board.IsWhiteTurn ? eval : -eval;
                info = info with {
                    Move = overallBestMove, Eval = whiteEval, MateIn = MateIn(whiteEval), Depth = depth, Nodes = nodes,
                    Seconds = stopwatch.Elapsed.TotalSeconds, Line = PrincipalVariation(board, overallBestMove, depth)
                };
                LastSearch = info;
                IterationCompleted?.Invoke(info);
            }
        }

        if (noise > 0 && !searchCancelled) overallBestMove = NoisyBestMove(board, depthSearched, noise);

        LastSearch = info with { Move = overallBestMove, Nodes = nodes, Seconds = stopwatch.Elapsed.TotalSeconds, Thinking = false };
        return overallBestMove;
    }

    // The line the search expects: the best move, then the best reply stored in the transposition table for each position after it
    // Stops at a move that isn't legal (the entry may belong to another position) or at a repeated position
    static List<Move> PrincipalVariation(Board board, Move first, int maxLength) {
        var line = new List<Move>();
        var seen = new HashSet<ulong>();

        Move move = first;
        while (!move.IsNull && line.Count < maxLength && seen.Add(board.ZobristKey)) {
            if (!MoveGenerator.GenerateMoves(board).Contains(move)) break;

            board.MakeMove(move);
            line.Add(move);
            move = TranspositionTable.TryProbe(board.ZobristKey, out HashEntry entry) ? entry.move : Move.NullMove;
        }

        for (int i = line.Count - 1; i >= 0; i--) board.UnmakeMove(line[i]);
        return line;
    }

    // Scores every move with random noise added, so weaker levels sometimes play a slightly worse move
    // Mates and clear material swings still outweigh the noise
    static Move NoisyBestMove(Board board, int depth, int noise) {
        Move best = Move.NullMove;
        int bestScore = negativeInfinity;

        foreach (Move move in MoveGenerator.GenerateMoves(board)) {
            board.MakeMove(move);
            int score = -Search(board, depth - 1, negativeInfinity, positiveInfinity, 1) + Random.Shared.Next(-noise, noise + 1);
            board.UnmakeMove(move);

            if (searchCancelled) return overallBestMove;
            if (score > bestScore) {
                bestScore = score;
                best = move;
            }
        }

        return best.IsNull ? overallBestMove : best;
    }

    // Main minimax search with alpha-beta pruning; ply is the distance from the root
    public static int Search(Board board, int depth, int alpha = negativeInfinity, int beta = positiveInfinity, int ply = 0) {
        bool firstCall = ply == 0;
        if (searchCancelled) return 0;
        nodes++;

        if (board.CountZobristKeys(board.ZobristKey) >= 3) return 0; // Threefold repetition draw

        int hashF = HashFlag.ALPHA;

        // Probe transposition table for prior result
        int? val = TranspositionTable.ProbeHash(board, depth, alpha, beta, ply);
        if (val.HasValue) {
            if (firstCall) {
                Move move = TranspositionTable.GetEntry(board.ZobristKey).move;
                if (!move.IsNull) currentBestMove = move;
            }
            return val.Value;
        }

        if (depth == 0) return QuiescenceSearch(board, alpha, beta); // Quiescence search

        List<Move> moves = MoveGenerator.GenerateMoves(board);
        Order(ref moves, board); // Improve pruning efficiency

        if (moves.Count == 0) {
            if (MoveHelper.IsInCheck(board, board.IsWhiteTurn)) {
                return checkmate + ply; // Mate found, a quicker mate scores further from zero
            }
            return 0; // Stalemate
        }

        Move bestMoveThisPosition = Move.NullMove;

        foreach (Move move in moves) {
            board.MakeMove(move);
            int eval = -Search(board, depth - 1, -beta, -alpha, ply + 1);
            board.UnmakeMove(move);

            if (searchCancelled) return 0;

            if (eval >= beta) {
                // Move too good, opponent will not allow it
                TranspositionTable.RecordHash(board, depth, beta, HashFlag.BETA, move, ply);
                return beta;
            }

            if (eval > alpha) {
                // Best score so far
                hashF = HashFlag.EXACT;
                alpha = eval;
                bestMoveThisPosition = move;

                if (firstCall) currentBestMove = move;
            }
        }

        TranspositionTable.RecordHash(board, depth, alpha, hashF, bestMoveThisPosition, ply);
        return alpha;
    }

    public static bool IsMateScore(int eval) => Math.Abs(eval) > -checkmate - maxMatePly;

    // Mate scores are checkmate + the ply of the mate, counted from the root.
    // Returns moves until mate, signed like the eval, or 0 when no mate was found
    static int MateIn(int eval) {
        if (!IsMateScore(eval)) return 0;

        int matePly = -checkmate - Math.Abs(eval);
        int movesToMate = (matePly + 1) / 2;
        return eval > 0 ? movesToMate : -movesToMate;
    }

    // Quiescence search to avoid horizon effect on captures
    public static int QuiescenceSearch(Board board, int alpha, int beta) {
        if (searchCancelled) return 0;
        nodes++;

        if (board.CountZobristKeys(board.ZobristKey) >= 3) return 0; // Threefold repetition draw

        int eval = Evaluation.Evaluate(board);
        if (eval >= beta) return beta;
        alpha = Math.Max(alpha, eval);

        List<Move> moves = MoveGenerator.GenerateCaptureMoves(board);
        Order(ref moves, board);

        foreach (Move move in moves) {
            board.MakeMove(move);
            eval = -QuiescenceSearch(board, -beta, -alpha);
            board.UnmakeMove(move);

            if (searchCancelled) return 0;
            if (eval >= beta) return beta;

            alpha = Math.Max(alpha, eval);
        }

        return alpha;
    }

    // Orders moves to improve alpha-beta efficiency
    public static void Order(ref List<Move> moves, Board board) {
        // Squares attacked by the opponent are the same for every move, so compute them once
        Bitboard unsafeSquares = MoveHelper.GetUnsafeSquares(board, board.IsWhiteTurn);

        Move[] sorted = moves.ToArray();
        int[] scores = new int[sorted.Length];
        for (int i = 0; i < sorted.Length; i++) scores[i] = -Score(sorted[i], board, unsafeSquares);

        Array.Sort(scores, sorted); // Ascending on negated scores = best move first
        moves.Clear();
        moves.AddRange(sorted);
    }

    // Heuristic move scoring for ordering
    public static int Score(Move move, Board board, Bitboard unsafeSquares) {
        if (move == overallBestMove) return positiveInfinity; // Prioritize best move found in previous iterations

        int score = 0;

        int movedPiece = board.Square[move.Source].Type;
        int capturedPiece = board.Square[move.Target].Type;

        // MVV-LVA: prioritize high-value captures with low-value attackers
        if (capturedPiece != Piece.None)
            score += 10 * PieceValue[capturedPiece] - PieceValue[movedPiece];

        // Promote earlier
        if (move.IsPromotion)
            score += PieceValue[move.PromotingTo];

        // Discourage moving into danger
        if (unsafeSquares.Contains(move.Target))
            score -= PieceValue[movedPiece];

        return score;
    }

    // Dynamic time allocation based on game phase and time left
    public static double GetThinkTime(Board board, double timeLeft) {
        const double minTime = 0.05;  // Minimum think time per move
        const double maxTime = 3.0;   // Maximum think time per move

        int pieceCount = 0;
        foreach (var square in board.Square) {
            if (square.Type != Piece.None) pieceCount++;
        }

        bool isOpening = pieceCount > 24;
        bool isEndgame = pieceCount < 12;
        int estimatedMovesRemaining = isEndgame ? 20 : (isOpening ? 40 : 30);

        double baseTime = timeLeft / estimatedMovesRemaining;

        double flexibilityMultiplier = timeLeft > 60 ? 1.5 : (timeLeft < 10 ? 0.5 : 1.0);

        double thinkTime = baseTime * flexibilityMultiplier;

        // Divide by 10 to preserve a buffer for future moves
        thinkTime = Math.Clamp(thinkTime / 10, minTime, maxTime);

        return thinkTime;
    }
}

// Eval is in centipawns and MateIn in moves, both from white's point of view. MateIn is 0 when no mate was found
record SearchInfo(Move Move, int Eval, int MateIn, int Depth, long Nodes, double Seconds, double TimeLimit, DateTime StartedAt, bool Thinking = false, bool FromBook = false, List<Move>? Line = null);
