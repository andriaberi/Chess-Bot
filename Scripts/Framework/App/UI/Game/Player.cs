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

    private Font font = UIHelper.LoadFont(fontSize);
    private Color fontColor = Theme.PlayerTextColor;

    // Each name sits by its own side of the board, centered in the gap between the board's frame and the window edge
    private void RenderName(string text, bool isWhite) {
        bool isOnTop = isWhite != Settings.FromWhitesView;
        int centerY = isOnTop ? UIHelper.TopBarCenterY : UIHelper.BottomBarCenterY;

        int x = UIHelper.GetScreenX(Settings.FromWhitesView ? 0 : 7);
        Vector2 size = Raylib.MeasureTextEx(font, text, fontSize, 1);

        Raylib.DrawTextEx(font, text, new Vector2(x, centerY - size.Y / 2), fontSize, 1, fontColor);
    }

    public void Render() {
        RenderName($"White: {whitePlayer}", true);
        RenderName($"Black: {blackPlayer}", false);
    }
}