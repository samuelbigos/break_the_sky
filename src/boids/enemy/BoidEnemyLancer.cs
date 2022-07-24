using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using GodotOnReady.Attributes;

public partial class BoidEnemyLancer : BoidEnemyBase
{
    [Export] private PackedScene _bulletScene;
    
    [OnReadyGet] private Spatial _weaponPosition1; 
    [OnReadyGet] private Spatial _weaponPosition2; 
    
    private float _attackCooldownTimer;
    private int _shotsFired;
    
    protected override void ProcessAlive(float delta)
    {
        if (_aiState == AIState.Engaged)
        {
            _attackCooldownTimer -= delta;
            if (_attackCooldownTimer < 0.0f)
            {
                EnterAIState(AIState.Seeking);
            }
        }
        
        base.ProcessAlive(delta);
    }

    private void Shoot()
    {
        Bullet bullet = _bulletScene.Instance() as Bullet;
        Debug.Assert(bullet != null, nameof(bullet) + " != null");
        
        Vector3 spawnPos = _weaponPosition1.GlobalTransform.origin;
        if (_shotsFired % 2 == 0)
            spawnPos = _weaponPosition2.GlobalTransform.origin;
        
        Vector2 spawnVel = (_targetBoid.GlobalPosition - GlobalPosition).Normalized() * _resourceStats.AttackVelocity;
        Game.Instance.AddChild(bullet);
        bullet.Init(spawnPos, spawnVel, Alignment, _resourceStats.AttackDamage);
    }

    protected override void OnEnterAIState_Seeking()
    {
        SetTarget(TargetType.Enemy, Game.Player);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, true);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Flee, false);
    }

    protected override void OnEnterAIState_Engaged()
    {
        base.OnEnterAIState_Engaged();
        
        ResetSteeringBehaviours();
        
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Flee, true);

        Shoot();

        _attackCooldownTimer = _resourceStats.AttackCooldown;
    }
}