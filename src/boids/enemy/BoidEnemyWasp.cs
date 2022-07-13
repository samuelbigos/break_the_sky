using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public class BoidEnemyWasp : BoidEnemyBase
{
    [Export] private PackedScene _bulletScene;

    private float _attackCooldownTimer;
    private float _shotCooldownTimer;
    private int _shotsFired;
    
    protected override void ProcessAlive(float delta)
    {
        if (_aiState == AIState.Engaged)
        {
            _cachedHeading = (_targetBoid.GlobalPosition - GlobalPosition).Normalized();

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
        
        base.ProcessAlive(delta);
    }

    private void Shoot()
    {
        SeekerMissile bullet = _bulletScene.Instance() as SeekerMissile;
        Debug.Assert(bullet != null, nameof(bullet) + " != null");
        
        Vector2 spawnPos = GlobalPosition;
        Game.Instance.AddChild(bullet);
        bullet.Init(spawnPos, Alignment, _targetBoid);
    }

    protected override void EnterAIState_Engaged()
    {
        base.EnterAIState_Engaged();
        
        ResetSteeringBehaviours();
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetBoid(_steeringId);
        steeringBoid.DesiredOffsetFromTarget = (GlobalPosition - _targetBoid.GlobalPosition).Normalized().ToNumerics() * EngageRange;
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainOffset, true);
        
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Wander, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Cohesion, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Alignment, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.FlowFieldFollow, false);

        _attackCooldownTimer = 1.0f;
    }
}