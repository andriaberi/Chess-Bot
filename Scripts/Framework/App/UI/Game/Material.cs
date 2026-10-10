namespace Chess.UI;

using Chess.Utility;
using Raylib_cs;
using System.Numerics;

class Material {
    // The pieces a side is up (the opponent's pieces it has taken, net of its own losses) and the points they're worth,
    // drawn after the player's name as on lichess and chess.com
    private const int IconSize = 40;
    private const int IconOverlap = 18; // Pieces of the same kind overlap, like a small stack
    private const int IconGroupGap = 6;
    private const int FontSize = 30;

    private static readonly int[] order = { API.Piece.Queen, API.Piece.Rook, API.Piece.Bishop, API.Piece.Knight, API.Piece.Pawn };
    private static readonly Dictionary<int, (char Letter, int Points)> pieces = new() {
        { API.Piece.Queen, ('q', 9) }, { API.Piece.Rook, ('r', 5) }, { API.Piece.Bishop, ('b', 3) }, { API.Piece.Knight, ('n', 3) }, { API.Piece.Pawn, ('p', 1) }
    };

    private static Font font = UIHelper.LoadFont(FontSize);
    private static Dictionary<(int Type, bool IsWhite), Texture2D>? icons;

    public static void Render(ChessEngine.Board board, bool isWhite, int x, int centerY) {
        // Counted from the FEN, which the bot's search leaves alone, rather than from the board it is searching on
        string placement = board.Fen.Split(' ')[0];
        int Count(char letter) => placement.Count(c => c == letter);

        int y = centerY - IconSize / 2;
        int lead = 0;

        foreach (int type in order) {
            var (letter, points) = pieces[type];
            int up = isWhite ? Count(char.ToUpper(letter)) - Count(letter) : Count(letter) - Count(char.ToUpper(letter));
            lead += up * points;
            if (up <= 0) continue;

            // The pieces shown are the opponent's; black ones are drawn as gray white pieces to stay visible
            Texture2D icon = Icon(type, true);
            Color tint = isWhite ? Theme.CapturedBlackTint : Color.White;
            for (int i = 0; i < up; i++) {
                Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height), new Rectangle(x, y, IconSize, IconSize), Vector2.Zero, 0, tint);
                x += IconOverlap;
            }
            x += IconSize - IconOverlap + IconGroupGap;
        }

        if (lead > 0) {
            Vector2 size = Raylib.MeasureTextEx(font, $"+{lead}", FontSize, 1);
            Raylib.DrawTextEx(font, $"+{lead}", new Vector2(x, centerY - size.Y / 2), FontSize, 1, Theme.ButtonTextColor);
        }
    }

    private static Texture2D Icon(int type, bool isWhite) {
        if (icons == null) {
            icons = new();
            foreach (int pieceType in order) {
                foreach (bool white in new[] { true, false }) {
                    API.Piece piece = new API.Piece(pieceType, white ? API.Piece.White : API.Piece.Black);
                    icons[(pieceType, white)] = UIHelper.LoadPieceTexture(piece);
                }
            }
        }
        return icons[(type, isWhite)];
    }
}
