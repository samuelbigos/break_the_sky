using Godot;
using System;
using System.Diagnostics;
using Vector3 = System.Numerics.Vector3;

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
    private BulletBomber _bomb;
    
    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
    }

    protected override void ProcessAlive(float delta)
    {
        // targeting
        if (_canShoot && _targetType == TargetType.Ally)
        {
            AcquireTarget();
        }
        
        if (_targetType == TargetType.Enemy)
        {
            // disengage
            float dist = (TargetPos - GlobalPosition).LengthSquared();
            if (dist > Mathf.Pow(_targetAcquireRadius, 2.0f))
            {
                ((BoidEnemyBase) _targetBoid).IsTargetted = false;
                SetTarget(TargetType.Ally, Game.Player);
            }
            
            // shooting
            float dot = _cachedVelocity.ToGodot().Normalized().Dot((TargetPos - GlobalPosition).Normalized());
            if (_canShoot && dist < Mathf.Pow(_shootRange, 2.0f) && dot > _shootTargetAlignment)
            {
                _Shoot((TargetPos - GlobalPosition).Normalized());
                
                SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Flee, true);
                SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, false);
                _fleeTimer = _fleeTime;
            }
        }

        // resupply
        _shootCooldownTimer -= delta;
        if (_targetType == TargetType.Ally)
        {
            float dist = (TargetPos - GlobalPosition).LengthSquared();
            if (!_canShoot && _shootCooldownTimer < 0.0f && dist < Mathf.Pow(_resupplyRadius, 2.0f))
            {
                Resupply();
            }
        }
        
        _fleeTimer -= delta;
        if (!_canShoot && _fleeTimer <= 0.0f || _targetType == TargetType.Ally)
        {
            SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Flee, false);
            SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, true);
            SetTarget(TargetType.Ally, Game.Player);
        }
        
        base.ProcessAlive(delta);
    }

    private void Resupply()
    {
        _canShoot = true;
        _bomb = _bulletScene.Instance() as BulletBomber;
        AddChild(_bomb);
        _bomb.Init((GlobalTransform.origin - GlobalTransform.basis.z * 2.1f).To2D(), Vector2.Zero, BoidAlignment.Ally, 0.0f);
    }

    private void AcquireTarget()
    {
        BoidEnemyBase target = null;
        foreach (BoidEnemyBase enemy in BoidFactory.Instance.EnemyBoids)
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
            SetTarget(TargetType.Enemy, target);
        }
    }

    protected override void _Shoot(Vector2 dir)
    {
        base._Shoot(dir);
        
        Debug.Assert(_bomb != null);
        Vector2 pos = _bomb.GlobalPosition;
        RemoveChild(_bomb);
        Game.Instance.AddChild(_bomb);
        _bomb.Init(pos, dir * _stats.AttackVelocity, Alignment, _stats.AttackDamage);
        _bomb.Target = _targetBoid;
        _bomb = null;
        
        _canShoot = false;
        _shootCooldownTimer = _shootCooldown;
    }
}
