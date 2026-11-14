using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Owns the board and the rules of the game: turns, legal-move checking,
// special moves (castling, en passant, promotion) and game-end detection.
public class GameController : MonoBehaviour
{
    public const float SquareSize = 0.66f;
    public const float BoardOffset = -2.3f;

    public GameObject chesspiece;
    public Timer timer;

    // Optional: a UI panel with four buttons (queen / rook / bishop / knight).
    // If left empty, a pawn that reaches the last rank is promoted to a queen automatically.
    public GameObject promotionPanel;

    private static readonly string[] BackRank =
    {
        "rook", "knight", "bishop", "queen", "king", "bishop", "knight", "rook"
    };

    private ChessManager[,] board = new ChessManager[8, 8];

    private string currentPlayer = "white";
    private bool gameOver = false;

    // The square a pawn just skipped over with a double step (-1 if none).
    private int enPassantX = -1;
    private int enPassantY = -1;

    private int halfMoveClock = 0; // half-moves since the last capture or pawn move
    private ChessManager pendingPromotion = null;
    private Dictionary<string, int> positionCounts = new Dictionary<string, int>();
    private int lastRepetitionCount = 0;

    public int EnPassantX { get { return enPassantX; } }
    public int EnPassantY { get { return enPassantY; } }

    // ---------------------------------------------------------------- setup

    public void Start()
    {
        SetupBoard();

        if (promotionPanel != null)
        {
            promotionPanel.SetActive(false);
        }

        lastRepetitionCount = RecordPosition();

        if (timer != null)
        {
            timer.StartTimer();
        }
    }

    private void SetupBoard()
    {
        for (int x = 0; x < 8; x++)
        {
            Create("white_" + BackRank[x], x, 0);
            Create("white_pawn", x, 1);
            Create("black_" + BackRank[x], x, 7);
            Create("black_pawn", x, 6);
        }
    }

    public ChessManager Create(string pieceName, int x, int y)
    {
        GameObject obj = Instantiate(chesspiece, new Vector3(0, 0, -1), Quaternion.identity);
        ChessManager cm = obj.GetComponent<ChessManager>();
        cm.name = pieceName;
        cm.SetXBoard(x);
        cm.SetYBoard(y);
        cm.Activate(this);
        board[x, y] = cm;
        return cm;
    }

    // ------------------------------------------------------------- accessors

    public static Vector3 BoardToWorld(int x, int y, float z)
    {
        return new Vector3(x * SquareSize + BoardOffset, y * SquareSize + BoardOffset, z);
    }

    public static string Opponent(string player)
    {
        return player == "white" ? "black" : "white";
    }

    public bool PositionOnBoard(int x, int y)
    {
        return x >= 0 && y >= 0 && x < 8 && y < 8;
    }

    public ChessManager GetPosition(int x, int y)
    {
        return PositionOnBoard(x, y) ? board[x, y] : null;
    }

    public string GetCurrentPlayer() { return currentPlayer; }
    public bool IsGameOver() { return gameOver; }

    // True while the game is over or we are waiting for a promotion choice.
    public bool IsInputBlocked()
    {
        return gameOver || pendingPromotion != null;
    }

