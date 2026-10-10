namespace Chess.Core;

using Chess.UI;
using Chess.API;
using Chess.Bot;
using Chess.Utility;
using Raylib_cs;

class Game {
    private App.Player whitePlayer;
    private App.Player blackPlayer;

    private ChessEngine.Board chessBoard;

    private Timer whiteTimer;
    private Timer blackTimer;

    private Board board;
    private Coords coord;
    private Position position;
    private Player player;
    private Status gameStatus;
    private Menu menu;
    private BotInfo botInfo;
    private EvalBar evalBar;
    private MoveHistory moveHistory;

    private OpeningBook openingBook;

    private App.Player currentPlayer;

    private bool statusCheck = true;
    private bool gameOver = false;
    private string result = "*"; // For the saved game: "1-0", "0-1", "1/2-1/2", or "*" while in progress

    private Task BotTask;
    private Task AnimationTask;

    private CancellationTokenSource botTokenSource;
    private CancellationTokenSource animationTokenSource;

    public Game(App.Player whitePlayer, App.Player blackPlayer, string fen, bool fromWhitesView) {
        this.whitePlayer = whitePlayer;
        this.blackPlayer = blackPlayer;

        Settings.FromWhitesView = fromWhitesView;

        whiteTimer = new Timer(Settings.TimeLimit, true);
        blackTimer = new Timer(Settings.TimeLimit, false);

        chessBoard = new ChessEngine.Board(fen);

        if (chessBoard.IsWhiteTurn) blackTimer.Stop();
        else whiteTimer.Stop();

        board = new Board();
        coord = new Coords();
        position = new Position(chessBoard);
        player = new Player(whitePlayer.PlayerType, blackPlayer.PlayerType);
        gameStatus = Status.None;
        menu = new Menu();
        botInfo = new BotInfo();
        evalBar = new EvalBar();
        moveHistory = new MoveHistory();
        Bot.LastSearch = null;

        if (!File.Exists("Resources/Openings/Books.bin")) {
            OpeningBook.GenerateBinaryOpeningBook("Resources/Openings/Books.txt", "Resources/Openings/Books.bin");
        }
        openingBook = new OpeningBook();

        currentPlayer = chessBoard.IsWhiteTurn ? whitePlayer : blackPlayer;

        botTokenSource = new CancellationTokenSource();
        animationTokenSource = new CancellationTokenSource();

        BotTask = Task.CompletedTask;
        AnimationTask = Task.CompletedTask;
    }

    public void Update() {
        if (!statusCheck) goto Update;

        string status = Arbiter.Status(chessBoard, whiteTimer.Time, blackTimer.Time);
        if (status != "") {
            gameStatus = GameOverStatus(status);
            gameOver = true;

            goto Handle;
        }

    Update:

        currentPlayer = chessBoard.IsWhiteTurn ? whitePlayer : blackPlayer;
        if (currentPlayer.IsBot && statusCheck) {
            statusCheck = false;
            botTokenSource = new CancellationTokenSource();
            var token = botTokenSource.Token;
            BotTask = Task.Run(() => GetBotMove(token), token);
        }

    Handle:

        if (gameOver) {
            whiteTimer.Stop();
            blackTimer.Stop();
        }

        // While the menu is open, and on the click that closes it, the board ignores the mouse
        bool menuWasOpen = menu.IsOpen;
        MenuAction action = menu.Update();
        if (!menuWasOpen && !menu.IsOpen) {
            position.Update(chessBoard, board, highlightMoves: statusCheck && !gameOver, ref whiteTimer, ref blackTimer);
        }
        position.AnimatePromotion(chessBoard);

        whiteTimer.Update();
        blackTimer.Update();

        if (action != MenuAction.None) HandleMenuAction(action);
    }

    private void HandleMenuAction(MenuAction action) {
        switch (action) {
            case MenuAction.PlayAsWhite:
                StartNewGame(new App.HumanPlayer(true), new App.BotPlayer(false), fromWhitesView: true);
                break;
            case MenuAction.PlayAsBlack:
                StartNewGame(new App.BotPlayer(true), new App.HumanPlayer(false), fromWhitesView: false);
                break;
            case MenuAction.AiVsAi:
                StartNewGame(new App.BotPlayer(true), new App.BotPlayer(false), fromWhitesView: true);
                break;
            case MenuAction.SaveGame:
                menu.ShowMessage($"Saved to {SaveGame()}");
                break;
            case MenuAction.CopyFen:
                Raylib.SetClipboardText(chessBoard.Fen);
                menu.ShowMessage("FEN copied to the clipboard");
                break;
            case MenuAction.FlipBoard:
                Settings.FromWhitesView = !Settings.FromWhitesView;
                position.Flip(board);
                menu.Close();
                break;
            case MenuAction.Exit:
                Environment.Exit(0);
                break;
        }
    }

