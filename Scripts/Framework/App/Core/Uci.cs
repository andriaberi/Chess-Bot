namespace Chess.Core;

using Chess.API;
using Chess.Bot;
using Chess.ChessEngine;
using System.Reflection;

class Uci {
    // Universal Chess Interface: lets chess GUIs and tournament runners (Cute Chess, fastchess, lichess-bot) use the bot
    // Started with --uci, it reads commands from standard input and answers on standard output, without opening a window
    // The protocol: https://backscattering.de/chess/uci/
    private readonly TextReader input;
    private readonly TextWriter output;
    private readonly object outputLock = new();

    private Board board = new Board("");
    private Difficulty level = Difficulty.Hard;
    private bool ownBook = false; // Off by default: tournament runners hand out their own openings so games vary
    private OpeningBook? openingBook;

    private Task search = Task.CompletedTask;
    private CancellationTokenSource stopSearch = new();

    public Uci(TextReader input, TextWriter output) {
        this.input = input;
        this.output = output;
    }

    // Name and version, e.g. "Chess-Bot 1.2", from the version the build was stamped with
    public static string EngineName {
        get {
            string? version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            return string.IsNullOrEmpty(version) ? "Chess-Bot" : $"Chess-Bot {version}";
        }
    }

    // Handles commands until "quit" or the end of the input; a search still running at the end of the input is finished first
    public void Run() {
        string? line;
        while ((line = input.ReadLine()) != null) {
            if (!Handle(line.Trim())) return;
        }
        search.Wait();
    }

    // Returns false on "quit"
    private bool Handle(string line) {
        string[] words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return true;

        switch (words[0]) {
            case "uci":
                Send($"id name {EngineName}");
                Send("id author Andria Beridze");
                Send("option name Difficulty type combo default Hard var Easy var Medium var Hard");
                Send("option name OwnBook type check default false");
                Send("uciok");
                break;
            case "isready":
                Send("readyok");
                break;
            case "setoption":
                SetOption(words);
                break;
            case "ucinewgame":
                StopSearch();
                board = new Board("");
                break;
            case "position":
                StopSearch();
                SetPosition(words);
                break;
            case "go":
                StopSearch();
                Go(words);
                break;
            case "stop":
                StopSearch();
                break;
            case "quit":
                StopSearch();
                return false;
        }
        return true;
    }

    // setoption name <name> value <value>
    private void SetOption(string[] words) {
        int nameAt = Array.IndexOf(words, "name");
        int valueAt = Array.IndexOf(words, "value");
        if (nameAt == -1 || valueAt == -1 || valueAt < nameAt) return;

        string name = string.Join(' ', words[(nameAt + 1)..valueAt]).ToLower();
        string value = string.Join(' ', words[(valueAt + 1)..]);

        if (name == "difficulty" && Enum.TryParse(value, ignoreCase: true, out Difficulty parsed)) level = parsed;
        if (name == "ownbook") ownBook = value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    // position startpos [moves ...] | position fen <fen> [moves ...]
    private void SetPosition(string[] words) {
        int movesAt = Array.IndexOf(words, "moves");
        int fenEnd = movesAt == -1 ? words.Length : movesAt;

        string fen = words.Length > 1 && words[1] == "fen" ? string.Join(' ', words[2..fenEnd]) : "";
        board = new Board(fen);

        if (movesAt == -1) return;
        foreach (string uci in words[(movesAt + 1)..]) {
            Move move = Notation.FromUci(board, uci);
            if (move.IsNull) break; // An illegal move leaves the position as it was before it
            board.MakeMove(move, record: true);
        }
    }

    // go [wtime <ms>] [btime <ms>] [winc <ms>] [binc <ms>] [movetime <ms>] [depth <n>] [infinite]
    private void Go(string[] words) {
        double Seconds(string name, double fallback) {
            int at = Array.IndexOf(words, name);
            return at != -1 && at + 1 < words.Length && double.TryParse(words[at + 1], out double ms) ? ms / 1000 : fallback;
        }

        bool white = board.IsWhiteTurn;
        double timeLeft = Seconds(white ? "wtime" : "btime", double.PositiveInfinity);
        double increment = Seconds(white ? "winc" : "binc", 0);
        double? moveTime = Seconds("movetime", -1) is double fixedTime and >= 0 ? fixedTime : null;

        int depthAt = Array.IndexOf(words, "depth");
        int? depth = depthAt != -1 && depthAt + 1 < words.Length && int.TryParse(words[depthAt + 1], out int d) ? d : null;

        // Without a clock or a fixed time, the search runs until it is stopped or reaches the given depth
        bool noClock = !words.Contains("wtime") && !words.Contains("btime");
        double? thinkTime = moveTime ?? (words.Contains("infinite") || (noClock && depth != null) ? double.PositiveInfinity : null);

        stopSearch = new CancellationTokenSource();
        CancellationToken token = stopSearch.Token;
        Board position = board;

        search = Task.Run(() => {
            Bot.IterationCompleted = info => Send(InfoLine(info, position.IsWhiteTurn));

            Move move = ownBook && position.StartFen == FenUtility.StandardStartFen ? Book().GetMove(position.MovesMade) : Move.NullMove;
            if (move.IsNull) move = Bot.Think(position, timeLeft, token, level, increment, thinkTime, depth);

            // Stopped before the first depth finished: any legal move beats answering with none
            if (move.IsNull) move = MoveGenerator.GenerateMoves(position).FirstOrDefault(Move.NullMove);
            Bot.IterationCompleted = null;
            Send($"bestmove {Notation.ToUci(move)}");
        });
    }

    // e.g. "info depth 7 score cp 25 nodes 1500000 nps 1800000 time 830 pv g1f3 b8c6"
    // UCI scores are from the side to move, the bot's from white; mate is given in moves, negative when getting mated
    private static string InfoLine(SearchInfo info, bool whiteToMove) {
        int sign = whiteToMove ? 1 : -1;
        string score = info.MateIn != 0 ? $"mate {info.MateIn * sign}" : $"cp {info.Eval * sign}";

        long nps = info.Seconds > 0 ? (long) (info.Nodes / info.Seconds) : 0;
        string line = string.Join(' ', (info.Line ?? new List<Move>()).Select(Notation.ToUci));

        return $"info depth {info.Depth} score {score} nodes {info.Nodes} nps {nps} time {(long) (info.Seconds * 1000)} pv {line}".TrimEnd();
    }

    private OpeningBook Book() {
        if (openingBook != null) return openingBook;
        if (!File.Exists("Resources/Openings/Books.bin")) {
            OpeningBook.GenerateBinaryOpeningBook("Resources/Openings/Books.txt", "Resources/Openings/Books.bin");
        }
        return openingBook = new OpeningBook();
    }

    // Stops a running search and waits for it to report its move, so searches never overlap on the engine's shared state
    private void StopSearch() {
        stopSearch.Cancel();
        search.Wait();
    }

    private void Send(string line) {
        lock (outputLock) {
            output.WriteLine(line);
            output.Flush();
        }
    }
}
