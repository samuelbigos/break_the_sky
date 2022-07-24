using System;
using System.Collections.Generic;
using Godot;
using GodotOnReady.Attributes;

public partial class BoidEnemyCarrier : BoidEnemyBase
{
    [Export] private List<NodePath> _turretPaths;

    [Export] private float _dronePulseCooldown = 2.0f;
    [Export] private float _droneSpawnInterval = 0.33f;
    [Export] private int _dronePulseCount = 10;
    [Export] private float _droneSpawnRange = 750.0f;
    [Export] private float GunTrackSpeed = 2.0f;

    private List<Turret> _turrets = new List<Turret>();
    private List<Spatial> _turretBarrels = new List<Spatial>();
    private float _beaconCooldown;
    private float _beaconCharge;
    private float _beaconDuration;
    private int _pulses;
    private bool _firstFrame = true;
    private float _droneSpawnTimer;
    private bool _spawningDrones = false;
    private float _dronePulseTimer;
    private int _dronePulseSpawned;
    private int _droneSpawnSide;

    [OnReady] private void Ready()
    {
        base._Ready();

        for (int i = 0; i < _turretPaths.Count; i++)
        {
            _turrets.Add(GetNode<Turret>(_turretPaths[i]));
            _turretBarrels.Add(_turrets[i].GetChild<Spatial>(0));
            _turrets[i].Owner = this;
            _turrets[i].ShootCooldown = _resourceStats.AttackCooldown;
            _turrets[i].ShootVelocity = _resourceStats.AttackVelocity;
            _turrets[i].ShootDamage = _resourceStats.AttackDamage;
            _turrets[i].Alignment = Alignment;
        }
    }
    
    public override void Init(ResourceBoid data, Action<BoidBase> onDestroy, Vector2 position, Vector2 velocity)
    {
        base.Init(data, onDestroy, position, velocity);
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetBoid(_steeringId);
        steeringBoid.DesiredDistFromTargetMin = EngageRange - EngageRange * 0.5f;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        float dist = (TargetPos - GlobalPosition).Length();
        if (!Destroyed)
        {
            _dronePulseTimer -= delta;
            if (_dronePulseTimer < 0.0 && dist < _droneSpawnRange && !_spawningDrones)
            {
                _spawningDrones = true;
                _dronePulseSpawned = 0;
            }

            if (_spawningDrones)
            {
                _droneSpawnTimer -= delta;
                if (_droneSpawnTimer < 0.0)
                {
                    _SpawnDrone();
                    _droneSpawnTimer = _droneSpawnInterval;
                    _dronePulseSpawned += 1;
                }

                if (_dronePulseSpawned >= _dronePulseCount)
                {
                    _spawningDrones = false;
                    _dronePulseTimer = _dronePulseCooldown;
                }
            }
        }
    }

    private void _SpawnDrone()
    {
        _droneSpawnSide = (_droneSpawnSide + 1) % 2;
        Vector2 pos;
        if (_droneSpawnSide == 0)
        {
            pos = (GetNode("SpawnLeft") as Spatial).GlobalTransform.origin.To2D();
        }
        else
        {
            pos = (GetNode("SpawnRight") as Spatial).GlobalTransform.origin.To2D();
        }
        Vector2 vel = 100.0f * (pos - GlobalPosition).Normalized();
        // BoidEnemyBase enemy = BoidFactory.Instance.CreateEnemyBoid(_rotorgunData, pos, vel);
        // enemy.OnBoidDestroyed += _OnRotorgunDestroyed;
        // enemy.SetTarget(TargetType.Enemy, Game.Player);
    }
    
    protected override void OnEnterAIState_Seeking()
    {
        base.OnEnterAIState_Seeking();
        
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, true);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainDistance, false);
    }

    protected override void OnEnterAIState_Engaged()
    {
        base.OnEnterAIState_Engaged();
        
        ResetSteeringBehaviours();
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainDistance, true);

        for (int i = 0; i < _turretPaths.Count; i++)
        {
            _turrets[i].Target = _targetBoid;
        }
    }
}