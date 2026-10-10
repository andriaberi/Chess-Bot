namespace Chess.UI;

using Chess.Bot;
using Chess.Utility;
using Raylib_cs;

class EvalBar {
    // Vertical bar to the right of the board showing how the bot rates the position, white's share filling from white's side
    private const int Width = 24;
    private const int Gap = 16;
    private const float TransitionSpeed = 6f;

    private float target = 0.5f;  // White's share of the bar, from the latest completed search
    private float shown = 0.5f;   // Eased towards the target, so the bar slides instead of jumping

    public void Render() {
        SearchInfo? info = Bot.LastSearch;
        if (info != null && !info.FromBook && info.Depth > 0) target = WhiteShare(info);

        shown += (target - shown) * Math.Min(1f, Raylib.GetFrameTime() * TransitionSpeed);

        int x = Settings.BoardMarginLeft + 8 * Settings.SquareSideLength + Settings.BorderSize + Gap;
        int y = Settings.ScreenHeight / 2 - 4 * Settings.SquareSideLength - Settings.BorderSize;
        int height = 8 * Settings.SquareSideLength + 2 * Settings.BorderSize;

        int whiteHeight = (int) (height * shown);

        Raylib.DrawRectangle(x, y, Width, height, Theme.TimerColorDarkActive);
        if (Settings.FromWhitesView) Raylib.DrawRectangle(x, y + height - whiteHeight, Width, whiteHeight, Theme.TimerColorLightActive);
        else Raylib.DrawRectangle(x, y, Width, whiteHeight, Theme.TimerColorLightActive);

        // Marks the level point
        Raylib.DrawRectangle(x, y + height / 2 - 1, Width, 2, Theme.ButtonHoverColor);
    }

    // Maps the eval onto winning chances, so the bar moves a lot near equality and little once the game is decided
    private static float WhiteShare(SearchInfo info) {
        if (info.MateIn != 0) return info.MateIn > 0 ? 1f : 0f;

        double winningChances = 2 / (1 + Math.Exp(-0.00368208 * info.Eval)) - 1;
        return (float) (0.5 + 0.5 * winningChances);
    }
}
