using System.Collections.Generic;
using UnityEngine;

// A single chess piece: knows its type, colour and which moves it could make.
// The piece type is read from the GameObject name ("white_pawn", "black_queen", ...).
public class ChessManager : MonoBehaviour
{
    public GameObject controller;
    public GameObject movePlate;

    private GameController gc;
    private int xBoard = -1;
    private int yBoard = -1;

    public string player;      // "white" or "black"
    public string pieceType;   // "pawn", "knight", "bishop", "rook", "queen", "king"
    public bool hasMoved = false;

    public Sprite black_queen, black_knight, black_bishop, black_king, black_rook, black_pawn;
    public Sprite white_queen, white_knight, white_bishop, white_king, white_rook, white_pawn;

    private static readonly int[][] RookDirections =
    {
        new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 }
    };

    private static readonly int[][] BishopDirections =
    {
        new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 }
    };

    private static readonly int[] KnightX = { 1, -1, 2, 2, 1, -1, -2, -2 };
    private static readonly int[] KnightY = { 2, 2, 1, -1, -2, -2, 1, -1 };

    // ---------------------------------------------------------------- setup

    public void Activate(GameController gameController)
    {
        gc = gameController;
        controller = gameController.gameObject;
        ParseName();
        SetCoords();
        UpdateSprite();
    }

    private void ParseName()
    {
        string[] parts = name.Split('_');
        if (parts.Length != 2)
        {
            Debug.LogError("Unexpected piece name: " + name);
            return;
        }

        player = parts[0];
        pieceType = parts[1];
    }

    // Turns a pawn into another piece (pawn promotion).
    public void Promote(string newType)
    {
        pieceType = newType;
        name = player + "_" + newType;
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        GetComponent<SpriteRenderer>().sprite = SpriteFor(player, pieceType);
    }

    private Sprite SpriteFor(string color, string type)
    {
        bool white = color == "white";
        switch (type)
        {
            case "queen": return white ? white_queen : black_queen;
            case "knight": return white ? white_knight : black_knight;
            case "bishop": return white ? white_bishop : black_bishop;
            case "king": return white ? white_king : black_king;
            case "rook": return white ? white_rook : black_rook;
            case "pawn": return white ? white_pawn : black_pawn;
        }
        return null;
    }

    // ---------------------------------------------------------- coordinates

    public void SetCoords()
    {
        transform.position = GameController.BoardToWorld(xBoard, yBoard, -1.0f);
    }

    public int GetXBoard() { return xBoard; }
    public int GetYBoard() { return yBoard; }
    public void SetXBoard(int x) { xBoard = x; }
    public void SetYBoard(int y) { yBoard = y; }

    // ---------------------------------------------------------------- input

    private void OnMouseUp()
    {
        if (gc == null || gc.IsInputBlocked() || gc.GetCurrentPlayer() != player)
        {
            return;
        }

        DestroyMovePlates();

        foreach (Move move in GetLegalMoves())
        {
            SpawnMovePlate(move);
        }
    }

    public void DestroyMovePlates()
    {
        GameObject[] plates = GameObject.FindGameObjectsWithTag("MovePlate");
        for (int i = 0; i < plates.Length; i++)
        {
            Destroy(plates[i]);
        }
    }

    private void SpawnMovePlate(Move move)
    {
        bool isAttack = gc.GetPosition(move.x, move.y) != null || move.isEnPassant;

        GameObject mp = Instantiate(movePlate, GameController.BoardToWorld(move.x, move.y, -3.0f), Quaternion.identity);

        MovePlate script = mp.GetComponent<MovePlate>();
        script.controller = controller;
        script.attack = isAttack;
        script.isCastling = move.isCastling;
        script.isEnPassant = move.isEnPassant;
        script.SetReference(gameObject);
        script.SetCoords(move.x, move.y);
    }

    // ---------------------------------------------------------------- moves

    // Moves that do not leave our own king in check.
    public List<Move> GetLegalMoves()
    {
        List<Move> legal = new List<Move>();
        foreach (Move move in GetPseudoLegalMoves())
        {
            if (gc.IsMoveSafe(this, move))
            {
                legal.Add(move);
            }
        }
        return legal;
    }

    // Moves by the piece's own movement rules (king safety is checked separately).
    public List<Move> GetPseudoLegalMoves()
    {
        List<Move> moves = new List<Move>();

        switch (pieceType)
        {
            case "queen":
                AddSlidingMoves(moves, RookDirections);
                AddSlidingMoves(moves, BishopDirections);
                break;
            case "rook":
                AddSlidingMoves(moves, RookDirections);
                break;
            case "bishop":
                AddSlidingMoves(moves, BishopDirections);
                break;
            case "knight":
                for (int i = 0; i < KnightX.Length; i++)
                {
                    AddStepMove(moves, xBoard + KnightX[i], yBoard + KnightY[i]);
                }
                break;
            case "king":
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx != 0 || dy != 0)
                        {
                            AddStepMove(moves, xBoard + dx, yBoard + dy);
                        }
                    }
                }
                AddCastlingMoves(moves);
                break;
            case "pawn":
                AddPawnMoves(moves);
                break;
        }

        return moves;
    }

    private void AddSlidingMoves(List<Move> moves, int[][] directions)
    {
        foreach (int[] dir in directions)
        {
            int x = xBoard + dir[0];
            int y = yBoard + dir[1];

            while (gc.PositionOnBoard(x, y))
            {
                ChessManager target = gc.GetPosition(x, y);
                if (target == null)
                {
                    moves.Add(new Move(x, y));
                }
                else
                {
                    if (target.player != player)
                    {
                        moves.Add(new Move(x, y));
                    }
                    break;
                }

                x += dir[0];
                y += dir[1];
            }
        }
    }

    private void AddStepMove(List<Move> moves, int x, int y)
    {
        if (!gc.PositionOnBoard(x, y))
        {
            return;
        }

        ChessManager target = gc.GetPosition(x, y);
        if (target == null || target.player != player)
        {
            moves.Add(new Move(x, y));
        }
    }

    private void AddCastlingMoves(List<Move> moves)
    {
        if (hasMoved || xBoard != 4)
        {
            return;
        }

        string enemy = GameController.Opponent(player);
        if (gc.IsSquareAttacked(xBoard, yBoard, enemy))
        {
            return; // cannot castle out of check
        }

        foreach (int rookX in new[] { 0, 7 })
        {
            ChessManager rook = gc.GetPosition(rookX, yBoard);
            if (rook == null || rook.pieceType != "rook" || rook.player != player || rook.hasMoved)
            {
                continue;
            }

            int dir = rookX < xBoard ? -1 : 1;

            bool pathClear = true;
            for (int x = xBoard + dir; x != rookX; x += dir)
            {
                if (gc.GetPosition(x, yBoard) != null)
                {
                    pathClear = false;
                    break;
                }
            }
            if (!pathClear)
            {
                continue;
            }

            int passX = xBoard + dir;
            int destX = xBoard + 2 * dir;
            if (gc.IsSquareAttacked(passX, yBoard, enemy) || gc.IsSquareAttacked(destX, yBoard, enemy))
            {
                continue;
            }

            moves.Add(new Move(destX, yBoard, true));
        }
    }

    private void AddPawnMoves(List<Move> moves)
    {
        int forward = player == "white" ? 1 : -1;
        int y = yBoard + forward;

        // Forward one, and two from the starting square.
        if (gc.PositionOnBoard(xBoard, y) && gc.GetPosition(xBoard, y) == null)
        {
            moves.Add(new Move(xBoard, y));

            int y2 = y + forward;
            if (!hasMoved && gc.PositionOnBoard(xBoard, y2) && gc.GetPosition(xBoard, y2) == null)
            {
                moves.Add(new Move(xBoard, y2));
            }
        }

        // Diagonal captures and en passant.
        for (int dx = -1; dx <= 1; dx += 2)
        {
            int x = xBoard + dx;
            if (!gc.PositionOnBoard(x, y))
            {
                continue;
            }

            ChessManager target = gc.GetPosition(x, y);
            if (target != null)
            {
                if (target.player != player)
                {
                    moves.Add(new Move(x, y));
                }
            }
            else if (x == gc.EnPassantX && y == gc.EnPassantY)
            {
                ChessManager victim = gc.GetPosition(x, yBoard);
                if (victim != null && victim.pieceType == "pawn" && victim.player != player)
                {
                    moves.Add(new Move(x, y, false, true));
                }
            }
        }
    }
}