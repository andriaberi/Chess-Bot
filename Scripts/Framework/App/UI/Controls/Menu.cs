namespace Chess.UI;

using Raylib_cs;
using System.Numerics;
using Chess.Utility;

class Menu {
    private List<Button> buttons = new();
    private int width = UIHelper.LeftColumnWidth;
    private const int height = 55;
    private const int margin = 20;
    private static readonly string[] buttonTexts = { "Play as White", "Play as Black", "AI vs AI", "Exit" };

    // Top edge of the topmost button
    public static int Top => UIHelper.BoardBottom - Settings.BorderSize - buttonTexts.Length * (height + margin) + margin;

    public Menu() {
        int posX = UIHelper.LeftColumnX;
        int posY = Settings.ScreenHeight / 2 + 4 * Settings.SquareSideLength - height;

        for (int i = buttonTexts.Length - 1; i >= 0; i--) {
            Button button = new(buttonTexts[i], posX, posY, width, height);
            buttons.Add(button);
            posY -= height + margin;
        }
    }

    public void Render() {
        foreach (var button in buttons) {
            button.Render(
                Theme.ButtonColor,
                Theme.ButtonHoverColor,
                Theme.ButtonTextColor,
                Theme.ButtonHoverTextColor
            );
        }
    }

    public int Update() {
        Vector2 mousePos = Raylib.GetMousePosition();
        int clicked = -1;

        for (int i = 0; i < buttons.Count; i++) {
            var button = buttons[i];
            button.UpdateHover(mousePos);

            if (button.WasClicked()) {
                clicked = buttons.Count - (i + 1);
            }
        }

        return clicked;
    }
}
