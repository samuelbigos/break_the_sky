using System.Collections.Generic;
using Godot;

public class PickupAdd : Node2D
{
    [Export] public float Lifetime = 5.0f;
    [Export] public float AttractRange = 75.0f;

    private Node2D _player = null;
    public float _lifetime;
    public Color _colour;


    public void Init(Leader player)
    {
        _player = player;
    }
    
    public override void _Ready()
    {
        _lifetime = Lifetime;
        _colour = ColourManager.Instance.Secondary;
    }

    public override void _Process(float delta)
    {
        float time = OS.GetSystemTimeMsecs() / 500.0f;
        float s = 1.0f + Mathf.Sin(time) * 0.2f;
        Scale = new Vector2(s, s);
        Rotation = Mathf.Sin(time) * 1.0f;

        _lifetime -= delta;
        if (_lifetime < Lifetime * 0.5)
        {
            if ((int) (_lifetime * Mathf.Lerp(100.0f, 1.0f, _lifetime / Lifetime)) % 10 < 7)
            {
                _colour = ColourManager.Instance.Secondary;
            }
            else
            {
                _colour = ColourManager.Instance.White;
            }

            Update();
        }

        if (_lifetime < 0.0)
        {
            QueueFree();

            // attract
        }

        float dist = (_player.GlobalPosition - GlobalPosition).Length();
        if (dist < AttractRange)
        {
            GlobalPosition += (_player.GlobalPosition - GlobalPosition).Normalized() * (1.0f - (dist / AttractRange)) *
                              delta * 150.0f;
        }
    }

    public override void _Draw()
    {
        DrawArc(new Vector2(0.0f, 0.0f), 10.0f, 0.0f, 360.0f, _colour, 3.0f);
        DrawLine(new Vector2(-5.0f, 0.0f), new Vector2(5.0f, 0.0f), _colour, 3.0f);
        DrawLine(new Vector2(0.0f, -5.0f), new Vector2(0.0f, 5.0f), _colour, 3.0f);
    }

    private void DrawArc(Vector2 center, float radius, float angleTo, float angleFrom, Color color, float thickness)
    {
        int pointNum = 16;
        List<Vector2> points = new List<Vector2>();
        foreach (int i in GD.Range(pointNum + 1))
        {
            float angle = Mathf.Deg2Rad(angleFrom + i * (angleTo - angleFrom) / pointNum - 90);
            points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

        DrawPolyline(points.ToArray(), color, thickness);
    }
}