using Godot;
using System;

public class BoidAllyBomber : BoidAllyBase
{
    [Export] private PackedScene _bulletScene;
    
    private float _shootCooldown; 
    
    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
    }
    
    protected override void DoSteering(float delta)
    {
        Vector3 targetPos = _target.GlobalPosition.To3D() + _target.Transform.basis.Xform(_targetOffset.To3D());
        Vector2 steering = Vector2.Zero;

        if ((_behaviours & (int) SteeringBehaviours.Arrive) != 0)
        {
            steering += _SteeringArrive(targetPos.To2D(), SlowingRadius);
        }
        if ((_behaviours & (int)SteeringBehaviours.Pursuit) != 0)
        {
            steering += _SteeringPursuit(targetPos.To2D(), _target.Velocity.To2D());
        }
        if ((_behaviours & (int) SteeringBehaviours.Separation) != 0)
        {
            steering += _SteeringSeparation(_game.AllBoids, _game.BaseBoidGrouping * 0.66f);
        }
        if ((_behaviours & (int) SteeringBehaviours.EdgeRepulsion) != 0)
        {
            steering += _SteeringEdgeRepulsion(_game.PlayRadius) * 2.0f;
        }

        // limit angular velocity
        if (_velocity.LengthSquared() > 0)
        {
            Vector2 linearComp = _velocity.To2D().Normalized() * steering.Length() * steering.Normalized().Dot(_velocity.To2D().Normalized());
            Vector2 tangent = new Vector2(_velocity.z, -_velocity.x);
            Vector2 angularComp = tangent.Normalized() * steering.Length() * steering.Normalized().Dot(tangent.Normalized());
            steering = linearComp + angularComp.Normalized() * Mathf.Clamp(angularComp.Length(), 0.0f, MaxAngularVelocity);
        }

        steering = steering.Truncate(MaxVelocity);
        _velocity += steering.To3D() * delta;
        _velocity = _velocity.Truncate(MaxVelocity);
    }
    
    protected override void _Shoot(Vector2 dir)
    {
        base._Shoot(dir);
        
        _shootCooldown = _game.BaseBoidReload;
        Bullet bullet = _bulletScene.Instance() as Bullet;
        float spread = _game.BaseBoidSpread;
        dir += new Vector2(-dir.y, dir.x) * (float) GD.RandRange(-spread, spread);
        bullet.Init(dir * _game.BaseBulletSpeed, Alignment, _game.PlayRadius, _game.BaseBoidDamage);
        _game.AddChild(bullet);
        bullet.GlobalPosition = GlobalPosition;
        _game.PushBack(this);
    }
}
