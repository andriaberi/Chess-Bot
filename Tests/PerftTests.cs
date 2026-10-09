namespace Chess.Tests;

using Chess.ChessEngine;

// Perft counts every legal move sequence to a fixed depth and compares it with known-correct totals,
// which catches almost any move generation bug (castling, en passant, pins, promotions, checks)
public class PerftTests {
    // Positions and expected counts per depth, starting at depth 1
    private static readonly (string Fen, long[] Counts)[] Positions = [
        ("RNBQKBNR/PPPPPPPP/8/8/8/8/pppppppp/rnbqkbnr w KQkq - 0 1",
            [20, 400, 8902, 197281, 4865609]),
        ("RNBQK2R/PPP1NnPP/8/2B5/8/2p5/pp1Pbppp/rnbq1k1r w KQ - 1 8",
            [44, 1486, 62379, 2103487]),
        ("R3K2R/PPPBBPPP/2N2Q1p/1p2P3/3PN3/bn2pnp1/p1ppqpb1/r3k2r w KQkq - 0 20",
            [48, 2039, 97862, 4085603]),
        ("8/4P1P1/8/1R3p1k/KP5r/3p4/2p5/8 w - - 0 40",
            [14, 191, 2812, 43238, 674624, 11030083]),
        ("R2Q1RK1/Pp1P2PP/q4N2/BBP1P3/nP6/1b3nbN/Pppp1ppp/r3k2r w kq - 0 1",
            [6, 264, 9467, 422333, 15833292]),
        ("R4RK1/1PP1QPPP/P1NP1N2/2B1P1b1/2b1p1B1/p1np1n2/1pp1qppp/r4rk1 w - - 0 10",
            [46, 2079, 89890, 3894594]),
    ];

    public static IEnumerable<object[]> Cases() {
        foreach (var (fen, counts) in Positions) {
            for (int depth = 1; depth <= counts.Length; depth++) {
                yield return [fen, depth, counts[depth - 1]];
            }
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Perft(string fen, int depth, long expected) {
        Assert.Equal(expected, TestHelpers.Perft(new Board(fen), depth));
    }
}
