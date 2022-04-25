using Godot;

public class CarrierRotorgun : BoidEnemyBase
{
    [Export] private PackedScene BulletScene;
    [Export] public float BulletSpeed = 200.0f;
    [Export] public float BulletRange = 500.0f;
    [Export] public float BulletCooldown = 1.0f;

    public Sprite _blade;
    public AudioStreamPlayer2D _sfxFire;

    public Node2D Lock;
    private float _rotVel = Mathf.Pi * 2.0f;
    public float _shotCooldown;

    private Particles2D _damaged;
    private BoidEnemyCarrier _parent;


    public override void _Ready()
    {
        base._Ready();

        _blade = GetNode("Blade") as Sprite;
        _sfxFire = GetNode("SFXFire") as AudioStreamPlayer2D;
        _blade.Modulate = ColourManager.Instance.Secondary;

        _damaged = GetNode("Damaged") as Particles2D;
        ;
        _damaged.ProcessMaterial = (Material) _damaged.ProcessMaterial.Duplicate(true);
        _parent = GetParent() as BoidEnemyCarrier;
    }

    public override void _Process(float delta)
    {
        _blade.Scale = _sprite.Scale;
        _blade.Rotation = Mathf.PosMod(_blade.Rotation + 50.0f * delta, Mathf.Pi * 2.0f);

        //	var toTarget = (_target.global_position - global_position).Normalized();
        //	var awayParent = (lock.global_position - global_position).Normalized();
        //	var rotAwayParent = -atan2(awayParent.x, awayParent.y) - GetParent().rotation + Mathf.Pi
        //	var rot = -atan2(toTarget.x, toTarget.y) - GetParent().rotation + Mathf.Pi
        //	rot = Mathf.Clamp(rot + Mathf.Pi * 2.0, rotAwayParent - Mathf.Pi * 0.5 + Mathf.Pi * 2.0, rotAwayParent + Mathf.Pi * 0.5 + Mathf.Pi * 2.0);
        //	rotation = rot;

        var s = Mathf.Clamp(1.0f - _health / MaxHealth, 0.2f, 1.0f);
        _damaged.Scale = new Vector2(s * 10.0f, s * 10.0f);
        //_damaged.ProcessMaterial = s * 5.0f;

        if (!_destroyed)
        {
            //var awayLock = (lock.global_position - global_position).Normalized();
            var toTarget = (_target.GlobalPosition - GlobalPosition).Normalized();
            var rot = -Mathf.Atan2(toTarget.x, toTarget.y) - _parent.Rotation + Mathf.Pi;
            Rotation = rot;

            var awayParent = (Lock.GlobalPosition - GlobalPosition).Normalized();
            var dist = (_target.GlobalPosition - GlobalPosition).Length();
            _shotCooldown -= delta;
            if (toTarget.Dot(awayParent) > 0.0f && _shotCooldown < 0.0f && dist < BulletRange)
            {
                _Shoot();
                _shotCooldown = BulletCooldown;
            }
        }
    }

    public void _Shoot()
    {
        BulletBeacon bullet = BulletScene.Instance() as BulletBeacon;
        Vector2 dir = (_target.GlobalPosition - GlobalPosition).Normalized();
        bullet.Init(dir * BulletSpeed, 1, _game.PlayRadius);
        bullet.GlobalPosition = GlobalPosition + dir * 80.0f;
        _game.AddChild(bullet);
        _sfxFire.Play();
    }

    protected override void Destroy(bool score)
    {
        base.Destroy(score);
        _sfxDestroy.Play();
        var pos = GlobalPosition;
        GetParent().RemoveChild(this);
        _game.AddChild(this);
        GlobalPosition = pos;
        _blade.Visible = false;
    }
}