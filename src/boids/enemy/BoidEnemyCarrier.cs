using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class BoidEnemyCarrier : BoidEnemyBase
{
    [Export] private Array<NodePath> _turretPaths;

    [Export] private float _dronePulseCooldown = 2.0f;
    [Export] private float _droneSpawnInterval = 1.0f;
    [Export] private int _dronePulseCount = 10;
    [Export] private float _droneSpawnRange = 750.0f;
    [Export] private float _gunTrackSpeed = 2.0f;
    [Export] private ResourceBoidEnemy _minion;
    [Export] private FlowFieldResource _minionFlowField;

    private List<Turret> _turrets = new List<Turret>();
    private List<Node3D> _turretBarrels = new List<Node3D>();
    private float _beaconCooldown;
    private float _beaconCharge;
    private float _beaconDuration;
    private int _pulses;
    private bool _firstFrame = true;
    private double _droneSpawnTimer;
    private bool _spawningDrones = false;
    private double _dronePulseTimer;
    private int _dronePulseSpawned;
    private int _droneSpawnSide;
    private int _flowFieldId;

    private List<BoidEnemyBase> _minions = new List<BoidEnemyBase>();

    public override void _Ready()
    {
        for (int i = 0; i < _turretPaths.Count; i++)
        {
            _turrets.Add(GetNode<Turret>(_turretPaths[i]));
            _turretBarrels.Add(_turrets[i].GetChild<Node3D>(0));
            _turrets[i].Owner = this;
            _turrets[i].ShootCooldown = _resourceStats.AttackCooldown;
            _turrets[i].ShootVelocity = _resourceStats.AttackVelocity;
            _turrets[i].ShootDamage = _resourceStats.AttackDamage;
            _turrets[i].BurstCooldown = _resourceStats.AttackCharge;
            _turrets[i].BurstDuration = _resourceStats.AttackDuration;
            _turrets[i].Alignment = Alignment;
        }
    }

    public override void Init(ResourceBoid data, Action<BoidBase> onDestroy, Vector2 position, Vector2 velocity)
    {
        base.Init(data, onDestroy, position, velocity);

        ref SteeringManager.Boid steeringBoid =
            ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        steeringBoid.DesiredDistFromTargetMin = EngageRange - EngageRange * 0.33f;

        // minion flowfield
        SteeringManager.FlowField flowField = new()
        {
            Resource = _minionFlowField,
            TrackID = steeringBoid.Id,
            Size = new System.Numerics.Vector2(500, 500),
        };
        
        if (!SteeringManager.Instance.Register(flowField, out _flowFieldId))
        {
            QueueFree();
            return;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        float dist = (TargetPos - GlobalPosition).Length();
        if (!Destroyed && _minions.Count < 5)
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

    protected override void _OnDestroy(Vector2 hitDir, float hitStrength)
    {
        base._OnDestroy(hitDir, hitStrength);

        SteeringManager.Instance.Unregister<SteeringManager.FlowField>(_flowFieldId);
    }

    private void _SpawnDrone()
    {
        _droneSpawnSide = (_droneSpawnSide + 1) % 2;
        Vector2 pos;
        if (_droneSpawnSide == 0)
        {
            pos = (GetNode("SpawnLeft") as Node3D).GlobalTransform.Origin.To2D();
        }
        else
        {
            pos = (GetNode("SpawnRight") as Node3D).GlobalTransform.Origin.To2D();
        }
        Vector2 vel = 25.0f * (pos - GlobalPosition).Normalized();
        BoidEnemyBase minion = BoidFactory.Instance.CreateEnemyBoid(_minion, pos, vel);
        minion.OnBoidDestroyed += _OnMinionDestroyed;
        _minions.Add(minion);
    }

    private void _OnMinionDestroyed(BoidBase minion)
    {
        _minions.Remove(minion as BoidEnemyBase);
    }

    protected override void OnEnterAIState_Engaged()
    {
        base.OnEnterAIState_Engaged();
        
        ResetSteeringBehaviours();
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainDistance, true);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainBroadside, true);
        
        for (int i = 0; i < _turretPaths.Count; i++)
        {
            _turrets[i].Target = _targetBoid;
        }
    }
}