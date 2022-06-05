using Godot;
using System;
using System.Numerics;
using Vector2 = Godot.Vector2;

public class FlowField : Spatial
{
    [Export] private FlowFieldResource _flowField;
    [Export] private Vector2 _size;

    private Vector2 _cachedGlobalPosition;
    private Matrix4x4 _toUv;

    public override void _Process(float delta)
    {
        base._Process(delta);

        _cachedGlobalPosition = GlobalTransform.origin.To2D();
    }

    public Vector2 GetFieldAtPosition(Vector2 pos)
    {
        Vector2 flowFieldSize = new Vector2(_flowField.X, _flowField.Y);
        Vector2 uv = pos - _cachedGlobalPosition;
        uv.x /= _size.x / _flowField.X;
        uv.y /= _size.y / _flowField.Y;
        uv += flowFieldSize * 0.5f;

        int x = (int)uv.x, y = (int)uv.y;
        if (x < 0.0f || x >= _flowField.X ||
            y < 0.0f || y >= _flowField.Y)
            return Vector2.Zero;

        return _flowField.VectorAt(x, y);
    }
}
