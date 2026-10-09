namespace Chess.Tests;

using Chess.ChessEngine;
using Xunit.Abstractions;
using Xunit.Sdk;

// Perft counts every legal move sequence to a fixed depth and compares it with known-correct totals,
// which catches almost any move generation bug (castling, en passant, pins, promotions, checks)
[TestCaseOrderer("Chess.Tests.PerftOrderer", "Tests")]
public class PerftTests : IClassFixture<PerftWarmup> {
    // Positions and expected counts per depth, starting at depth 1
    // Names follow the Chess Programming Wiki's perft results page, where these positions come from
    private static readonly (string Name, string Fen, long[] Counts)[] Positions = [
        ("Initial", "RNBQKBNR/PPPPPPPP/8/8/8/8/pppppppp/rnbqkbnr w KQkq - 0 1",
            [20, 400, 8902, 197281, 4865609]),
        ("Kiwipete", "R3K2R/PPPBBPPP/2N2Q1p/1p2P3/3PN3/bn2pnp1/p1ppqpb1/r3k2r w KQkq - 0 20",
            [48, 2039, 97862, 4085603]),
        ("Position 3", "8/4P1P1/8/1R3p1k/KP5r/3p4/2p5/8 w - - 0 40",
            [14, 191, 2812, 43238, 674624, 11030083]),
        ("Position 4", "R2Q1RK1/Pp1P2PP/q4N2/BBP1P3/nP6/1b3nbN/Pppp1ppp/r3k2r w kq - 0 1",
            [6, 264, 9467, 422333, 15833292]),
        ("Position 5", "RNBQK2R/PPP1NnPP/8/2B5/8/2p5/pp1Pbppp/rnbq1k1r w KQ - 1 8",
            [44, 1486, 62379, 2103487]),
        ("Position 6", "R4RK1/1PP1QPPP/P1NP1N2/2B1P1b1/2b1p1B1/p1np1n2/1pp1qppp/r4rk1 w - - 0 10",
            [46, 2079, 89890, 3894594]),
    ];

    public static IEnumerable<object[]> Cases() {
        foreach (var (name, fen, counts) in Positions) {
            for (int depth = 1; depth <= counts.Length; depth++) {
                yield return [name, fen, depth, counts[depth - 1]];
            }
        }
    }

    public static int PositionIndex(string name) => Array.FindIndex(Positions, position => position.Name == name);

    // The name only labels the test (`make perft` shows it); the FEN is what's tested
    [Theory]
    [MemberData(nameof(Cases))]
    public void Perft(string name, string fen, int depth, long expected) {
        _ = name;
        Assert.Equal(expected, TestHelpers.Perft(new Board(fen), depth));
    }
}

// Runs each position from depth 1 upward, in the order listed, so the deep cases run on code the JIT has
// already optimized; in xUnit's default order a deep case can run first and be timed on unoptimized code
public class PerftOrderer : ITestCaseOrderer {
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase =>
        testCases
            .OrderBy(testCase => PerftTests.PositionIndex((string) testCase.TestMethodArguments[0]))
            .ThenBy(testCase => (int) testCase.TestMethodArguments[2]);
}

// Runs once before the first perft test, untimed: shallow perft on every position gives the JIT time to
// optimize move generation, so the timed cases measure steady-state speed rather than warm-up
public class PerftWarmup {
    public PerftWarmup() {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 500) {
            foreach (var cases in PerftTests.Cases().GroupBy(data => (string) data[1])) {
                TestHelpers.Perft(new Board(cases.Key), 3);
            }
        }
    }
}
