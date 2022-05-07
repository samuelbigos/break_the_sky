using Godot;

public class PickupAdd : Area
{
    [Export] public float Lifetime = 5.0f;
    [Export] public float AttractRange = 75.0f;

    private Player _player = null;
    private float _lifetime;
    
    public Vector2 GlobalPosition
    {
        get => new Vector2(GlobalTransform.origin.x, GlobalTransform.origin.z);
        set
        {
            Transform trans = new Transform(GlobalTransform.basis, new Vector3(value.x, 0.0f, value.y));
        }
    }

    public float Rotation2D
    {
        set
        {
            Rotation = new Vector3(0.0f, value, 0.0f);
        }
    }

    public void Init(Player player)
    {
        _player = player;
    }
    
    public override void _Ready()
    {
        _lifetime = Lifetime;
    }

    public override void _Process(float delta)
    {
        float time = OS.GetSystemTimeMsecs() / 500.0f;
        float s = 1.0f + Mathf.Sin(time) * 0.2f;
        Scale = new Vector3(s, s, s);
        Rotation2D = Mathf.Sin(time) * 1.0f;

        _lifetime -= delta;
        if (_lifetime < Lifetime * 0.5)
        {
            if ((int) (_lifetime * Mathf.Lerp(100.0f, 1.0f, _lifetime / Lifetime)) % 10 < 7)
            {
                //_colour = ColourManager.Instance.Secondary;
            }
            else
            {
                //_colour = ColourManager.Instance.White;
            }
        }

        if (_lifetime < 0.0)
        {
            QueueFree();

            // attract
        }

        float dist = (_player.GlobalPosition - Utils.To2D(GlobalTransform.origin)).Length();
        if (dist < AttractRange)
        {
            GlobalPosition += (_player.GlobalPosition - GlobalPosition).Normalized() * (1.0f - (dist / AttractRange)) *
                              delta * 150.0f;
        }
    }
}