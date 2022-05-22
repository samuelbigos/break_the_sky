using Godot;
using System;
using System.Diagnostics;

public class BoidAllyBomber : BoidAllyBase
{
    [Export] private PackedScene _bulletScene;
    [Export] private float _targetAcquireRadius = 100.0f;
    [Export] private float _resupplyRadius = 100.0f;
    [Export] private float _shootRange = 50.0f;
    [Export] private float _shootTargetAlignment = 0.8f;
    [Export] private float _shootCooldown = 2.0f;
    [Export] private float _fleeTime = 0.5f;

    private bool _canShoot = false;
    private float _shootCooldownTimer;
    private float _fleeTimer;
    
    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(float delta)
    {
        if (!IsInstanceValid(_target) || _target.Destroyed)
            _target = _player;
        
        base._Process(delta);

        // targeting
        if (_canShoot && _target == _player)
        {
            AcquireTarget();
        }
        
        if (_target is BoidEnemyBase)
        {
            // disengage
            float dist = (_target.GlobalPosition - GlobalPosition).LengthSquared();
            if (dist > Mathf.Pow(_targetAcquireRadius, 2.0f))
            {
                ((BoidEnemyBase) _target).IsTargetted = false;
                _target = _player;
            }
            
            // shooting
            float dot = _velocity.To2D().Normalized().Dot((_target.GlobalPosition - GlobalPosition).Normalized());
            if (_canShoot && dist < Mathf.Pow(_shootRange, 2.0f) && dot > _shootTargetAlignment)
            {
                _Shoot((_target.GlobalPosition - GlobalPosition).Normalized());
                
                SetSteeringBehaviourEnabled(SteeringBehaviours.Flee, true);
                SetSteeringBehaviourEnabled(SteeringBehaviours.Pursuit, false);
                _fleeTimer = _fleeTime;
            }
        }

        // resupply
        _shootCooldownTimer -= delta;
        if (_target == _player)
        {
            float dist = (_target.GlobalPosition - GlobalPosition).LengthSquared();
            if (!_canShoot && _shootCooldownTimer < 0.0f && dist < Mathf.Pow(_resupplyRadius, 2.0f))
            {
                _canShoot = true;
            }
        }
        
        _fleeTimer -= delta;
        if (!_canShoot && _fleeTimer <= 0.0f || _target == _player)
        {
            SetSteeringBehaviourEnabled(SteeringBehaviours.Flee, false);
            SetSteeringBehaviourEnabled(SteeringBehaviours.Pursuit, true);
            _target = _player;
        }
    }

    private void AcquireTarget()
    {
        BoidEnemyBase target = null;
        foreach (BoidEnemyBase enemy in _game.EnemyBoids)
        {
            if ((enemy.GlobalPosition - GlobalPosition).LengthSquared() > Mathf.Pow(_targetAcquireRadius, 2.0f))
                continue;
            
            target = enemy;
            
            if (enemy.IsTargetted) // prioritise enemies already targetted lower to avoid overkill
                continue;

            target = enemy;
        }
        
        if (target != null)
        {
            target.IsTargetted = true;
            _target = target;
        }
    }

    protected override void _Shoot(Vector2 dir)
    {
        base._Shoot(dir);
        
        BulletBomber bullet = _bulletScene.Instance() as BulletBomber;
        Debug.Assert(bullet != null);
        _game.AddChild(bullet);
        bullet.Init(GlobalPosition, dir * _game.BaseBulletSpeed, Alignment, _game.BaseBoidDamage);
        bullet.Target = _target;
        
        _canShoot = false;
        _shootCooldownTimer = _shootCooldown;
    }
}
