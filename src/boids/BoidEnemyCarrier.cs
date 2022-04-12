using System.Collections.Generic;
using System.Linq;
using Godot;

public class BoidEnemyCarrier : BoidEnemyBase
{
    [Export] private PackedScene BulletScene;
    [Export] PackedScene DroneScene;

    [Export] public float TargetDist = 400.0f;
    [Export] public float DronePulseCooldown = 2.0f;
    [Export] public float DroneSpawnInterval = 0.33f;
    [Export] public int DronePulseCount = 10;
    [Export] public float DroneSpawnRange = 750.0f;

    public AudioStreamPlayer _sfxBeaconFire;
    public AudioStreamPlayer _sfxDestroy;
    public AudioStream _rocochetSfx;

    private List<CarrierRotorgun> _rotorguns = new List<CarrierRotorgun>();
    public float _beaconCooldown;
    public float _beaconCharge;
    public float _beaconDuration;
    public int _pulses;
    private bool _firstFrame = true;
    public float _droneSpawnTimer;
    private bool _spawningDrones = false;
    public float _dronePulseTimer;
    public int _dronePulseSpawned;
    public int _droneSpawnSide;


    public override void _Ready()
    {
        _sfxBeaconFire = GetNode("SFXBeaconFire") as AudioStreamPlayer;
        _sfxDestroy = GetNode("SFXDestroy") as AudioStreamPlayer;
        _rocochetSfx = GD.Load("res://assets/sfx/ricochet.wav") as AudioStream;

        _sprite.Modulate = ColourManager.Instance.Secondary;

        for (int i = 0; i < 4; i++)
        {
            _rotorguns.Add(GetNode($"Rotorgun{i}") as CarrierRotorgun);
            _rotorguns[_rotorguns.Count - 1].Lock = GetNode($"Lock{i}") as Node2D;
            _rotorguns[i].Init(_game, _target);
        }
    }

    public override void _Process(float delta)
    {
        Rotation = -Mathf.Atan2(_velocity.x, _velocity.y);
        if (_firstFrame)
        {
            _firstFrame = false;
            _sfxHit.Stream = _rocochetSfx;
        }

        int count = 0;
        foreach (var r in _rotorguns)
        {
            if (IsInstanceValid(r) && !r.IsDestroyed())
            {
                count += 1;
            }
        }

        if (count == 0 && !IsDestroyed())
        {
            Destroy(true);
        }

        var dist = (_target.GlobalPosition - GlobalPosition).Length();
        if (dist < TargetDist)
        {
            _move = false;
        }
        else
        {
            _move = true;

            // drone spawn
        }

        if (!IsDestroyed())
        {
            _dronePulseTimer -= delta;
            if (_dronePulseTimer < 0.0 && dist < DroneSpawnRange && !_spawningDrones)
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
                    _droneSpawnTimer = DroneSpawnInterval;
                    _dronePulseSpawned += 1;
                }

                if (_dronePulseSpawned >= DronePulseCount)
                {
                    _spawningDrones = false;
                    _dronePulseTimer = DronePulseCooldown;
                }
            }
        }
    }

    public void _SpawnDrone()
    {
        Vector2 spawnPos;
        BoidEnemyBase enemy = DroneScene.Instance() as BoidEnemyBase;
        _droneSpawnSide = (_droneSpawnSide + 1) % 2;
        if (_droneSpawnSide == 0)
        {
            enemy.GlobalPosition = (GetNode("SpawnLeft") as Node2D).GlobalPosition;
        }
        else
        {
            enemy.GlobalPosition = (GetNode("SpawnRight") as Node2D).GlobalPosition;
        }

        enemy.Init(_game, _target);
        _game.AddChild(enemy);
        _game.Enemies.Append(enemy);
        enemy._velocity = enemy.MaxVelocity * (enemy.GlobalPosition - GlobalPosition).Normalized();
    }

    public override void OnHit(float damage, bool score, Vector2 bulletVel, bool microbullet, Vector2 pos)
    {
        _sfxHit.Play();
    }

    protected override void Destroy(bool score)
    {
        base.Destroy(score);
        
        _sfxDestroy.Play();
    }
}