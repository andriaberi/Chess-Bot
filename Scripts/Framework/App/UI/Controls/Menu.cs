namespace Chess.UI;

using Chess.Bot;
using Chess.Utility;
using Raylib_cs;
using System.Numerics;

enum MenuAction { None, PlayAsWhite, PlayAsBlack, AiVsAi, SaveGame, CopyFen, FlipBoard, Exit }

class Menu {
    // A "Menu" button at the top of the left column, opening a popup over the board with the game controls
    // Difficulty and clock are applied here; everything else is returned to the game as an action
    private const int ButtonHeight = 55;
    private const int PanelWidth = 640;
    private const int Padding = 28;
    private const int Gap = 12;
    private const int SectionGap = 24;
    private const int CaptionFontSize = 28;
    private const int ButtonFontSize = 32;
    private const double MessageSeconds = 4;

    public static int Bottom => UIHelper.BoardTop + ButtonHeight;

    public bool IsOpen { get; private set; }

    private readonly Button menuButton;
    private readonly List<(string Text, int Y)> captions = new();
    private readonly List<Entry> entries = new();
    private readonly Rectangle panel;

    private string message = "";
    private double messageUntil;
    private readonly int messageY;

    private static Font captionFont = UIHelper.LoadFont(CaptionFontSize);

    // A button in the popup; IsSelected marks the current choice of a setting, OnClick returns the action to take
    private record Entry(Button Button, Func<bool> IsSelected, Func<MenuAction> OnClick);

    private static readonly (string Label, double Seconds)[] clocks = {
        ("3 min", 3 * 60), ("5 min", 5 * 60), ("10 min", 10 * 60), ("Off", double.PositiveInfinity)
    };

    public Menu() {
        menuButton = new Button("Menu", UIHelper.LeftColumnX, UIHelper.BoardTop, UIHelper.LeftColumnWidth, ButtonHeight);

        // Laid out top to bottom first, then the panel is centered on the board around it
        int contentWidth = PanelWidth - Padding * 2;
        int y = Padding;
        var rows = new List<(int Y, (string Label, Func<bool> IsSelected, Func<MenuAction> OnClick)[] Items)>();

        void Section(string caption, params (string, Func<bool>, Func<MenuAction>)[] items) {
            captions.Add((caption, y));
            y += CaptionFontSize + 8;
            rows.Add((y, items));
            y += ButtonHeight + SectionGap;
        }

        Func<bool> never = () => false;

        Section("New game",
            ("Play White", never, () => MenuAction.PlayAsWhite),
            ("Play Black", never, () => MenuAction.PlayAsBlack),
            ("AI vs AI", never, () => MenuAction.AiVsAi));

        Section("Bot difficulty",
            ("Easy", () => Bot.Level == Difficulty.Easy, () => SetLevel(Difficulty.Easy)),
            ("Medium", () => Bot.Level == Difficulty.Medium, () => SetLevel(Difficulty.Medium)),
            ("Hard", () => Bot.Level == Difficulty.Hard, () => SetLevel(Difficulty.Hard)));

        Section("Clock (from the next game)", clocks.Select(clock => (
            clock.Label,
            (Func<bool>) (() => Settings.TimeLimit == clock.Seconds),
            (Func<MenuAction>) (() => SetClock(clock.Seconds)))).ToArray());

        Section("Game",
            ("Save game", never, () => MenuAction.SaveGame),
            ("Copy FEN", never, () => MenuAction.CopyFen),
            ("Flip board", never, () => MenuAction.FlipBoard));

        // A line for feedback such as "Saved to ...", above the last row
        messageY = y;
        y += CaptionFontSize + SectionGap;

        rows.Add((y, new (string, Func<bool>, Func<MenuAction>)[] {
            ("Resume", never, () => { IsOpen = false; return MenuAction.None; }),
            ("Exit", never, () => MenuAction.Exit)
        }));
        y += ButtonHeight + Padding;

        int panelX = Settings.BoardMarginLeft + 4 * Settings.SquareSideLength - PanelWidth / 2;
        int panelY = Settings.ScreenHeight / 2 - y / 2;
        panel = new Rectangle(panelX, panelY, PanelWidth, y);

        for (int i = 0; i < captions.Count; i++) captions[i] = (captions[i].Text, captions[i].Y + panelY);
        messageY += panelY;

        // Buttons in a row share its width equally
        foreach (var (rowY, items) in rows) {
            int width = (contentWidth - Gap * (items.Length - 1)) / items.Length;
            for (int i = 0; i < items.Length; i++) {
                var (label, isSelected, onClick) = items[i];
                Button button = new Button(label, panelX + Padding + i * (width + Gap), panelY + rowY, width, ButtonHeight, ButtonFontSize);
                entries.Add(new Entry(button, isSelected, onClick));
            }
        }
    }

    private static MenuAction SetLevel(Difficulty level) {
        Bot.Level = level;
        return MenuAction.None;
    }

    private static MenuAction SetClock(double seconds) {
        Settings.TimeLimit = seconds;
        return MenuAction.None;
    }

    public void ShowMessage(string text) {
        message = text;
        messageUntil = Raylib.GetTime() + MessageSeconds;
    }

    public void Close() => IsOpen = false;

    public MenuAction Update() {
        Vector2 mouse = Raylib.GetMousePosition();

        menuButton.UpdateHover(mouse);
        if (menuButton.WasClicked()) {
            IsOpen = !IsOpen;
            return MenuAction.None;
        }

        if (!IsOpen) return MenuAction.None;

        foreach (Entry entry in entries) {
            entry.Button.UpdateHover(mouse);
            if (entry.Button.WasClicked()) return entry.OnClick();
        }

        // A click outside the popup closes it
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && !Raylib.CheckCollisionPointRec(mouse, panel)) IsOpen = false;

        return MenuAction.None;
    }

    public void Render() {
        Color baseColor = IsOpen ? Theme.ButtonHoverColor : Theme.ButtonColor;
        Color textColor = IsOpen ? Theme.ButtonHoverTextColor : Theme.ButtonTextColor;
        menuButton.Render(baseColor, Theme.ButtonHoverColor, textColor, Theme.ButtonHoverTextColor);
    }

    // Drawn after everything else, so it covers the board
    public void RenderPopup() {
        if (!IsOpen) return;

        Raylib.DrawRectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight, Theme.PromotionOverlayCol);
        Raylib.DrawRectangleRec(panel, Theme.BackgroundColor);

        foreach (var (text, y) in captions) {
            Raylib.DrawTextEx(captionFont, text, new Vector2(panel.X + Padding, y), CaptionFontSize, 1, Theme.ButtonTextColor);
        }

        // The current difficulty and clock stay marked like a hovered button
        foreach (Entry entry in entries) {
            bool selected = entry.IsSelected();
            entry.Button.Render(
                selected ? Theme.ButtonHoverColor : Theme.ButtonColor,
                Theme.ButtonHoverColor,
                selected ? Theme.ButtonHoverTextColor : Theme.ButtonTextColor,
                Theme.ButtonHoverTextColor
            );
        }

        if (message != "" && Raylib.GetTime() < messageUntil) {
            UIHelper.DrawTextCentered(captionFont, message, panel.X + panel.Width / 2, messageY, CaptionFontSize, Theme.ButtonHoverTextColor);
        }
    }
}
