using System.Collections.Generic;
using Godot;

public class BoidEnemyCarrier : BoidEnemyBase
{
    [Export] private List<NodePath> _rotorgunsPaths;
    [Export] private List<NodePath> _lockPaths;

    [Export] private string _droneId;

    [Export] private float _targetDist = 400.0f;
    [Export] private float _dronePulseCooldown = 2.0f;
    [Export] private float _droneSpawnInterval = 0.33f;
    [Export] private int _dronePulseCount = 10;
    [Export] private float _droneSpawnRange = 750.0f;

    private AudioStreamPlayer3D _sfxBeaconFire;
    private AudioStream _rocochetSfx;

    private List<BoidEnemyCarrierRotorgun> _rotorguns = new List<BoidEnemyCarrierRotorgun>();
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

    public override void _Ready()
    {
        base._Ready();
        
        _sfxBeaconFire = GetNode("SFXBeaconFire") as AudioStreamPlayer3D;
        _rocochetSfx = GD.Load("res://assets/sfx/ricochet.wav") as AudioStream;

        for (int i = 0; i < _rotorgunsPaths.Count; i++)
        {
            _rotorguns.Add(GetNode<BoidEnemyCarrierRotorgun>(_rotorgunsPaths[i]));
            _rotorguns[i].Init("rotorgun", _player, _game, _target);
            _rotorguns[i].InitRotorgun(GetNode<Spatial>(_lockPaths[i]), this);
        }
        
        _sfxHitPlayer.Stream = _rocochetSfx;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        int count = 0;
        foreach (BoidEnemyCarrierRotorgun r in _rotorguns)
        {
            if (IsInstanceValid(r) && !r.Destroyed)
            {
                count += 1;
            }
        }

        if (count == 0 && !Destroyed)
        {
            _Destroy(true);
        }

        float dist = (_target.GlobalPosition - GlobalPosition).Length();
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
        DataEnemyBoid enemyData = Database.EnemyBoids.FindEntry<DataEnemyBoid>(_droneId);
        BoidEnemyBase enemy = enemyData.Scene.Instance<BoidEnemyBase>();
        _droneSpawnSide = (_droneSpawnSide + 1) % 2;
        if (_droneSpawnSide == 0)
        {
            enemy.GlobalPosition = (GetNode("SpawnLeft") as Spatial).GlobalTransform.origin.To2D();
        }
        else
        {
            enemy.GlobalPosition = (GetNode("SpawnRight") as Spatial).GlobalTransform.origin.To2D();
        }

        enemy.Init(null, _player, _game, _target);
        _game.AddChild(enemy);
        _game.AddEnemy(enemy);
        enemy.Velocity = enemy.MaxVelocity * (enemy.GlobalPosition - GlobalPosition).Normalized().To3D();
    }
}