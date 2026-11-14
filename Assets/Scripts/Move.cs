// A single destination square for a piece, plus flags for special moves.
public class Move
{
    public int x;
    public int y;
    public bool isCastling;
    public bool isEnPassant;

    public Move(int x, int y, bool isCastling = false, bool isEnPassant = false)
    {
        this.x = x;
        this.y = y;
        this.isCastling = isCastling;
        this.isEnPassant = isEnPassant;
    }
}