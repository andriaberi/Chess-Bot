namespace Chess.UI;

using Chess.Bot;
using Chess.Utility;
using Raylib_cs;

class EvalGraph {
    // Panel in the left column, under the bot panel: the bot's eval over the game, like the eval bar over time
    // It ends where the result card and the buttons below it leave room
    private const int Width = UIHelper.LeftColumnWidth;
    private const int Padding = 22;
    private const int MinGraphPlies = 20; // The graph starts this wide, so early moves don't fill it

    private static int Top => BotInfo.Bottom + UIHelper.ColumnGap;
    public static int Bottom => Status.Top - UIHelper.ColumnGap;

    // White's share of the eval bar after each move the bot searched, by ply from the start of the game
    private readonly List<(int Ply, float WhiteShare)> evals = new() { (0, 0.5f) };
    private readonly object evalsLock = new();

    // Called from the bot's thread once its move is on the board
    public void Record(int ply, SearchInfo? info) {
        if (info == null || info.FromBook || info.Depth == 0) return;
        lock (evalsLock) evals.Add((ply, EvalBar.WhiteShare(info)));
    }

    public void Render(ChessEngine.Board board) {
        int x = UIHelper.LeftColumnX;
        int y = Top;
        int height = Bottom - y;

        Raylib.DrawRectangle(x, y, Width, height, Theme.ButtonColor);

        Rectangle area = new Rectangle(x + Padding, y + Padding, Width - Padding * 2, height - Padding * 2);

        (int Ply, float WhiteShare)[] points;
        lock (evalsLock) points = evals.ToArray();

        int plies = board.MovesNotation.Count;
        float span = Math.Max(MinGraphPlies, plies);

        // One column per pixel up to the latest move, holding the eval between recorded moves;
        // white's share fills from white's side, as on the eval bar
        // Columns for moves not played yet stay empty, so an early game doesn't look like either side is winning
        int segment = 0;
        for (int column = 0; column < (int) area.Width; column++) {
            float ply = column / area.Width * span;
            if (ply > plies) break;

            while (segment + 1 < points.Length && points[segment + 1].Ply <= ply) segment++;

            float share = points[segment].WhiteShare;
            if (segment + 1 < points.Length) {
                var (fromPly, fromShare) = points[segment];
                var (toPly, toShare) = points[segment + 1];
                share = fromShare + (toShare - fromShare) * (ply - fromPly) / (toPly - fromPly);
            }

            int whiteHeight = (int) (area.Height * share);
            int columnX = (int) area.X + column;
            Raylib.DrawRectangle(columnX, (int) area.Y, 1, (int) area.Height, Theme.BackgroundColor);
            if (Settings.FromWhitesView) Raylib.DrawRectangle(columnX, (int) (area.Y + area.Height) - whiteHeight, 1, whiteHeight, Theme.TimerColorLightActive);
            else Raylib.DrawRectangle(columnX, (int) area.Y, 1, whiteHeight, Theme.TimerColorLightActive);
        }

        // Marks the level point, as on the eval bar
        Raylib.DrawRectangle((int) area.X, (int) (area.Y + area.Height / 2) - 1, (int) area.Width, 2, Theme.ButtonHoverColor);
    }
}
