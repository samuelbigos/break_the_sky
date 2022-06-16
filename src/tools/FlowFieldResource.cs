using System.Diagnostics;
using Godot;
using Vector2 = System.Numerics.Vector2;

public class FlowFieldResource : Resource
{
    [Export] public int X;
    [Export] public int Y;
    [Export] public Vector2[] Vectors;

    public Vector2 VectorAt(int x, int y)
    {
        Debug.Assert(x < X && y < Y, "x < X && y < Y");
        return Vectors[y * X + x];
    }
}
