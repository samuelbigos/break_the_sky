using System.Diagnostics;
using Godot;

public partial class FlowFieldResource : Resource
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
