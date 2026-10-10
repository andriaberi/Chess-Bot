namespace Chess.UI;

using Chess.API;
using Chess.Utility;
using Raylib_cs;
using System.Numerics;

class UIHelper {
    // Finding square's x position on screen by its column index
    public static int GetScreenX(int columnIndex) {
        return Settings.BoardMarginLeft + (Settings.FromWhitesView ? columnIndex : (7 - columnIndex)) * Settings.SquareSideLength;
    }

    // Finding square's y position on screen by its row index
    public static int GetScreenY(int rowIndex) {
        return Settings.ScreenHeight / 2 - (Settings.FromWhitesView ? (rowIndex - 3) : (4 - rowIndex)) * Settings.SquareSideLength;
    }

    // The column left of the board stacks the menu button, the bot panel, the eval graph and the game result,
    // with the Hint and Resign buttons at the bottom
    public const int LeftColumnWidth = 320;
    public static int LeftColumnX => (Settings.BoardMarginLeft - Settings.BorderSize) / 2 - LeftColumnWidth / 2;

    // The column right of the board, mirroring the left one, holds the move history
    public static int RightColumnX => Settings.ScreenWidth - LeftColumnX - LeftColumnWidth;

    // Top and bottom edges of the board's frame
    public static int BoardTop => Settings.ScreenHeight / 2 - 4 * Settings.SquareSideLength - Settings.BorderSize;
    public static int BoardBottom => Settings.ScreenHeight / 2 + 4 * Settings.SquareSideLength + Settings.BorderSize;

    // Middle of the gaps between the board's frame and the top and bottom of the window, where the player names and clocks go
    public static int TopBarCenterY => BoardTop / 2;
    public static int BottomBarCenterY => (BoardBottom + Settings.ScreenHeight) / 2;

    // Spacing between the stacked items of the left column
    public const int ColumnGap = 20;

    public static int GetScreenX(Coord coord) => GetScreenX(coord.ColumnIndex);
    public static int GetScreenY(Coord coord) => GetScreenY(coord.RowIndex);

    public static Font LoadFont(int fontSize) {
        string fontPath = "Resources/Fonts/Nunito-Medium.ttf";
        return Raylib.LoadFontEx(fontPath, fontSize, null, 0);
    }

    // Index of the square under the mouse, or -1 when the mouse is off the board
    public static int SquareUnderMouse() {
        Vector2 mouse = Raylib.GetMousePosition();
        for (int i = 0; i < 64; i++) {
            Rectangle rect = new Rectangle(GetScreenX(i % 8), GetScreenY(i / 8), Settings.SquareSideLength, Settings.SquareSideLength);
            if (Raylib.CheckCollisionPointRec(mouse, rect)) return i;
        }
        return -1;
    }

    public static Vector2 SquareCenter(int square) {
        Coord coord = new Coord(square);
        return new Vector2(GetScreenX(coord) + Settings.SquareSideLength / 2f, GetScreenY(coord) + Settings.SquareSideLength / 2f);
    }

    // A straight arrow between the centers of two squares, its head ending at the target's center
    public static void DrawArrow(int fromSquare, int toSquare, Color color) {
        const float headLength = 40, headWidth = 30, thickness = 16;

        Vector2 from = SquareCenter(fromSquare);
        Vector2 to = SquareCenter(toSquare);
        Vector2 direction = Vector2.Normalize(to - from);
        Vector2 side = new Vector2(-direction.Y, direction.X);

        Vector2 headBase = to - direction * headLength;
        Raylib.DrawLineEx(from, headBase, thickness, color);

        // Raylib only fills triangles given counter-clockwise, so both windings are drawn and one is skipped
        Vector2 left = headBase + side * headWidth, right = headBase - side * headWidth;
        Raylib.DrawTriangle(to, left, right, color);
        Raylib.DrawTriangle(to, right, left, color);
    }

    public static void DrawTextCentered(Font font, string text, float centerX, float y, int fontSize, Color color) {
        Vector2 size = Raylib.MeasureTextEx(font, text, fontSize, 1);
        Raylib.DrawTextEx(font, text, new Vector2(centerX - size.X / 2, y), fontSize, 1, color);
    }

    public static string GetPieceName(API.Piece piece) {
        if (piece.IsKing) return "King";
        if (piece.IsQueen) return "Queen";
        if (piece.IsRook) return "Rook";
        if (piece.IsBishop) return "Bishop";
        if (piece.IsKnight) return "Knight";
        if (piece.IsPawn) return "Pawn";
        return "";
    }

    public static string GetPieceColor(API.Piece piece) {
        return piece.IsWhite ? "White" : "Black";
    }

    // Get an image url corresponding to the piece
    public static string GetImageNameByPiece(API.Piece piece) {
        return GetPieceColor(piece) + "/" + GetPieceName(piece) + ".png";
    }
}