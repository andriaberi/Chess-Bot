namespace Chess.UI;

using Chess.Utility;
using Raylib_cs;
using System.Numerics;

class MoveHistory {
    // Panel in the column right of the board listing the moves played, one row per move number
    // It follows the latest move, and the mouse wheel scrolls back through earlier ones
    private const int Width = UIHelper.LeftColumnWidth;
    private const int Padding = 22;
    private const int CaptionFontSize = 35;
    private const int FontSize = 32;
    private const int RowHeight = 44;
    private const int NumberWidth = 56;
    private const int MoveWidth = 110;

    private static Font captionFont = UIHelper.LoadFont(CaptionFontSize);
    private static Font font = UIHelper.LoadFont(FontSize);

    private int scroll = 0; // Rows scrolled back from the latest one
    private int movesSeen = 0;

    public void Render(ChessEngine.Board board) {
        int plies = board.MovesNotation.Count; // Read once, the bot's thread may add a move meanwhile

        // As tall as the board, frame included
        int x = UIHelper.RightColumnX;
        int y = UIHelper.BoardTop;
        int height = UIHelper.BoardBottom - y;

        Raylib.DrawRectangle(x, y, Width, height, Theme.ButtonColor);

        int contentY = y + Padding;
        Raylib.DrawTextEx(captionFont, "Moves", new Vector2(x + Padding, contentY), CaptionFontSize, 1, Theme.ButtonTextColor);
        contentY += CaptionFontSize + Padding;

        Raylib.DrawRectangle(x + Padding, contentY, Width - Padding * 2, 1, Theme.ButtonTextColor);
        contentY += 2 + Padding / 2;

        // Plies are counted from the start of the game, so a position set up with black to move starts with "1..."
        // Taken from where the game started, since the bot's search changes the side to move while it thinks
        int firstPly = board.FirstPly;
        int firstRow = firstPly / 2;
        int rowCount = plies == 0 ? 0 : (firstPly + plies - 1) / 2 - firstRow + 1;
        int visibleRows = (y + height - Padding / 2 - contentY) / RowHeight;

        // A new move brings the list back to the latest one
        if (plies != movesSeen) {
            movesSeen = plies;
            scroll = 0;
        }

        Rectangle bounds = new Rectangle(x, y, Width, height);
        if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), bounds)) scroll += (int) Raylib.GetMouseWheelMove();
        scroll = Math.Clamp(scroll, 0, Math.Max(0, rowCount - visibleRows));

        int startRow = Math.Max(0, rowCount - visibleRows - scroll);
        int endRow = Math.Min(rowCount, startRow + visibleRows);

        for (int row = startRow; row < endRow; row++) {
            int rowY = contentY + (row - startRow) * RowHeight;
            int textY = rowY + (RowHeight - FontSize) / 2;

            Raylib.DrawTextEx(font, $"{firstRow + row + 1}.", new Vector2(x + Padding, textY), FontSize, 1, Theme.ButtonTextColor);

            for (int side = 0; side < 2; side++) {
                int ply = (firstRow + row) * 2 + side - firstPly;
                if (ply >= plies) break;

                int moveX = x + Padding + NumberWidth + side * MoveWidth;
                string text = ply < 0 ? "..." : board.MovesNotation[ply];

                // The latest move is marked like a hovered button
                bool latest = ply == plies - 1;
                if (latest) Raylib.DrawRectangle(moveX - 8, rowY + 4, MoveWidth - 4, RowHeight - 8, Theme.ButtonHoverColor);

                Color color = ply < 0 ? Theme.ButtonTextColor : Theme.ButtonHoverTextColor;
                Raylib.DrawTextEx(font, text, new Vector2(moveX, textY), FontSize, 1, color);
            }
        }
    }
}
