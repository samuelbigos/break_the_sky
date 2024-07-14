using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class BoidEnemySentry : BoidEnemyBase
{
    [Export] private PackedScene _bulletScene;
    [Export] private bool _fleeOnAttacked;
    
    [Export] private Node3D _weaponPosition1; 
    [Export] private Node3D _weaponPosition2; 
    
    private double _attackCooldownTimer;
    private double _shotCooldownTimer;
    private int _shotsFired;
    
    protected override void ProcessAlive(double delta)
    {
        switch (_aiState)
        {
            case AIState.Engaged:
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

                break;
            }
            default:
            {
                _visualHeadingOverride = Vector2.Zero;
                break;
            }
        }

        base.ProcessAlive(delta);
    }

    protected override void _OnHit(float damage, Vector2 bulletVel, Vector2 pos)
    {
        base._OnHit(damage, bulletVel, pos);

        if (_fleeOnAttacked)
        {
            SwitchAiState(AIState.Flee);
        }
    }

    private void Shoot()
    {
        Bullet bullet = _bulletScene.Instantiate() as Bullet;
        DebugUtils.Assert(bullet != null, nameof(bullet) + " != null");
        Vector3 spawnPos = (_shotsFired % 2 == 0) ? _weaponPosition1.GlobalTransform.Origin : _weaponPosition2.GlobalTransform.Origin;
        Game.Instance.AddChild(bullet);
        bullet.Init(spawnPos, _targetBoid, false, _resourceStats.AttackVelocity, _resourceStats.AttackDamage, Alignment);
        _shotsFired++;
    }

    protected override void OnEnterAIState_Engaged()
    {
        base.OnEnterAIState_Engaged();
        
        ResetSteeringBehaviours();
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        steeringBoid.DesiredDistFromTargetMin = EngageRange * 0.8f;
        steeringBoid.DesiredDistFromTargetMax = EngageRange;
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainDistance, true);
        
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Wander, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Cohesion, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Alignment, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.FlowFieldFollow, false);
    }
}