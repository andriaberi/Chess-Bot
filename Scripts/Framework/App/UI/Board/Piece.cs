namespace Chess.UI;

using Chess.API;
using Chess.Utility;
using Raylib_cs;
using System.Numerics;

class Piece {
    public Coord Coord;
    public float X, Y;
    private Texture2D texture; // Preloaded texture to avoid loading it every frame

    public Piece(API.Piece piece, Coord coord) {
        Coord = coord;
        X = UIHelper.GetScreenX(coord);
        Y = UIHelper.GetScreenY(coord);

        texture = UIHelper.LoadPieceTexture(piece);
    }

    // When illegal move is played, piece goes to its original position
    public void ResetPosition() {
        X = UIHelper.GetScreenX(Coord);
        Y = UIHelper.GetScreenY(Coord);
    }

    public void Render() {
        Raylib.DrawTexturePro(
            texture,
            new Rectangle(0, 0, texture.Width, texture.Height),
            new Rectangle(X, Y, Settings.SquareSideLength, Settings.SquareSideLength),
            new Vector2(0, 0),
            0,
            Color.White
        );
    }
}