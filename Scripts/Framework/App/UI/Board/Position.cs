namespace Chess.UI;

using Chess.API;
using Chess.Core;
using Chess.Utility;
using System.Numerics;
using Raylib_cs;

class Position {
    private List<Piece> pieces;
    private int draggedPiece = -1; // To keep track of the piece during dragging
    private int animatedPiece = -1; // To keep track of the piece during animation
    private int selectedSquare = -1; // Square of the piece picked by click or drag, whose moves are highlighted
    private bool wasSelected = false; // Whether the pressed piece was already selected, so releasing on it deselects it

    private int promotionLastChecked = -1;

    // A human promotion waits here until a piece is picked; -1 when no picker is open
    private int promotionSource = -1;
    private int promotionTarget = -1;
    private List<Piece> promotionChoices = new List<Piece>();
    private static readonly int[] promotionTypes = { API.Piece.Queen, API.Piece.Knight, API.Piece.Rook, API.Piece.Bishop };

    public Position(ChessEngine.Board board) {
        pieces = new List<Piece>();
        SetUpPosition(board);
    }

    // Creating new pieces to render them on the screen
    public void SetUpPosition(ChessEngine.Board board) {
        pieces = new List<Piece>();
        for (int i = 0; i < 64; i++) {
            if (!board.Square[i].IsNone) {
                pieces.Add(new Piece(board.Square[i], new Coord(i)));
            }
        }
    }

    public void Update(ChessEngine.Board board, Board boardUI, bool highlightMoves, ref Timer whiteTimerUI, ref Timer blackTimerUI) {
        if (promotionTarget != -1) {
            UpdatePromotionPicker(board, boardUI, highlightMoves, ref whiteTimerUI, ref blackTimerUI);
            return;
        }

        // Nothing can be selected while it isn't a human's turn
        if (!highlightMoves && selectedSquare != -1) Deselect(boardUI);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
            int square = SquareUnderMouse();

            // With a piece selected, clicking one of its highlighted squares plays the move
            if (selectedSquare != -1 && square != -1 && boardUI.IsValidToMove(square)) {
                int source = selectedSquare;
                selectedSquare = -1;
                PlayMove(source, square, board, boardUI, ref whiteTimerUI, ref blackTimerUI);
                return;
            }

            wasSelected = square != -1 && square == selectedSquare;
            Deselect(boardUI);

            int index = pieces.FindIndex(p => p.Coord.SquareIndex == square);
            if (index != -1) {
                draggedPiece = index; // Drag the piece

                // If the piece is of the current player, select it and highlight its valid moves
                if (board.Square[square].IsWhite == board.IsWhiteTurn && highlightMoves) {
                    selectedSquare = square;
                    boardUI.HighlightValidMoves(ChessEngine.MoveGenerator.GenerateMoves(board, square));
                    boardUI.HighlightSquare(square);
                }
            }
        }