    // Writes the game as PGN to a new text file in the Games folder, and returns its path
    private string SaveGame() {
        Directory.CreateDirectory("Games");
        string path = Path.Combine("Games", $"game_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

        string Name(App.Player player) => player.IsBot ? $"Bot ({Bot.Level})" : "Human";
        File.WriteAllText(path, Pgn.Write(chessBoard, Name(whitePlayer), Name(blackPlayer), result));

        return path;
    }

    private Status GameOverStatus(string status) {
        switch (status) {
            case "Checkmate":
                // The side to move is the one that got mated
                result = chessBoard.IsWhiteTurn ? "0-1" : "1-0";
                return new Status("Checkmate", chessBoard.IsWhiteTurn ? "Black Wins" : "White Wins", Theme.CheckmateTextColor);
            case "Time Out":
                result = whiteTimer.Time == 0 ? "0-1" : "1-0";
                return new Status("Out of time", whiteTimer.Time == 0 ? "Black Wins" : "White Wins", Theme.CheckmateTextColor);
            case "Stalemate":
                result = "1/2-1/2";
                return new Status("Stalemate", "Draw", Theme.StalemateTextColor);
            default:
                result = "1/2-1/2";
                return new Status(Arbiter.DrawReason(chessBoard, whiteTimer.Time, blackTimer.Time), "Draw", Theme.DrawTextColor);
        }
    }

    private void GetBotMove(CancellationToken token) {
        try {
            if (token.IsCancellationRequested) return;

            Move move = openingBook.GetMove(chessBoard.MovesMade);
            if (move.IsNull) move = currentPlayer.Search(chessBoard, chessBoard.IsWhiteTurn ? whiteTimer.Time : blackTimer.Time, token);
            else Bot.LastSearch = new SearchInfo(move, 0, 0, 0, 0, 0, 0, DateTime.Now, FromBook: true);

            // A cancelled search returns whatever it had so far, which must not be played
            if (token.IsCancellationRequested) return;

            animationTokenSource = new CancellationTokenSource();
            var animToken = animationTokenSource.Token;
            AnimationTask = Task.Run(() => AnimateMove(move, animToken), animToken);
        } catch (OperationCanceledException) {
            // Canceled
        }
    }

    private void AnimateMove(Move move, CancellationToken token) {
        if (token.IsCancellationRequested) return;

        // The game ended (e.g. on time) while the bot was thinking
        if (Arbiter.Status(chessBoard, whiteTimer.Time, blackTimer.Time) != "") {
            statusCheck = true;
            return;
        }

        position.AnimateMove(move, chessBoard);
        if (token.IsCancellationRequested) return;

        chessBoard.MakeMove(move, record: true);
        board.SetLastMove(move);

        Thread.Sleep(100);
        if (token.IsCancellationRequested) return;

        if (chessBoard.IsWhiteTurn) {
            whiteTimer.Start();
            blackTimer.Stop();
        } else {
            blackTimer.Start();
            whiteTimer.Stop();
        }

        statusCheck = true;
    }

    // A task cancelled before it started throws on Wait, which is fine here
    private static void WaitIgnoringCancellation(Task task) {
        try {
            task.Wait();
        } catch (AggregateException) {
            // Ignore task cancellations
        }
    }

    private void StartNewGame(App.Player white, App.Player black, bool fromWhitesView) {
        // Stop the bot and any animation, and wait until they have finished:
        // the engine keeps static state, so an old search must not overlap the next game
        // The bot goes first, because it may start an animation right before it stops
        botTokenSource.Cancel();
        WaitIgnoringCancellation(BotTask);

        animationTokenSource.Cancel();
        WaitIgnoringCancellation(AnimationTask);

        whitePlayer = white;
        blackPlayer = black;
        Settings.FromWhitesView = fromWhitesView;
        menu.Close();

        // Reset game state
        whiteTimer = new Timer(Settings.TimeLimit, true);
        blackTimer = new Timer(Settings.TimeLimit, false);

        chessBoard = new ChessEngine.Board("");

        if (chessBoard.IsWhiteTurn) blackTimer.Stop();
        else whiteTimer.Stop();

        board = new Board();
        coord = new Coords();
        position = new Position(chessBoard);
        player = new Player(whitePlayer.PlayerType, blackPlayer.PlayerType);
        gameStatus = Status.None;
        botInfo = new BotInfo();
        evalBar = new EvalBar();
        moveHistory = new MoveHistory();
        Bot.LastSearch = null;

        openingBook = new OpeningBook();

        currentPlayer = chessBoard.IsWhiteTurn ? whitePlayer : blackPlayer;

        botTokenSource = new CancellationTokenSource();
        animationTokenSource = new CancellationTokenSource();

        statusCheck = true;
        gameOver = false;
        result = "*";
    }

    public void Render() {
        menu.Render();
        botInfo.Render();
        evalBar.Render();
        moveHistory.Render(chessBoard);
        board.Render();
        coord.Render();
        position.Render();
        player.Render();
        gameStatus.Render();
        whiteTimer.Render();
        blackTimer.Render();
        menu.RenderPopup();
    }
}
