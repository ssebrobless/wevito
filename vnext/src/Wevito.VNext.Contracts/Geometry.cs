namespace Wevito.VNext.Contracts;

public readonly record struct PointInt(int X, int Y);

public readonly record struct SizeInt(int Width, int Height);

public readonly record struct RectInt(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;

    public PointInt BottomRight => new(Right, Bottom);

    public bool Contains(PointInt point)
    {
        return point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;
    }
}
