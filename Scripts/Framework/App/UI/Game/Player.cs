namespace Chess.UI;

using Chess.Utility;
using Raylib_cs;
using System.Numerics;

class Player {
    private string whitePlayer;
    private string blackPlayer;

    public Player(string whitePlayer, string blackPlayer) {
        this.whitePlayer = whitePlayer;
        this.blackPlayer = blackPlayer;
    }

    private static int fontSize = 48;
    private const int MaterialGap = 24; // Between the name and the material it is up

    private Font font = UIHelper.LoadFont(fontSize);
    private Color fontColor = Theme.PlayerTextColor;

    // Each name sits by its own side of the board, centered in the gap between the board's frame and the window edge,
    // followed by the material that side is up
    private void RenderName(ChessEngine.Board board, string text, bool isWhite) {
        bool isOnTop = isWhite != Settings.FromWhitesView;
        int centerY = isOnTop ? UIHelper.TopBarCenterY : UIHelper.BottomBarCenterY;

        int x = UIHelper.GetScreenX(Settings.FromWhitesView ? 0 : 7);
        Vector2 size = Raylib.MeasureTextEx(font, text, fontSize, 1);

        Raylib.DrawTextEx(font, text, new Vector2(x, centerY - size.Y / 2), fontSize, 1, fontColor);
        Material.Render(board, isWhite, x + (int) size.X + MaterialGap, centerY);
    }

    public void Render(ChessEngine.Board board) {
        RenderName(board, $"White: {whitePlayer}", true);
        RenderName(board, $"Black: {blackPlayer}", false);
    }
}