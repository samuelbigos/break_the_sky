using Godot;
using System;

public class BoidAllyDrone : BoidAllyBase
{
    [Export] private PackedScene _bulletScene;
    
    private float _shootCooldown;
    private bool _cachedShoot;
    
    public override void _Ready()
    {
        base._Ready();
    }

    public override void Init(string id, Action<BoidBase> onDestroy, Vector2 position, Vector2 velocity)
    {
        base.Init(id, onDestroy, position, velocity);
        
        SetTarget(TargetType.Ally, Game.Player);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        Vector2 shootDir = (GameCamera.Instance.MousePosition - GlobalPosition).Normalized();
        
        _shootCooldown -= delta;
        if (_cachedShoot)
        {
            if (_CanShoot(shootDir))
            {
                _Shoot(shootDir);
            }
        }

        if (_shootCooldown > 0.0f)
        {
            float t = _shootCooldown / Game.Instance.BaseBoidReload;
            t = Mathf.Pow(Mathf.Clamp(t, 0.0f, 1.0f), 5.0f);
            Vector3 from = _baseScale * 2.0f;
            _mesh.Scale = from.LinearInterpolate(_baseScale, 1.0f - t);
        }
    }
    

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionPressed("shoot"))
        {
            _cachedShoot = true;
        }

        if (@event.IsActionReleased("shoot"))
        {
            _cachedShoot = false;
        }
    }

    protected override bool _CanShoot(Vector2 dir)
    {
        return _shootCooldown <= 0.0 && base._CanShoot(dir);
    }

    protected override void _Shoot(Vector2 dir)
    {
        base._Shoot(dir);
        
        _shootCooldown = Game.Instance.BaseBoidReload;
        Bullet bullet = _bulletScene.Instance() as Bullet;
        float spread = Game.Instance.BaseBoidSpread;
        dir += new Vector2(-dir.y, dir.x) * (float) GD.RandRange(-spread, spread);
        Game.Instance.AddChild(bullet);
        bullet.Init(GlobalPosition, dir * Game.Instance.BaseBulletSpeed, Alignment, Game.Instance.BaseBoidDamage);
    }
}