        // Center the piece on the mouse cursor
        if (draggedPiece != -1) {
            pieces[draggedPiece].X = Raylib.GetMouseX() - Settings.SquareSideLength / 2;
            pieces[draggedPiece].Y = Raylib.GetMouseY() - Settings.SquareSideLength / 2;
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Left) && draggedPiece != -1) {
            int source = pieces[draggedPiece].Coord.SquareIndex;
            int square = SquareUnderMouse();

            pieces[draggedPiece].ResetPosition();
            draggedPiece = -1;

            if (square == source) {
                // A click rather than a drag: the piece stays selected, and clicking it again deselects it
                if (wasSelected) Deselect(boardUI);
            } else if (square != -1 && boardUI.IsValidToMove(square)) {
                selectedSquare = -1;
                PlayMove(source, square, board, boardUI, ref whiteTimerUI, ref blackTimerUI);
            } else {
                SoundManager.Play("Illegal");
                Deselect(boardUI);
            }
        }
    }

    // Moves every piece to its square after the board was turned around
    public void Flip(Board boardUI) {
        foreach (Piece piece in pieces) piece.ResetPosition();
        foreach (Piece choice in promotionChoices) choice.ResetPosition();
        Deselect(boardUI);
    }

    private void Deselect(Board boardUI) {
        selectedSquare = -1;
        boardUI.Clear();
    }

    // Index of the square under the mouse, or -1 when the mouse is off the board
    private static int SquareUnderMouse() {
        Vector2 mouse = Raylib.GetMousePosition();
        for (int i = 0; i < 64; i++) {
            Rectangle rect = new Rectangle(UIHelper.GetScreenX(i % 8), UIHelper.GetScreenY(i / 8), Settings.SquareSideLength, Settings.SquareSideLength);
            if (Raylib.CheckCollisionPointRec(mouse, rect)) return i;
        }
        return -1;
    }

    // Plays the human's move, whether dragged or clicked; the target must be highlighted as legal
    private void PlayMove(int source, int target, ChessEngine.Board board, Board boardUI, ref Timer whiteTimerUI, ref Timer blackTimerUI) {
        // Promotion waits for the player to pick a piece, the pawn stays put until then
        if (board.Square[source].IsPawn && (target < 8 || target > 55)) {
            OpenPromotionPicker(source, target, board.IsWhiteTurn);
            boardUI.Clear();
            return;
        }

        int index = pieces.FindIndex(p => p.Coord.SquareIndex == source);

        // If other piece was killed, remove it from the list
        int capturedIndex = pieces.FindIndex(p => p.Coord.SquareIndex == target);
        if (capturedIndex != -1) {
            pieces.RemoveAt(capturedIndex);
            if (index > capturedIndex) index--;
        }

        // Update and record the data
        Move move = new Move(new Coord(source), new Coord(target));
        boardUI.SetLastMove(move);

        pieces[index].Coord = new Coord(target);
        pieces[index].ResetPosition();

        // Castle
        if (board.Square[move.Source].IsKing && Math.Abs(move.Source - move.Target) == 2) {
            move = new Move(move.Source, move.Target, Move.Castling);
            int rookSource = move.Target + (move.Target == 62 || move.Target == 6 ? 1 : -2);
            int rookTarget = move.Target + (move.Target == 62 || move.Target == 6 ? -1 : 1);

            int rookIndex = pieces.FindIndex(p => p.Coord.SquareIndex == rookSource);
            pieces[rookIndex].Coord = new Coord(rookTarget);
            pieces[rookIndex].ResetPosition();
        }
        // En passant
        if (board.Square[move.Source].IsPawn && Math.Abs(move.Source - move.Target) % 8 != 0 && board.Square[move.Target].IsNone) {
            move = new Move(move.Source, move.Target, Move.EnPassant);

            int capturedPawn = board.IsWhiteTurn ? move.Target - 8 : move.Target + 8;
            pieces.RemoveAt(pieces.FindIndex(p => p.Coord.SquareIndex == capturedPawn));
        }

        PlaySound(move, board);
        board.MakeMove(move, record: true);
        SwitchTimers(board, ref whiteTimerUI, ref blackTimerUI);
    }

    private static void SwitchTimers(ChessEngine.Board board, ref Timer whiteTimerUI, ref Timer blackTimerUI) {
        if (board.IsWhiteTurn) {
            whiteTimerUI.Start();
            blackTimerUI.Stop();
        } else {
            blackTimerUI.Start();
            whiteTimerUI.Stop();
        }
    }

    // The choices stack from the promotion square towards the middle of the board, queen first
    private void OpenPromotionPicker(int source, int target, bool isWhite) {
        promotionSource = source;
        promotionTarget = target;
        promotionChoices = new List<Piece>();

        int direction = isWhite ? -8 : 8;
        for (int i = 0; i < promotionTypes.Length; i++) {
            API.Piece piece = new API.Piece(promotionTypes[i], isWhite ? API.Piece.White : API.Piece.Black);
            promotionChoices.Add(new Piece(piece, new Coord(target + i * direction)));
        }
    }

    private void ClosePromotionPicker() {
        promotionSource = -1;
        promotionTarget = -1;
        promotionChoices = new List<Piece>();
    }

    // Left click on a choice promotes, a click anywhere else or a right click cancels the move
    private void UpdatePromotionPicker(ChessEngine.Board board, Board boardUI, bool canMove, ref Timer whiteTimerUI, ref Timer blackTimerUI) {
        // The game ended (e.g. on time) while the picker was open
        if (!canMove || Raylib.IsMouseButtonPressed(MouseButton.Right)) {
            ClosePromotionPicker();
            return;
        }

        if (!Raylib.IsMouseButtonPressed(MouseButton.Left)) return;

        int choice = promotionChoices.FindIndex(piece => Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), new Rectangle(piece.X, piece.Y, Settings.SquareSideLength, Settings.SquareSideLength)));
        if (choice == -1) {
            ClosePromotionPicker();
            return;
        }

        ushort flag = promotionTypes[choice] switch {
            API.Piece.Queen => Move.QueenPromotion,
            API.Piece.Knight => Move.KnightPromotion,
            API.Piece.Rook => Move.RookPromotion,
            _ => Move.BishopPromotion
        };
        Move move = new Move(promotionSource, promotionTarget, flag);

        // Remove the captured piece and swap the pawn for the picked piece
        int capturedIndex = pieces.FindIndex(p => p.Coord.SquareIndex == move.Target);
        if (capturedIndex != -1) pieces.RemoveAt(capturedIndex);

        int pawnIndex = pieces.FindIndex(p => p.Coord.SquareIndex == move.Source);
        pieces[pawnIndex] = new Piece(new API.Piece(promotionTypes[choice], board.IsWhiteTurn ? API.Piece.White : API.Piece.Black), new Coord(move.Target));

        ClosePromotionPicker();
        boardUI.SetLastMove(move);

        PlaySound(move, board);
        board.MakeMove(move, record: true);
        SwitchTimers(board, ref whiteTimerUI, ref blackTimerUI);
    }

    public void AnimateMove(Move move, ChessEngine.Board board) {
        // If any piece is dragged, reset its position
        if (draggedPiece != -1) pieces[draggedPiece].ResetPosition();
        draggedPiece = -1;

        int index = pieces.FindIndex(p => p.Coord.SquareIndex == move.Source);

        // Remove killed piece
        if (pieces.FindIndex(p => p.Coord.SquareIndex == move.Target) != -1) {
            int targetIndex = pieces.FindIndex(p => p.Coord.SquareIndex == move.Target);
            pieces.RemoveAt(targetIndex);
            if (index > targetIndex) index--;
        }

        Animate(index, move);

        // Just because floating point numbers are not precise, reset the position of the piece to ensure it is pixel perfect
        pieces[index].Coord = new Coord(move.Target);
        pieces[index].ResetPosition();

        // Castle
        // Move the rook to the other side of the king
        if (move.Flag == Move.Castling) {
            int rookSource = move.Target + (move.Target == 62 || move.Target == 6 ? 1 : -2);
            int rookTarget = move.Target + (move.Target == 62 || move.Target == 6 ? -1 : 1);

            int rookIndex = pieces.FindIndex(p => p.Coord.SquareIndex == rookSource);

            pieces[rookIndex].Coord = new Coord(rookTarget);
            pieces[rookIndex].ResetPosition();
        }

        // En passant
        // Remove the piece that was killed
        if (move.Flag == Move.EnPassant) {
            int target = board.IsWhiteTurn ? move.Target - 8 : move.Target + 8;
            index = pieces.FindIndex(p => p.Coord.SquareIndex == target);
            pieces.RemoveAt(index);
        }

        PlaySound(move, board);
    }

    public void Animate(int index, Move move) {
        int frames = 18;
        double startX = UIHelper.GetScreenX(ChessEngine.BoardHelper.ColumnIndex(move.Source));
        double startY = UIHelper.GetScreenY(ChessEngine.BoardHelper.RowIndex(move.Source));
        double endX = UIHelper.GetScreenX(ChessEngine.BoardHelper.ColumnIndex(move.Target));
        double endY = UIHelper.GetScreenY(ChessEngine.BoardHelper.RowIndex(move.Target));
        double dx = (endX - startX) / frames;
        double dy = (endY - startY) / frames;

        animatedPiece = index;
        // Move piece by a small amount each frame, and wait for a short time
        // Creates an animation effect
        for (int i = 0; i < frames; i++) {
            pieces[index].X = (int) (startX + dx * i);
            pieces[index].Y = (int) (startY + dy * i);

            Thread.Sleep(10);
        }
        animatedPiece = -1;
    }

    private void PlaySound(Move move, ChessEngine.Board board) {
        bool soundPlayed = false;

        board.MakeMove(move);
        if (Arbiter.Status(board) != "") { soundPlayed = true; SoundManager.Play("Game-Over"); } else if (ChessEngine.MoveHelper.IsInCheck(board, true) || ChessEngine.MoveHelper.IsInCheck(board, false)) { soundPlayed = true; SoundManager.Play("Check"); }
        board.UnmakeMove(move);

        if (soundPlayed) return;

        if (board.Square[move.Target].Type != API.Piece.None) SoundManager.Play("Capture");
        else if (move.IsPromotion) SoundManager.Play("Promotion");
        else if (move.IsCastling) SoundManager.Play("Castle");
        else SoundManager.Play("Move");
    }

    // Special case that needs to be handled separately due to the bug before
    public void AnimatePromotion(ChessEngine.Board board) {
        if (board.MovesMade.Count == 0) return;
        if (!board.MovesMade[board.MovesMade.Count - 1].IsPromotion) return;
        if (promotionLastChecked == board.MovesMade.Count) return;

        Move move = board.MovesMade[board.MovesMade.Count - 1];
        int color = board.IsWhiteTurn ? API.Piece.Black : API.Piece.White;
        int index = pieces.FindIndex(piece => piece.Coord == new Coord(move.Target));

        if (index == -1) return;
        promotionLastChecked = board.MovesMade.Count;

        pieces[index] = new Piece(new API.Piece(color, move.PromotingTo), new Coord(move.Target));
    }

    public void Render() {
        foreach (Piece piece in pieces) {
            piece.Render();
        }

        if (draggedPiece != -1) pieces[draggedPiece].Render();
        if (animatedPiece != -1) pieces[animatedPiece].Render();

        if (promotionTarget != -1) RenderPromotionPicker();
    }

    // Dims the board and draws the choices on top of it
    private void RenderPromotionPicker() {
        int boardX = Math.Min(UIHelper.GetScreenX(0), UIHelper.GetScreenX(7));
        int boardY = Math.Min(UIHelper.GetScreenY(0), UIHelper.GetScreenY(7));
        Raylib.DrawRectangle(boardX, boardY, 8 * Settings.SquareSideLength, 8 * Settings.SquareSideLength, Theme.PromotionOverlayCol);

        Vector2 mouse = Raylib.GetMousePosition();
        foreach (Piece choice in promotionChoices) {
            Rectangle rect = new Rectangle(choice.X, choice.Y, Settings.SquareSideLength, Settings.SquareSideLength);
            bool hovered = Raylib.CheckCollisionPointRec(mouse, rect);

            Raylib.DrawRectangleRec(rect, hovered ? Theme.PromotionHoverCol : Theme.PromotionChoiceCol);
            choice.Render();
        }
    }
}