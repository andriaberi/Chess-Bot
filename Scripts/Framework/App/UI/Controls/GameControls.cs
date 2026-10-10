namespace Chess.UI;

using Chess.Utility;
using Raylib_cs;

class GameControls {
    // Hint and Resign at the bottom of the left column, level with the board's bottom edge
    private const int ButtonHeight = 55;
    private const double ConfirmSeconds = 3;

    public static int Top => UIHelper.BoardBottom - ButtonHeight;

    private readonly Button hint;
    private readonly Button resign;
    private double confirmUntil = -1; // Resigning takes a second click before this time

    public GameControls() {
        int width = (UIHelper.LeftColumnWidth - UIHelper.ColumnGap) / 2;
        hint = new Button("Hint", UIHelper.LeftColumnX, Top, width, ButtonHeight);
        resign = new Button("Resign", UIHelper.LeftColumnX + width + UIHelper.ColumnGap, Top, width, ButtonHeight);
    }

    private bool Confirming => Raylib.GetTime() < confirmUntil;

    public GameAction Update(bool canHint, bool canResign) {
        if (!canResign) confirmUntil = -1;

        hint.UpdateHover(Raylib.GetMousePosition());
        resign.UpdateHover(Raylib.GetMousePosition());

        if (canHint && hint.WasClicked()) return GameAction.Hint;

        if (canResign && resign.WasClicked()) {
            if (Confirming) {
                confirmUntil = -1;
                return GameAction.Resign;
            }
            confirmUntil = Raylib.GetTime() + ConfirmSeconds;
        }

        return GameAction.None;
    }

    // A hint being worked out, or a resignation waiting for its second click, shows like a selected menu choice
    public void Render(bool canHint, bool hintThinking, bool canResign) {
        resign.SetText(Confirming ? "Sure?" : "Resign");

        RenderButton(hint, canHint || hintThinking, hintThinking);
        RenderButton(resign, canResign, Confirming);
    }

    private static void RenderButton(Button button, bool enabled, bool selected) {
        if (!enabled) {
            button.Render(Theme.ButtonColor, Theme.ButtonColor, Theme.ButtonDisabledTextColor, Theme.ButtonDisabledTextColor);
        } else if (selected) {
            button.Render(Theme.ButtonHoverColor, Theme.ButtonHoverColor, Theme.ButtonHoverTextColor, Theme.ButtonHoverTextColor);
        } else {
            button.Render(Theme.ButtonColor, Theme.ButtonHoverColor, Theme.ButtonTextColor, Theme.ButtonHoverTextColor);
        }
    }
}
