namespace Chess.Core;

using Chess.UI;
using Chess.API;
using Chess.Bot;
using Chess.Utility;
using Raylib_cs;
using System.Numerics;

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
    private EvalGraph evalGraph;
    private Annotations annotations;
    private GameControls controls;

    private OpeningBook openingBook;

    private App.Player currentPlayer;

    private bool statusCheck = true;
    private bool gameOver = false;
    private string result = "*"; // For the saved game: "1-0", "0-1", "1/2-1/2", or "*" while in progress

    private Task BotTask;
    private Task AnimationTask;

    private CancellationTokenSource botTokenSource;
    private CancellationTokenSource animationTokenSource;

    // A hint is the bot's best move for the human, searched in the background and shown as an arrow until a move is made
    private Task HintTask = Task.CompletedTask;
    private CancellationTokenSource hintTokenSource = new();
    private volatile bool hintThinking = false;
    private Move hintMove = Move.NullMove;
    private int hintPly = -1; // Number of moves played when the hint was found

    private bool CanHint => !gameOver && statusCheck && currentPlayer.IsHuman && !hintThinking;
    private bool CanResign => !gameOver && (whitePlayer.IsHuman || blackPlayer.IsHuman);

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
        controls = new GameControls();
        botInfo = new BotInfo();
        evalBar = new EvalBar();
        moveHistory = new MoveHistory();
        evalGraph = new EvalGraph();
        annotations = new Annotations();
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
        // A hint searches on the board, flipping its side to move as it goes: checking the status or
        // whose turn it is meanwhile would generate moves alongside it, or start the bot searching too
        if (hintThinking) goto Handle;

        // Checking the status generates moves on the board, so it waits while the bot searches on it
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
        GameAction action = menu.Update();
        if (!menuWasOpen && !menu.IsOpen) {
            // The hint searches on the board, so the human can't move until it is done
            // The promotion picker uses the right button to cancel, so that click isn't also an annotation
            bool picking = position.PromotionPickerOpen;
            position.Update(chessBoard, board, highlightMoves: statusCheck && !gameOver && !hintThinking, ref whiteTimer, ref blackTimer);
            if (!picking && !position.PromotionPickerOpen) annotations.Update(chessBoard);
        }
        position.AnimatePromotion(chessBoard);

        whiteTimer.Update();
        blackTimer.Update();

        if (action == GameAction.None && !menu.IsOpen) action = controls.Update(CanHint, CanResign);
        if (action != GameAction.None) HandleGameAction(action);
    }

    private void HandleGameAction(GameAction action) {
        switch (action) {
            case GameAction.PlayAsWhite:
                StartNewGame(new App.HumanPlayer(true), new App.BotPlayer(false), fromWhitesView: true);
                break;
            case GameAction.PlayAsBlack:
                StartNewGame(new App.BotPlayer(true), new App.HumanPlayer(false), fromWhitesView: false);
                break;
            case GameAction.AiVsAi:
                StartNewGame(new App.BotPlayer(true), new App.BotPlayer(false), fromWhitesView: true);
                break;
            case GameAction.SaveGame:
                menu.ShowMessage($"Saved to {SaveGame()}");
                break;
            case GameAction.CopyFen:
                Raylib.SetClipboardText(chessBoard.Fen);
                menu.ShowMessage("FEN copied to the clipboard");
                break;
            case GameAction.FlipBoard:
                Settings.FromWhitesView = !Settings.FromWhitesView;
                position.Flip(board);
                menu.Close();
                break;
            case GameAction.Exit:
                Environment.Exit(0);
                break;
            case GameAction.Hint:
                StartHint();
                break;
            case GameAction.Resign:
                Resign();
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

            // The book follows games from the standard start, so it can't be used for a game set up from another position
            bool fromStart = chessBoard.StartFen == ChessEngine.FenUtility.StandardStartFen;
            Move move = fromStart ? openingBook.GetMove(chessBoard.MovesMade) : Move.NullMove;
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
        evalGraph.Record(chessBoard.MovesNotation.Count, Bot.LastSearch);

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

    // Stops the bot, any animation and any hint search, and waits until they have finished:
    // the engine keeps static state, so an old search must not overlap what comes next
    // The bot goes first, because it may start an animation right before it stops
    private void StopBackgroundWork() {
        botTokenSource.Cancel();
        WaitIgnoringCancellation(BotTask);

        animationTokenSource.Cancel();
        WaitIgnoringCancellation(AnimationTask);

        hintTokenSource.Cancel();
        WaitIgnoringCancellation(HintTask);
    }

    private void StartHint() {
        double timeLeft = chessBoard.IsWhiteTurn ? whiteTimer.Time : blackTimer.Time;
        int ply = chessBoard.MovesNotation.Count;

        hintThinking = true;
        hintTokenSource = new CancellationTokenSource();
        var token = hintTokenSource.Token;

        // Always at full strength, whatever difficulty the bot plays at
        HintTask = Task.Run(() => {
            try {
                Move move = Bot.Think(chessBoard, timeLeft, token, Difficulty.Hard);
                if (token.IsCancellationRequested) return;
                hintMove = move;
                hintPly = ply;
            } finally {
                hintThinking = false;
            }
        }); // Not given the token: a task cancelled before it starts would skip the finally and leave hintThinking set
    }

    // An arrow from the hinted piece to its square, until the next move is played
    private void RenderHint() {
        if (gameOver || hintMove.IsNull || hintPly != chessBoard.MovesNotation.Count) return;

        Color color = Theme.ButtonHoverColor;
        color.A = 190;
        UIHelper.DrawArrow(hintMove.Source, hintMove.Target, color);
    }

    // The human gives up; in a game against the bot that is always the human's side
    private void Resign() {
        StopBackgroundWork();

        // A bot move stopped partway through its animation may have left a piece off its square
        position.SetUpPosition(chessBoard);

        bool whiteResigns = whitePlayer.IsHuman;
        result = whiteResigns ? "0-1" : "1-0";
        gameStatus = new Status("Resignation", whiteResigns ? "Black Wins" : "White Wins", Theme.CheckmateTextColor);

        gameOver = true;
        statusCheck = false; // Keeps the bot from starting another search
    }

    private void StartNewGame(App.Player white, App.Player black, bool fromWhitesView) {
        StopBackgroundWork();

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
        evalGraph = new EvalGraph();
        annotations = new Annotations();
        Bot.LastSearch = null;

        openingBook = new OpeningBook();

        currentPlayer = chessBoard.IsWhiteTurn ? whitePlayer : blackPlayer;

        botTokenSource = new CancellationTokenSource();
        animationTokenSource = new CancellationTokenSource();

        statusCheck = true;
        gameOver = false;
        result = "*";
        hintMove = Move.NullMove;
        hintPly = -1;
    }

    public void Render() {
        menu.Render();
        botInfo.Render();
        evalBar.Render();
        moveHistory.Render(chessBoard);
        evalGraph.Render(chessBoard);
        controls.Render(CanHint, hintThinking, CanResign);
        board.Render();
        coord.Render();
        position.Render();
        annotations.Render();
        RenderHint();
        player.Render();
        gameStatus.Render();
        whiteTimer.Render();
        blackTimer.Render();
        menu.RenderPopup();
    }
}
