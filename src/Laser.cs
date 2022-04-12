using Godot;

public class Laser : Area2D
{
    private CollisionShape2D _shape;
    public BoidEnemyLaser.LaserState state = BoidEnemyLaser.LaserState.Inactive;

    public override void _Ready()
    {
        base._Ready();

        _shape = GetNode("CollisionShape2D") as CollisionShape2D;
    }

    public override void _Draw()
    {
        if (state == BoidEnemyLaser.LaserState.Inactive)
        {
            return;
        }

        if (state == BoidEnemyLaser.LaserState.Charging)
        {
            if (OS.GetSystemTimeMsecs() % 100 > 50)
            {
                RectangleShape2D rectShape = _shape.Shape as RectangleShape2D;
                Vector2 size = new Vector2(rectShape.Extents.x * 1.0f, rectShape.Extents.y * 2.0f);
                Rect2 rect = new Rect2(-size * 0.5f, size);
                DrawRect(rect, ColourManager.Instance.Accent, false, 4.0f);
            }
        }

        if (state == BoidEnemyLaser.LaserState.Firing)
        {
            RectangleShape2D rectShape = _shape.Shape as RectangleShape2D;
            Vector2 size = new Vector2(rectShape.Extents.x * 2.0f, rectShape.Extents.y * 2.0f);
            Rect2 rect = new Rect2(-size * 0.5f, size);
            DrawRect(rect, Colors.White, true, 2.0f);
        }
    }
}