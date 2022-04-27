using Godot;

public class BulletBeacon : Bullet
{
    private float _radius = 7.0f;

    public override void _Draw()
    {
        DrawCircle(new Vector2(0.0f, 0.0f), _radius, ColourManager.Instance.Secondary);
        DrawCircle(new Vector2(0.0f, 0.0f), _radius - 3.0f, ColourManager.Instance.White);
    }
}