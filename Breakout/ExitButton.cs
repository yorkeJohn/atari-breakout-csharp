namespace Breakout;

public sealed class ExitButton : Button
{
    public ExitButton(Font font, int width)
    {
        Text = "Exit Game";
        Font = font;
        BackColor = Color.Black;
        ForeColor = Color.White;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.MouseDownBackColor = Color.Black;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Size = new Size(width, 100);
    }
}