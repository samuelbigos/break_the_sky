using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class BoidEnemyLancer : BoidEnemyBase
{
    [Export] private PackedScene _bulletScene;

    [Export] private MeshInstance3D _antiGravMesh;
    [Export] private Node3D _weaponPosition1; 
    [Export] private Node3D _weaponPosition2;
    
    private double _attackCooldownTimer;
    private int _shotsFired;
    
    protected override void ProcessAlive(double delta)
    {
        if (_aiState == AIState.Engaged)
        {
            _attackCooldownTimer -= delta;
            if (_attackCooldownTimer < 0.0f)
            {
                SwitchAiState(AIState.Seeking);
            }
        }
        
        base.ProcessAlive(delta);
    }

    private void Shoot()
    {
        Bullet bullet = _bulletScene.Instantiate() as Bullet;
        DebugUtils.Assert(bullet != null, nameof(bullet) + " != null");
        
        Vector3 spawnPos = _weaponPosition1.GlobalTransform.Origin;
        if (_shotsFired % 2 == 0)
            spawnPos = _weaponPosition2.GlobalTransform.Origin;
        
        Game.Instance.AddChild(bullet);
        bullet.Init(spawnPos, _targetBoid, false, _resourceStats.AttackVelocity, _resourceStats.AttackDamage, Alignment);
    }

    protected override void _OnDestroy(Vector2 hitDir, float hitStrength)
    {
        base._OnDestroy(hitDir, hitStrength);
        
        _antiGravMesh.QueueFree();
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