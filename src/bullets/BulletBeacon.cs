using Godot;

public class BulletBeacon : Node2D
{
    public Vector2 _velocity;
    public int _alignment;
    public float _playRadius;
    private float _radius = 7.0f;
    private float _damage = 1.0f;
    private float _health = 1.0f;

    public float GetAlignment()
    {
        return _alignment;
    }

    public override void _Ready()
    {
    }

    public void Init(Vector2 velocity, int alignment, float playRadius)
    {
        _velocity = velocity;
        _alignment = alignment;
        _playRadius = playRadius;

        Rotation = -Mathf.Atan2(_velocity.x, _velocity.y);
    }

    public override void _Process(float delta)
    {
        GlobalPosition += _velocity * delta;
        if (GlobalPosition.Length() > _playRadius)
        {
            QueueFree();
        }
    }

    public void OnHit()
    {
        _health -= 1.0f;
        if ((_health <= 0.0f))
        {
            QueueFree();
        }
    }

    public override void _Draw()
    {
        DrawCircle(new Vector2(0.0f, 0.0f), _radius, ColourManager.Instance.Secondary);
        DrawCircle(new Vector2(0.0f, 0.0f), _radius - 3.0f, ColourManager.Instance.White);
    }
}