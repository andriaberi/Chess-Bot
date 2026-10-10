namespace Chess.UI;

using Chess.Utility;
using Raylib_cs;

public class Status {
    // Card in the left column, between the eval graph and the Hint and Resign buttons, announcing how the game ended
    // The title gives the result ("White Wins", "Draw") and the caption under it the reason ("Checkmate", "Repetition", ...)
    private readonly string caption;
    private readonly string title;
    private readonly Color color;

    private const int Width = UIHelper.LeftColumnWidth;
    private const int Padding = 20;
    private const int AccentHeight = 4;
    private const int TitleFontSize = 48;
    private const int CaptionFontSize = 30;
    public const int Height = AccentHeight + Padding + TitleFontSize + CaptionFontSize + Padding;
    public static int Top => GameControls.Top - UIHelper.ColumnGap - Height;

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
        int y = Top;
        float centerX = x + Width / 2f;

        Raylib.DrawRectangle(x, y, Width, Height, Theme.ButtonColor);
        Raylib.DrawRectangle(x, y, Width, AccentHeight, color);

        int contentY = y + AccentHeight + Padding;
        UIHelper.DrawTextCentered(titleFont, title, centerX, contentY, TitleFontSize, Theme.ButtonHoverTextColor);
        UIHelper.DrawTextCentered(captionFont, caption, centerX, contentY + TitleFontSize, CaptionFontSize, color);
    }
}
