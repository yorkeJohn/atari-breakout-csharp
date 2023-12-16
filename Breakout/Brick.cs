namespace Breakout;

public class Brick
{
    public Rectangle Rect { get; }
    public readonly int Row;

    public Brick(Rectangle rect, int row)
    {
        Rect = rect;
        Row = row;
    }

    public int GetPointValue()
    {
        return (int)Math.Floor((double)Row / 2) * 2 + 1;
    }

    private Color GetColor()
    {
        return Row switch
        {
            < 2 => Color.Yellow,
            < 4 => Color.Green,
            < 6 => Color.Orange,
            < 8 => Color.Red,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void Draw(Graphics g)
    {
        g.FillRectangle(new SolidBrush(GetColor()), Rect);
    }
}