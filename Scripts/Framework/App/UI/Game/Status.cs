namespace Chess.UI;

using Chess.Utility;
using Raylib_cs;
using System.Numerics;

public class Status {
    // Card in the left column, between the bot panel and the menu, announcing how the game ended
    // The caption gives the reason ("Checkmate", "Repetition", ...) and the title the result ("White Wins", "Draw")
    private readonly string caption;
    private readonly string title;
    private readonly Color color;

    private const int Width = UIHelper.LeftColumnWidth;
    private const int Padding = 22;
    private const int AccentHeight = 4;
    private const int CaptionFontSize = 30;
    private const int TitleFontSize = 48;
    private const int Height = AccentHeight + Padding + CaptionFontSize + Padding / 2 + TitleFontSize + Padding;

    private static Font captionFont = UIHelper.LoadFont(CaptionFontSize);
    private static Font titleFont = UIHelper.LoadFont(TitleFontSize);

    public static readonly Status None = new Status("", "", Color.White);

    public Status(string caption, string title, Color color) {
        this.caption = caption;
        this.title = title;
        this.color = color;
    }

    public void Render() {
        if (title == "") return;

        int x = UIHelper.LeftColumnX;
        int y = (BotInfo.Bottom + Menu.Top) / 2 - Height / 2;

        Raylib.DrawRectangle(x, y, Width, Height, Theme.DeskBackCol);
        Raylib.DrawRectangle(x, y, Width, AccentHeight, color);

        int contentY = y + AccentHeight + Padding;
        Raylib.DrawTextEx(captionFont, caption, new Vector2(x + Padding, contentY), CaptionFontSize, 1, color);
        contentY += CaptionFontSize + Padding / 2;

        Raylib.DrawTextEx(titleFont, title, new Vector2(x + Padding, contentY), TitleFontSize, 1, Theme.ButtonHoverTextColor);
    }
}
