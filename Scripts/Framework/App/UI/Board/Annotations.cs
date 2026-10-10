namespace Chess.UI;

using Chess.Utility;
using Raylib_cs;

class Annotations {
    // Arrows and circled squares drawn with the right mouse button, as on lichess:
    // right-drag between two squares draws an arrow, right-click on one square circles it, and doing either again removes it
    // A left click on the board or the next move clears them all
    private const float CircleWidth = 8;

    private readonly List<(int From, int To)> arrows = new();
    private readonly HashSet<int> circles = new();

    private int dragStart = -1; // Square the right button went down on, while it is held
    private int movesSeen = -1;

    public void Update(ChessEngine.Board board) {
        int moves = board.MovesNotation.Count;
        if (moves != movesSeen) {
            movesSeen = moves;
            Clear();
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && UIHelper.SquareUnderMouse() != -1) Clear();

        if (Raylib.IsMouseButtonPressed(MouseButton.Right)) dragStart = UIHelper.SquareUnderMouse();

        if (Raylib.IsMouseButtonReleased(MouseButton.Right) && dragStart != -1) {
            int end = UIHelper.SquareUnderMouse();

            if (end == dragStart) {
                if (!circles.Remove(end)) circles.Add(end);
            } else if (end != -1) {
                if (!arrows.Remove((dragStart, end))) arrows.Add((dragStart, end));
            }

            dragStart = -1;
        }
    }

    public void Clear() {
        arrows.Clear();
        circles.Clear();
        dragStart = -1;
    }

    public void Render() {
        float radius = Settings.SquareSideLength / 2f;
        foreach (int square in circles) {
            Raylib.DrawRing(UIHelper.SquareCenter(square), radius - CircleWidth, radius, 0, 360, 48, Theme.AnnotationCol);
        }

        foreach (var (from, to) in arrows) UIHelper.DrawArrow(from, to, Theme.AnnotationCol);

        // The arrow being dragged follows the mouse
        int hovered = UIHelper.SquareUnderMouse();
        if (dragStart != -1 && hovered != -1 && hovered != dragStart) UIHelper.DrawArrow(dragStart, hovered, Theme.AnnotationCol);
    }
}
