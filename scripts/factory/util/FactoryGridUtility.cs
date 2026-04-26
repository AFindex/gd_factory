using Godot;

public static class FactoryGridUtility
{
    public static Rect2I BuildCellRect(Vector2I a, Vector2I b, int padding = 0)
    {
        var minCell = new Vector2I(
            System.Math.Min(a.X, b.X) - padding,
            System.Math.Min(a.Y, b.Y) - padding);
        var maxCell = new Vector2I(
            System.Math.Max(a.X, b.X) + padding,
            System.Math.Max(a.Y, b.Y) + padding);
        return new Rect2I(
            minCell,
            new Vector2I(maxCell.X - minCell.X + 1, maxCell.Y - minCell.Y + 1));
    }
}
