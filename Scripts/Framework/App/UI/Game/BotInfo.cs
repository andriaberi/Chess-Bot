namespace Chess.UI;

using Chess.API;
using Chess.Bot;
using Chess.Utility;
using Raylib_cs;
using System.Numerics;

class BotInfo {
    // Panel in the left column, under the menu button, showing the stats of the bot's latest search
    private const int Width = UIHelper.LeftColumnWidth;
    private const int Padding = 22;
    private const int ProgressHeight = 4;
    private const int CaptionFontSize = 30;
    private const int MoveFontSize = 56;
    private const int FontSize = 34;
    private const int RowHeight = 46;
    private const int RowCount = 4;

    private const int Height = ProgressHeight + Padding + CaptionFontSize + MoveFontSize + Padding + 2 + Padding / 2 + RowCount * RowHeight + Padding / 2;
    public static int Top => Menu.Bottom + UIHelper.ColumnGap;
    public static int Bottom => Top + Height;

    private static Font captionFont = UIHelper.LoadFont(CaptionFontSize);
    private static Font moveFont = UIHelper.LoadFont(MoveFontSize);
    private static Font font = UIHelper.LoadFont(FontSize);

    public void Render() {
        SearchInfo? info = Bot.LastSearch;
        bool searched = info != null && !info.FromBook;

        // While thinking, time is shown live rather than as of the last completed depth
        double seconds = info == null ? 0 : info.Thinking ? (DateTime.Now - info.StartedAt).TotalSeconds : info.Seconds;

        (string Label, string Value)[] rows = {
            ("Depth", searched && info!.Depth > 0 ? info.Depth.ToString() : "-"),
            ("Nodes", searched ? FormatCount(info!.Nodes) : "-"),
            ("Speed", searched && info!.Seconds > 0 ? FormatCount((long) (info.Nodes / info.Seconds)) + "/s" : "-"),
            ("Time", searched ? $"{seconds:0.00}s" : "-"),
        };

        int x = UIHelper.LeftColumnX;
        int y = Top;

        Raylib.DrawRectangle(x, y, Width, Height, Theme.ButtonColor);

        // Think time used so far, out of the time the bot gave itself for this move
        if (info != null && info.Thinking && info.TimeLimit > 0) {
            float progress = (float) Math.Clamp(seconds / info.TimeLimit, 0, 1);
            Raylib.DrawRectangle(x, y, (int) (Width * progress), ProgressHeight, Theme.ButtonHoverColor);
        }

        int contentY = y + ProgressHeight + Padding;
        string caption = info != null && info.FromBook ? "Book Move" : "Best Move";
        Raylib.DrawTextEx(captionFont, caption, new Vector2(x + Padding, contentY), CaptionFontSize, 1, Theme.ButtonTextColor);
        contentY += CaptionFontSize;

        string move = info == null || info.Move.IsNull ? "-" : FormatMove(info.Move);
        string eval = searched && info!.Depth > 0 ? FormatEval(info) : "";
        Raylib.DrawTextEx(moveFont, move, new Vector2(x + Padding, contentY), MoveFontSize, 1, Theme.ButtonHoverTextColor);
        DrawRightAligned(moveFont, eval, x + Width - Padding, contentY, MoveFontSize, Theme.ButtonTextColor);
        contentY += MoveFontSize + Padding;

        Raylib.DrawRectangle(x + Padding, contentY, Width - Padding * 2, 2, Theme.ButtonTextColor);
        contentY += 2 + Padding / 2;

        int rowY = contentY + (RowHeight - FontSize) / 2;
        foreach (var (label, value) in rows) {
            Raylib.DrawTextEx(font, label, new Vector2(x + Padding, rowY), FontSize, 1, Theme.ButtonTextColor);
            DrawRightAligned(font, value, x + Width - Padding, rowY, FontSize, Theme.ButtonHoverTextColor);
            rowY += RowHeight;
        }
    }

    private static void DrawRightAligned(Font font, string text, int right, int y, int fontSize, Color color) {
        Vector2 size = Raylib.MeasureTextEx(font, text, fontSize, 1);
        Raylib.DrawTextEx(font, text, new Vector2(right - size.X, y), fontSize, 1, color);
    }

    // Long algebraic notation, e.g. e2e4 or e7e8q
    private static string FormatMove(Move move) {
        string promotion = move.Flag switch {
            Move.QueenPromotion => "q",
            Move.RookPromotion => "r",
            Move.BishopPromotion => "b",
            Move.KnightPromotion => "n",
            _ => ""
        };
        return $"{move.SourceCoord}{move.TargetCoord}{promotion}";
    }

    private static string FormatEval(SearchInfo info) {
        if (info.MateIn != 0) return info.MateIn > 0 ? $"M{info.MateIn}" : $"-M{-info.MateIn}";
        return (info.Eval / 100.0).ToString("+0.00;-0.00;0.00");
    }

    private static string FormatCount(long count) {
        if (count >= 1_000_000) return $"{count / 1_000_000.0:0.0}M";
        if (count >= 1_000) return $"{count / 1_000.0:0.0}k";
        return count.ToString();
    }
}
