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
