using Godot;

public class BoidEnemyCarrierRotorgun : BoidEnemyBase
{
    [Export] private PackedScene _bulletScene;
    [Export] private float _bulletSpeed = 200.0f;
    [Export] private float _bulletRange = 500.0f;
    [Export] private float _bulletCooldown = 1.0f;

    private Sprite _blade;

    private Node2D _lock;
    private float _rotVel = Mathf.Pi * 2.0f;
    private float _shotCooldown;

    private BoidEnemyCarrier _parent;

    public virtual void Init(Leader player, Game game, BoidBase target, Node2D lockNode)
    {
        _player = player;
        _game = game;
        _target = target;
        _lock = lockNode;
    }
    
    public override void _Ready()
    {
        base._Ready();

        _blade = GetNode("Blade") as Sprite;
        _blade.Modulate = ColourManager.Instance.Secondary;
        
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

        float s = Mathf.Clamp(1.0f - _health / MaxHealth, 0.2f, 1.0f);
        _damagedParticles.Scale = new Vector2(s * 10.0f, s * 10.0f);
        //_damaged.ProcessMaterial = s * 5.0f;

        if (!_destroyed)
        {
            //var awayLock = (lock.global_position - global_position).Normalized();
            Vector2 toTarget = (_target.GlobalPosition - GlobalPosition).Normalized();
            float rot = -Mathf.Atan2(toTarget.x, toTarget.y) - _parent.Rotation + Mathf.Pi;
            Rotation = rot;

            Vector2 awayParent = (_lock.GlobalPosition - GlobalPosition).Normalized();
            float dist = (_target.GlobalPosition - GlobalPosition).Length();
            _shotCooldown -= delta;
            if (toTarget.Dot(awayParent) > 0.0f && _shotCooldown < 0.0f && dist < _bulletRange)
            {
                _Shoot(new Vector2());
                _shotCooldown = _bulletCooldown;
            }
        }
    }

    protected override void _Shoot(Vector2 dir)
    {
        base._Shoot(dir);
        
        BulletBeacon bullet = _bulletScene.Instance() as BulletBeacon;
        dir = (_target.GlobalPosition - GlobalPosition).Normalized();
        bullet.Init(dir * _bulletSpeed, 1, _game.PlayRadius, 1.0f);
        bullet.GlobalPosition = GlobalPosition + dir * 80.0f;
        _game.AddChild(bullet);
    }

    protected override void _Destroy(bool score)
    {
        base._Destroy(score);
        
        _blade.Visible = false;
    }
}