using Godot;
using System;

public class BoidAllyDrone : BoidAllyBase
{
    [Export] private PackedScene _bulletScene;

    public float ShootCooldown => _shootCooldown;
    
    private float _shootCooldown;
    private bool _cachedShoot;

    public override void Init(ResourceBoid data, Action<BoidBase> onDestroy, Vector2 position, Vector2 velocity)
    {
        base.Init(data, onDestroy, position, velocity);
        
        SetTarget(TargetType.Ally, Game.Player);
    }

    protected override void ProcessAlive(float delta)
    {
        base.ProcessAlive(delta);
        
        // shooting
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
            float t = _shootCooldown / _resourceStats.AttackCooldown;
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

        _shootCooldown = _resourceStats.AttackCooldown;
        Bullet bullet = _bulletScene.Instance() as Bullet;
        Game.Instance.AddChild(bullet);
        bullet.Init(GlobalPosition, dir * _resourceStats.AttackVelocity, Alignment, _resourceStats.AttackDamage);
    }
}
