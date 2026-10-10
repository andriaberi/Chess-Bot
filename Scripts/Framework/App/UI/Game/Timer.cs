namespace Chess.UI;

using Chess.Utility;
using Raylib_cs;
using System.Numerics;

class Timer {
    private double time;
    private bool isRunning = true;
    private DateTime prevUpdate = DateTime.Now;

    private readonly bool isWhite;
    private readonly Font font = UIHelper.LoadFont(52);

    private const int FontSize = 52;
    private const int RectWidth = 200;
    private const int RectHeight = 48;

    public double Time => time;

    public Timer(double initialTime, bool isWhite) {
        time = initialTime;
        this.isWhite = isWhite;
    }

    public void Start() => isRunning = true;
    public void Stop() => isRunning = false;

    public void Update() {
        if (!isRunning) {
            prevUpdate = DateTime.Now;
            return;
        }

        time -= (DateTime.Now - prevUpdate).TotalSeconds;
        if (time <= 0) {
            time = 0;
            isRunning = false;
            SoundManager.Play("Game-Over");
        }

        prevUpdate = DateTime.Now;
    }

    public void Render() {
        // The side whose pieces start at the top of the board has its clock above it
        bool isOnTop = isWhite != Settings.FromWhitesView;
        bool active = isRunning;

        Color rectColor = GetColor(active, isWhite, true);
        Color textColor = GetColor(active, isWhite, false);

        int x = Settings.BoardMarginLeft + 8 * Settings.SquareSideLength - RectWidth;
        int y = (isOnTop ? UIHelper.TopBarCenterY : UIHelper.BottomBarCenterY) - RectHeight / 2;

        Raylib.DrawRectangle(x, y, RectWidth, RectHeight, rectColor);

        string text = FormatTime(time);
        Vector2 size = Raylib.MeasureTextEx(font, text, FontSize, 1);
        Vector2 pos = new Vector2(x + RectWidth / 2 - size.X / 2, y + RectHeight / 2 - size.Y / 2 + 2);

        Raylib.DrawTextEx(font, text, pos, FontSize, 1, textColor);
    }

    private static string FormatTime(double t) {
        if (double.IsPositiveInfinity(t)) return "--:--";

        int d = (int) (t * 10);
        return d < 600
            ? $"{d / 10:D2}.{d % 10}"
            : $"{d / 600:D2}:{(d / 10) % 60:D2}";
    }

    private static Color GetColor(bool active, bool white, bool isRect) {
        if (active) {
            return white
                ? (isRect ? Theme.TimerColorLightActive : Theme.TimerColorDarkActive)
                : (isRect ? Theme.TimerColorDarkActive : Theme.TimerColorLightActive);
        } else {
            return white
                ? (isRect ? Theme.TimerColorLightDisabled : Theme.TimerColorDarkDisabled)
                : (isRect ? Theme.TimerColorDarkDisabled : Theme.TimerColorLightDisabled);
        }
    }
}
