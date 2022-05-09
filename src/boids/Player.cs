using Godot;

public class Player : BoidBase
{
    [Export] public float Damping = 0.5f;

    public AudioStreamPlayer3D _sfxPickup;

    public Color _colour;
    private bool _destroyed = false;
    private bool _queueAddBoids = false;
    
    public override void _Ready()
    {
        _colour = ColourManager.Instance.Secondary;
        _sfxPickup = GetNode("SFXPickup") as AudioStreamPlayer3D;

        Connect("area_entered", this, nameof(_OnBoidAreaEntered));
    }

    public override void _Process(float delta)
    {
        if (!_destroyed)
        {
            Vector2 mousePos = GlobalCamera.Instance.MousePosition();
            Vector2 lookAt = mousePos - GlobalPosition;
            Rotation = new Vector3(0.0f, -Mathf.Atan2(lookAt.x, -lookAt.y), 0.0f);

            Vector2 forward = new Vector2(0.0f, -1.0f);
            Vector2 left = new Vector2(-1.0f, 0.0f);
            float accel = _game.BasePlayerSpeed;

            Vector2 dir = new Vector2(0.0f, 0.0f);
            if (Input.IsActionPressed("w"))
            {
                dir += forward;
            }

            if (Input.IsActionPressed("s"))
            {
                dir += -forward;
            }

            if (Input.IsActionPressed("a"))
            {
                dir += left;
            }

            if (Input.IsActionPressed("d"))
            {
                dir += -left;
            }

            if (dir != new Vector2(0.0f, 0.0f))
            {
                dir = dir.Normalized();
                dir *= 100.0f * accel * delta;
                _velocity += dir.To3D();
            }
            
            GlobalTranslate(_velocity * delta);

            _velocity *= Mathf.Pow(1.0f - Mathf.Clamp(Damping, 0.0f, 1.0f), delta * 60.0f);

            //	if Input.IsActionJustPressed("formation_1"):
            //		_game.ChangeFormation(0, false)
            //	if Input.IsActionJustPressed("formation_2"):
            //		_game.ChangeFormation(1, false)
            //	if Input.IsActionJustPressed("formation_3"):
            //		_game.ChangeFormation(2, false)

            if (Input.IsActionJustPressed("boids_align"))
            {
                _game.ChangeFormation((Game.Formation)1, false);
            }

            if (Input.IsActionJustReleased("boids_align"))
            {
                _game.ChangeFormation((Game.Formation)0, false);
            }
        }

        if (_queueAddBoids)
        {
            _game.AddBoids(GlobalPosition);
            _queueAddBoids = false;
        }
    }

    protected override void _Destroy(bool score)
    {
        base._Destroy(score);
    }

    public override void _OnBoidAreaEntered(Area area)
    {
        if (area is PickupAdd)
        {
            area.QueueFree();
            _queueAddBoids = true;
            _sfxPickup.Play();
        }
    }
}