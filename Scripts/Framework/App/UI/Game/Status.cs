namespace Chess.UI;

using Chess.Utility;
using Raylib_cs;

public class Status {
    // Card in the left column, between the bot panel and the menu, announcing how the game ended
    // The title gives the result ("White Wins", "Draw") and the caption under it the reason ("Checkmate", "Repetition", ...)
    private readonly string caption;
    private readonly string title;
    private readonly Color color;

    private const int Width = UIHelper.LeftColumnWidth;
    private const int Padding = 20;
    private const int AccentHeight = 4;
    private const int TitleFontSize = 48;
    private const int CaptionFontSize = 30;
    private const int Height = AccentHeight + Padding + TitleFontSize + CaptionFontSize + Padding;

    private static Font titleFont = UIHelper.LoadFont(TitleFontSize);
    private static Font captionFont = UIHelper.LoadFont(CaptionFontSize);

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
        float centerX = x + Width / 2f;

        Raylib.DrawRectangle(x, y, Width, Height, Theme.ButtonColor);
        Raylib.DrawRectangle(x, y, Width, AccentHeight, color);

        int contentY = y + AccentHeight + Padding;
        UIHelper.DrawTextCentered(titleFont, title, centerX, contentY, TitleFontSize, Theme.ButtonHoverTextColor);
        UIHelper.DrawTextCentered(captionFont, caption, centerX, contentY + TitleFontSize, CaptionFontSize, color);
    }
}
