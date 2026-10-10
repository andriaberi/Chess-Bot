namespace Chess.UI;

using Chess.Utility;
using Raylib_cs;
using System.Numerics;

class Material {
    // The pieces a side is up (the opponent's pieces it has taken, net of its own losses),
    // drawn after the player's name as on lichess and chess.com
    private const int IconSize = 40;
    private const int IconOverlap = 18; // Pieces of the same kind overlap, like a small stack
    private const int IconGroupGap = 6;

    private static readonly (int Type, char Letter)[] order = {
        (API.Piece.Queen, 'q'), (API.Piece.Rook, 'r'), (API.Piece.Bishop, 'b'), (API.Piece.Knight, 'n'), (API.Piece.Pawn, 'p')
    };

    public static void Render(ChessEngine.Board board, bool isWhite, int x, int centerY) {
        // Counted from the FEN, which the bot's search leaves alone, rather than from the board it is searching on
        string placement = board.Fen.Split(' ')[0];
        int Count(char letter) => placement.Count(c => c == letter);

        int y = centerY - IconSize / 2;

        foreach (var (type, letter) in order) {
            int up = isWhite ? Count(char.ToUpper(letter)) - Count(letter) : Count(letter) - Count(char.ToUpper(letter));
            if (up <= 0) continue;

            // The pieces shown are the opponent's; black ones are drawn as gray white pieces to stay visible
            Texture2D icon = UIHelper.LoadPieceTexture(new API.Piece(type, API.Piece.White));
            Color tint = isWhite ? Theme.CapturedBlackTint : Color.White;
            for (int i = 0; i < up; i++) {
                Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height), new Rectangle(x, y, IconSize, IconSize), Vector2.Zero, 0, tint);
                x += IconOverlap;
            }
            x += IconSize - IconOverlap + IconGroupGap;
        }
    }
}
