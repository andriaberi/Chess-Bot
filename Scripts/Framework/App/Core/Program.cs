namespace Chess.Core;

using Chess.App;
using Chess.Utility;
using Raylib_cs;
using System.Numerics;

class Program {
    static void Main(string[] args) {
        Raylib.SetTraceLogLevel(TraceLogLevel.None);
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        // Start small: some window managers (e.g. GNOME) auto-maximize a window that opens near screen size
        Raylib.InitWindow(Settings.ScreenWidth / 2, Settings.ScreenHeight / 2, "Chess by Andria Beridze");
        Raylib.SetWindowMinSize(Settings.ScreenWidth / 4, Settings.ScreenHeight / 4);
        FitWindowToMonitor();
        Raylib.SetTargetFPS(90);
        Raylib.InitAudioDevice();

        // The game is laid out for a fixed ScreenWidth x ScreenHeight canvas,
        // which is drawn off-screen and then scaled to fit the window
        RenderTexture2D canvas = Raylib.LoadRenderTexture(Settings.ScreenWidth, Settings.ScreenHeight);
        Raylib.SetTextureFilter(canvas.Texture, TextureFilter.Bilinear);

        // Default game: Human vs Bot | Initial position | Board visible from white's view
        Game game = new Game(new HumanPlayer(true), new BotPlayer(false), "", true);

        while (!Raylib.WindowShouldClose()) {
            Rectangle area = CanvasArea();
            float scale = area.Width / Settings.ScreenWidth;

            // Map window mouse coordinates back onto the canvas, so the UI code keeps working unchanged
            Raylib.SetMouseOffset(-(int) area.X, -(int) area.Y);
            Raylib.SetMouseScale(1 / scale, 1 / scale);

            Raylib.BeginTextureMode(canvas);
            Raylib.ClearBackground(Theme.BackgroundColor);

            game.Update();
            game.Render();

            Raylib.EndTextureMode();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Theme.BackgroundColor);

            // Render textures are stored upside down, hence the negative source height
            Rectangle source = new Rectangle(0, 0, Settings.ScreenWidth, -Settings.ScreenHeight);
            Raylib.DrawTexturePro(canvas.Texture, source, area, Vector2.Zero, 0, Color.White);

            Raylib.EndDrawing();
        }

        Raylib.UnloadRenderTexture(canvas);
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }

    // Largest rectangle with the canvas's aspect ratio that fits in the window, centered
    static Rectangle CanvasArea() {
        float windowWidth = Raylib.GetScreenWidth();
        float windowHeight = Raylib.GetScreenHeight();
        float scale = Math.Min(windowWidth / Settings.ScreenWidth, windowHeight / Settings.ScreenHeight);

        float width = Settings.ScreenWidth * scale;
        float height = Settings.ScreenHeight * scale;
        return new Rectangle((windowWidth - width) / 2, (windowHeight - height) / 2, width, height);
    }

    // Sizes the window to at most 75% of the monitor, keeping the canvas's aspect ratio
    static void FitWindowToMonitor() {
        int monitor = Raylib.GetCurrentMonitor();
        int monitorWidth = Raylib.GetMonitorWidth(monitor);
        int monitorHeight = Raylib.GetMonitorHeight(monitor);
        if (monitorWidth <= 0 || monitorHeight <= 0) return;

        float scale = Math.Min(1f, 0.75f * Math.Min((float) monitorWidth / Settings.ScreenWidth, (float) monitorHeight / Settings.ScreenHeight));
        int width = (int) (Settings.ScreenWidth * scale);
        int height = (int) (Settings.ScreenHeight * scale);

        Vector2 monitorPosition = Raylib.GetMonitorPosition(monitor);
        Raylib.SetWindowSize(width, height);
        Raylib.SetWindowPosition((int) monitorPosition.X + (monitorWidth - width) / 2, (int) monitorPosition.Y + (monitorHeight - height) / 2);
    }
}
