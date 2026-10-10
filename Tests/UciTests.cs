namespace Chess.Tests;

using Chess.Core;

public class UciTests {
    // Runs the commands to the end of the input, which waits for any search to finish, and returns the engine's lines
    private static string[] Run(params string[] commands) {
        var output = new StringWriter();
        new Uci(new StringReader(string.Join('\n', commands)), output).Run();
        return output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public void Handshake() {
        string[] lines = Run("uci", "isready");

        Assert.StartsWith("id name Chess-Bot", lines[0]);
        Assert.Contains("option name Difficulty type combo default Hard var Easy var Medium var Hard", lines);
        Assert.Equal("uciok", lines[^2]);
        Assert.Equal("readyok", lines[^1]);
    }

    [Fact]
    public void PlaysTheMoveFromAPositionWithMoves() {
        string[] lines = Run("position startpos moves f2f3 e7e5 g2g4", "go depth 3");
        Assert.Equal("bestmove d8h4", lines[^1]);
    }

    [Fact]
    public void PlaysFromAFen() {
        // White mates with Ra8
        string[] lines = Run("position fen 6k1/5ppp/8/8/8/8/5PPP/R5K1 w - - 0 1", "go depth 3");
        Assert.Equal("bestmove a1a8", lines[^1]);
    }

    [Fact]
    public void FindsAMoveOnTheClock() {
        string[] lines = Run("position startpos moves e2e4", "go wtime 1000 btime 1000 winc 10 binc 10");
        Assert.Matches("^bestmove [a-h][1-8][a-h][1-8]$", lines[^1]);
    }

    [Fact]
    public void ReportsNoMoveWhenThereIsNone() {
        // Checkmated
        string[] lines = Run("position startpos moves f2f3 e7e5 g2g4 d8h4", "go depth 2");
        Assert.Equal("bestmove 0000", lines[^1]);
    }

    [Fact]
    public void StopsAnInfiniteSearch() {
        // The stop arrives right after go, so this only finishes if stop cuts the search short
        string[] lines = Run("position startpos", "go infinite", "stop");
        Assert.Matches("^bestmove [a-h][1-8][a-h][1-8]$", lines[^1]);
    }

    [Fact]
    public void DifficultyOptionLeavesTheWindowsLevelAlone() {
        Run("setoption name Difficulty value Easy");
        Assert.Equal(Chess.Bot.Difficulty.Hard, Chess.Bot.Bot.Level);
    }
}
