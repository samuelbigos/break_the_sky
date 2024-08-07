using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class BoidEnemyWasp : BoidEnemyBase
{
    [Export] private PackedScene _bulletScene;
    
    [Export] private Node3D _weaponPosition; 

    private double _attackCooldownTimer;
    private double _shotCooldownTimer;
    private int _shotsFired;
    
    protected override void ProcessAlive(double delta)
    {
        if (_aiState == AIState.Engaged)
        {
            _visualHeadingOverride = (_targetBoid.GlobalPosition - GlobalPosition).Normalized();

            _attackCooldownTimer -= delta;
            if (_attackCooldownTimer < 0.0f)
            {
                _shotCooldownTimer -= delta;

                if (_shotCooldownTimer < 0.0f)
                {
                    Shoot();
                    _shotCooldownTimer = _resourceStats.AttackDuration / _resourceStats.AttackCount;
                    _shotsFired++;
                }

                if (_shotsFired >= _resourceStats.AttackCount)
                {
                    _shotsFired = 0;
                    _attackCooldownTimer = _resourceStats.AttackCooldown;
                }
            }
        }
        else
        {
            _visualHeadingOverride = Vector2.Zero;
        }
        
        base.ProcessAlive(delta);
    }

    private void Shoot()
    {
        SeekerMissile missile = _bulletScene.Instantiate() as SeekerMissile;
        DebugUtils.Assert(missile != null, nameof(missile) + " != null");
        Vector3 spawnPos = _weaponPosition.GlobalTransform.Origin;
        float angle = (_shotsFired - _resourceStats.AttackCount * 0.5f + 0.5f) * Mathf.Pi / 10.0f;
        Vector2 spawnVel = _cachedVelocity + (_targetBoid.GlobalPosition - GlobalPosition).Normalized().Rotated(angle) * 20.0f;
        Game.Instance.AddChild(missile);
        missile.Init(_resourceStats.AttackDamage, spawnPos, spawnVel, Alignment, _targetBoid);
    }

    protected override void OnEnterAIState_Engaged()
    {
        base.OnEnterAIState_Engaged();
        
        ResetSteeringBehaviours();
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        steeringBoid.DesiredOffsetFromTarget = (GlobalPosition - _targetBoid.GlobalPosition).Normalized().ToNumerics() * EngageRange;
        //SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainOffset, true);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainDistance, true);
        
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Wander, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Cohesion, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Alignment, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.FlowFieldFollow, false);

        _attackCooldownTimer = 1.0f;
    }
}