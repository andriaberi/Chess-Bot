namespace Chess.Core;

using System.Text;

class Pgn {
    // Portable Game Notation: tag pairs, then the moves, e.g. "1. e4 e5 2. Nf3 Nc6 1-0"
    // Other chess programs and sites can import it, and it reads fine as plain text
    private const string StandardStartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private const int LineLength = 80;

    // Result is "1-0", "0-1", "1/2-1/2", or "*" for a game still in progress
    public static string Write(ChessEngine.Board board, string white, string black, string result) {
        var pgn = new StringBuilder();

        void Tag(string name, string value) => pgn.AppendLine($"[{name} \"{value}\"]");

        Tag("Event", "Casual game");
        Tag("Site", "Chess-Bot");
        Tag("Date", DateTime.Now.ToString("yyyy.MM.dd"));
        Tag("Round", "-");
        Tag("White", white);
        Tag("Black", black);
        Tag("Result", result);

        // A game set up from another position records where it started
        if (board.StartFen != StandardStartFen) {
            Tag("SetUp", "1");
            Tag("FEN", board.StartFen);
        }

        pgn.AppendLine();

        var tokens = new List<string>();
        int plies = board.MovesNotation.Count; // Read once, the bot's thread may add a move meanwhile
        for (int i = 0; i < plies; i++) {
            int ply = board.FirstPly + i;
            int moveNumber = ply / 2 + 1;

            if (ply % 2 == 0) tokens.Add($"{moveNumber}.");
            else if (i == 0) tokens.Add($"{moveNumber}...");

            tokens.Add(board.MovesNotation[i]);
        }
        tokens.Add(result);

        // Lines are kept under 80 characters, as the PGN standard asks
        var line = new StringBuilder();
        foreach (string token in tokens) {
            if (line.Length > 0 && line.Length + 1 + token.Length > LineLength) {
                pgn.AppendLine(line.ToString());
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(token);
        }
        pgn.AppendLine(line.ToString());

        return pgn.ToString();
    }
}