    public void Update()
    {
        if (gameOver && Input.GetMouseButtonDown(0))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // ------------------------------------------------------ check detection

    public bool IsKingInCheck(string player)
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessManager p = board[x, y];
                if (p != null && p.player == player && p.pieceType == "king")
                {
                    return IsSquareAttacked(x, y, Opponent(player));
                }
            }
        }
        return false;
    }

    // Is the square attacked by any piece of byPlayer?
    public bool IsSquareAttacked(int x, int y, string byPlayer)
    {
        // Pawns attack diagonally forward, so an attacker sits one rank "behind" the target.
        int pawnDir = byPlayer == "white" ? 1 : -1;
        if (IsPiece(x - 1, y - pawnDir, byPlayer, "pawn") || IsPiece(x + 1, y - pawnDir, byPlayer, "pawn"))
        {
            return true;
        }

        int[] knightX = { 1, -1, 2, 2, 1, -1, -2, -2 };
        int[] knightY = { 2, 2, 1, -1, -2, -2, 1, -1 };
        for (int i = 0; i < knightX.Length; i++)
        {
            if (IsPiece(x + knightX[i], y + knightY[i], byPlayer, "knight"))
            {
                return true;
            }
        }

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if ((dx != 0 || dy != 0) && IsPiece(x + dx, y + dy, byPlayer, "king"))
                {
                    return true;
                }
            }
        }

        // Straight lines: rook or queen. Diagonals: bishop or queen.
        if (RayHits(x, y, 1, 0, byPlayer, "rook") || RayHits(x, y, -1, 0, byPlayer, "rook") ||
            RayHits(x, y, 0, 1, byPlayer, "rook") || RayHits(x, y, 0, -1, byPlayer, "rook"))
        {
            return true;
        }

        if (RayHits(x, y, 1, 1, byPlayer, "bishop") || RayHits(x, y, 1, -1, byPlayer, "bishop") ||
            RayHits(x, y, -1, 1, byPlayer, "bishop") || RayHits(x, y, -1, -1, byPlayer, "bishop"))
        {
            return true;
        }

        return false;
    }

    private bool IsPiece(int x, int y, string player, string type)
    {
        ChessManager p = GetPosition(x, y);
        return p != null && p.player == player && p.pieceType == type;
    }

    // Walks from (x, y) in one direction; true if the first piece met is a slider of that kind (or a queen).
    private bool RayHits(int x, int y, int dx, int dy, string byPlayer, string sliderType)
    {
        int cx = x + dx;
        int cy = y + dy;
        while (PositionOnBoard(cx, cy))
        {
            ChessManager p = board[cx, cy];
            if (p != null)
            {
                return p.player == byPlayer && (p.pieceType == sliderType || p.pieceType == "queen");
            }
            cx += dx;
            cy += dy;
        }
        return false;
    }

    // Plays the move on the board temporarily and reports whether our king is safe afterwards.
    public bool IsMoveSafe(ChessManager piece, Move move)
    {
        int ox = piece.GetXBoard();
        int oy = piece.GetYBoard();
        int capX = move.x;
        int capY = move.isEnPassant ? oy : move.y;
        ChessManager captured = board[capX, capY];

        board[ox, oy] = null;
        board[capX, capY] = null;
        board[move.x, move.y] = piece;

        bool safe = !IsKingInCheck(piece.player);

        board[move.x, move.y] = null;
        board[capX, capY] = captured;
        board[ox, oy] = piece;

        return safe;
    }

    public bool HasAnyLegalMove(string player)
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessManager p = board[x, y];
                if (p != null && p.player == player && p.GetLegalMoves().Count > 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // ------------------------------------------------------- executing moves

    public void ExecuteMove(ChessManager piece, Move move)
    {
        if (IsInputBlocked())
        {
            return;
        }

        int ox = piece.GetXBoard();
        int oy = piece.GetYBoard();
        bool isPawn = piece.pieceType == "pawn";

        // Capture (for en passant the victim is beside us, not on the destination square).
        int capX = move.x;
        int capY = move.isEnPassant ? oy : move.y;
        ChessManager captured = board[capX, capY];
        if (captured != null)
        {
            board[capX, capY] = null;
            Destroy(captured.gameObject);
        }

        if (move.isCastling)
        {
            MoveCastlingRook(ox, move);
        }

        // Move the piece.
        board[ox, oy] = null;
        piece.SetXBoard(move.x);
        piece.SetYBoard(move.y);
        piece.SetCoords();
        piece.hasMoved = true;
        board[move.x, move.y] = piece;

        // A double pawn step makes en passant possible for exactly one reply.
        if (isPawn && Mathf.Abs(move.y - oy) == 2)
        {
            enPassantX = move.x;
            enPassantY = (move.y + oy) / 2;
        }
        else
        {
            enPassantX = -1;
            enPassantY = -1;
        }

        halfMoveClock = (isPawn || captured != null) ? 0 : halfMoveClock + 1;

        bool reachesLastRank = isPawn && (move.y == 7 || move.y == 0);
        if (reachesLastRank)
        {
            if (promotionPanel != null)
            {
                pendingPromotion = piece; // the turn ends after the player picks a piece
                promotionPanel.SetActive(true);
                return;
            }
            piece.Promote("queen");
        }

        EndTurn();
    }

    private void MoveCastlingRook(int kingFromX, Move kingMove)
    {
        bool kingSide = kingMove.x > kingFromX;
        int rookFromX = kingSide ? 7 : 0;
        int rookToX = kingSide ? kingMove.x - 1 : kingMove.x + 1;

        ChessManager rook = board[rookFromX, kingMove.y];
        if (rook == null)
        {
            return;
        }

        board[rookFromX, kingMove.y] = null;
        rook.SetXBoard(rookToX);
        rook.SetCoords();
        rook.hasMoved = true;
        board[rookToX, kingMove.y] = rook;
    }

    // -------------------------------------------------------------- promotion

    // Hook these up to the four promotion buttons (Button -> OnClick).
    public void PromoteToQueen() { ChoosePromotion("queen"); }
    public void PromoteToRook() { ChoosePromotion("rook"); }
    public void PromoteToBishop() { ChoosePromotion("bishop"); }
    public void PromoteToKnight() { ChoosePromotion("knight"); }

    public void ChoosePromotion(string pieceType)
    {
        if (pendingPromotion == null)
        {
            return;
        }

        if (pieceType != "queen" && pieceType != "rook" && pieceType != "bishop" && pieceType != "knight")
        {
            pieceType = "queen";
        }

        pendingPromotion.Promote(pieceType);
        pendingPromotion = null;

        if (promotionPanel != null)
        {
            promotionPanel.SetActive(false);
        }

        EndTurn();
    }

    // ------------------------------------------------------------ turn / end

    private void EndTurn()
    {
        currentPlayer = Opponent(currentPlayer);
        lastRepetitionCount = RecordPosition();
        CheckGameEnd();
    }

    private void CheckGameEnd()
    {
        if (!HasAnyLegalMove(currentPlayer))
        {
            if (IsKingInCheck(currentPlayer))
            {
                EndGame("Checkmate! " + Opponent(currentPlayer) + " is the winner");
            }
            else
            {
                EndGame("Stalemate - draw");
            }
            return;
        }

        if (IsInsufficientMaterial())
        {
            EndGame("Draw - insufficient material");
        }
        else if (halfMoveClock >= 100)
        {
            EndGame("Draw - 50-move rule");
        }
        else if (lastRepetitionCount >= 3)
        {
            EndGame("Draw - threefold repetition");
        }
    }

    private bool IsInsufficientMaterial()
    {
        int knights = 0;
        int darkBishops = 0;
        int lightBishops = 0;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessManager p = board[x, y];
                if (p == null)
                {
                    continue;
                }

                switch (p.pieceType)
                {
                    case "king":
                        break;
                    case "knight":
                        knights++;
                        break;
                    case "bishop":
                        if ((x + y) % 2 == 0) darkBishops++; else lightBishops++;
                        break;
                    default:
                        return false; // a pawn, rook or queen can still deliver mate
                }
            }
        }

        int bishops = darkBishops + lightBishops;
        if (knights + bishops <= 1)
        {
            return true; // king vs king, or king + one minor piece vs king
        }

        // Only bishops, all on the same colour of square.
        return knights == 0 && (darkBishops == 0 || lightBishops == 0);
    }

    private int RecordPosition()
    {
        string key = BuildPositionKey();
        int count;
        positionCounts.TryGetValue(key, out count);
        count++;
        positionCounts[key] = count;
        return count;
    }

    private string BuildPositionKey()
    {
        StringBuilder sb = new StringBuilder(96);
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                ChessManager p = board[x, y];
                if (p == null)
                {
                    sb.Append('.');
                    continue;
                }

                char c = p.pieceType == "knight" ? 'n' : p.pieceType[0];
                sb.Append(p.player == "white" ? char.ToUpper(c) : c);

                // Castling rights depend on whether kings and rooks have moved.
                if (p.pieceType == "king" || p.pieceType == "rook")
                {
                    sb.Append(p.hasMoved ? '+' : '-');
                }
            }
        }

        sb.Append(currentPlayer[0]);
        sb.Append(enPassantX);
        return sb.ToString();
    }

    public void EndGame(string message)
    {
        gameOver = true;

        if (timer != null)
        {
            timer.StopTimer();
        }

        ShowText("WinnerText", message);
        ShowText("RestartText", null);
    }

    // Kept so older code that calls Winner("white") still works.
    public void Winner(string playerWinner)
    {
        EndGame(playerWinner + " is the winner");
    }

    private void ShowText(string tag, string message)
    {
        GameObject obj = GameObject.FindGameObjectWithTag(tag);
        if (obj == null)
        {
            Debug.LogWarning("No object with tag " + tag + " found in the scene.");
            return;
        }

        Text text = obj.GetComponent<Text>();
        if (text == null)
        {
            return;
        }

        text.enabled = true;
        if (message != null)
        {
            text.text = message;
        }
    }
}