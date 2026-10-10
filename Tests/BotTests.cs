namespace Chess.Tests;

using Chess.API;
using Chess.ChessEngine;
using static TestHelpers;

public class BotTests {
    // With 5 minutes on the clock the bot thinks for about a second per move
    private const double Clock = 300;

    [Fact]
    public void FindsMateInOne() {
        Board board = new Board("");
        Play(board, "f2f3", "e7e5", "g2g4");

        Move move = Chess.Bot.Bot.Think(board, Clock);

        Assert.Equal("d8h4", Uci(move));
    }

    [Fact]
    public void ReportsMateDistance() {
        // Black mates in one: reported from white's point of view
        Board board = new Board("");
        Play(board, "f2f3", "e7e5", "g2g4");
        Chess.Bot.Bot.Think(board, Clock);
        Assert.Equal(-1, Chess.Bot.Bot.LastSearch!.MateIn);

        // Rook ladder: 1. Ra7 Kg8 2. Rb8#, with no mate in one
        board = new Board("R3K3/1R6/8/8/8/8/8/7k w - - 0 1");
        Chess.Bot.Bot.Think(board, Clock);
        Assert.Equal(2, Chess.Bot.Bot.LastSearch!.MateIn);
    }

    [Fact]
    public void TranspositionTable_KeepsMateDistanceWhenReachedAtAnotherPly() {
        // Stored 3 plies from the root with a mate 4 plies further on (ply 7), then reached again at ply 1: mate is at ply 5
        Board board = new Board("");
        Chess.Bot.TranspositionTable.Clear();
        Chess.Bot.TranspositionTable.RecordHash(board, 5, -1000000 + 7, Chess.Bot.HashFlag.EXACT, Move.NullMove, 3);

        Assert.Equal(-1000000 + 5, Chess.Bot.TranspositionTable.ProbeHash(board, 5, -2000000, 2000000, 1));
    }

    [Theory]
    [InlineData("2B1K3/8/8/8/8/7n/8/4k3 w - - 0 1")] // K+B vs K+N
    [InlineData("4K3/8/2B5/8/8/8/8/4k3 w - - 0 1")]  // K+B vs K
    [InlineData("4K3/8/2NN4/8/8/8/8/4k3 b - - 0 1")] // K+N+N vs K
    public void Evaluate_IsLevelWhenNeitherSideCanForceMate(string fen) {
        Assert.Equal(0, Chess.Bot.Evaluation.Evaluate(new Board(fen)));
    }

    [Fact]
    public void Evaluate_FavorsBishopAndKnightAgainstABareKing() {
        Assert.True(Chess.Bot.Evaluation.Evaluate(new Board("4K3/8/2BN4/8/8/8/8/4k3 w - - 0 1")) > 0);
    }

    [Fact]
    public void CapturesAHangingQueen() {
        Board board = new Board("");
        Play(board, "e2e4", "d7d5", "d1g4"); // The queen on g4 can be taken by the c8 bishop

        Move move = Chess.Bot.Bot.Think(board, Clock);

        Assert.Equal("c8g4", Uci(move));
    }

    [Fact]
    public void CancelledSearch_StopsEarlyAndLeavesTheBoardUnchanged() {
        Board board = new Board("");
        string before = Snapshot(board);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Chess.Bot.Bot.Think(board, Clock, cancel.Token);

        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(500), $"Search ran for {stopwatch.Elapsed.TotalMilliseconds} ms after being cancelled");
        Assert.Equal(before, Snapshot(board));
    }

    [Fact]
    public void Search_LeavesTheBoardUnchanged() {
        Board board = new Board("R3K2R/PPPBBPPP/2N2Q1p/1p2P3/3PN3/bn2pnp1/p1ppqpb1/r3k2r w KQkq - 0 20");
        string before = Snapshot(board);

        Move move = Chess.Bot.Bot.Think(board, Clock);

        Assert.Equal(before, Snapshot(board));
        Assert.Contains(MoveGenerator.GenerateMoves(board), legal => legal == move);
    }
}
