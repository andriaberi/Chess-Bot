namespace Chess.App;

using Chess.API;
using Chess.ChessEngine;

class HumanPlayer : Player {
    public HumanPlayer(bool isWhite) : base(isWhite, true) { }

    public override Move Search(Board board, double TimeRemaining, double increment, CancellationToken cancel) {
        return Move.NullMove;
    }
}