using UnityEngine;

// A clickable square shown for a legal move. It only reports the click;
// all game-state changes happen in GameController.ExecuteMove.
public class MovePlate : MonoBehaviour
{
    public GameObject controller;
    private GameObject reference = null;
    private int matrixX;
    private int matrixY;

    public bool attack = false;
    public bool isCastling = false;
    public bool isEnPassant = false;

    public void Start()
    {
        if (attack)
        {
            gameObject.GetComponent<SpriteRenderer>().color = Color.red;
        }
    }

    public void OnMouseUp()
    {
        if (controller == null)
        {
            controller = GameObject.FindGameObjectWithTag("GameController");
        }

        if (controller == null || reference == null)
        {
            return;
        }

        GameController gc = controller.GetComponent<GameController>();
        if (gc.IsInputBlocked())
        {
            return;
        }

        ChessManager piece = reference.GetComponent<ChessManager>();
        Move move = new Move(matrixX, matrixY, isCastling, isEnPassant);

        piece.DestroyMovePlates();
        gc.ExecuteMove(piece, move);
    }

    public void SetCoords(int x, int y)
    {
        matrixX = x;
        matrixY = y;
    }

    public void SetReference(GameObject obj)
    {
        reference = obj;
    }

    public GameObject GetReference()
    {
        return reference;
    }
}