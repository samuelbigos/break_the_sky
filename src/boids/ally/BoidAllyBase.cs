using System;
using System.Collections.Generic;
using Godot;

public class BoidAllyBase : BoidBase
{
    [Export] private float _destroyTime = 3.0f;
    [Export] private float _shootSize = 1.5f;
    [Export] private float _shootTrauma = 0.05f;
    [Export] private float _destroyTrauma = 0.1f;
    
    [Export] public bool BlocksShots = true;

    [Export] private PackedScene _microBulletScene;
    
    [Export] private NodePath _sfxHitMicroPlayerNode;
    [Export] private NodePath _sfxShootPlayerPath;
    private AudioStreamPlayer2D _sfxShootPlayer;
    
    public ResourceBoidEnemy Data => _data as ResourceBoidEnemy;

    protected override BoidAlignment Alignment => BoidAlignment.Ally;

    private float _microTurretSearchTimer;
    private float _microTurretCooldown;
    private BoidEnemyBase _microTurretTarget;

    public override void _Ready()
    {
        base._Ready();
        
        _baseScale = _mesh.Scale;

        _sfxShootPlayer = GetNode<AudioStreamPlayer2D>(_sfxShootPlayerPath);

        SpatialMaterial mat = _selectedIndicator.GetActiveMaterial(0) as SpatialMaterial;
        mat.AlbedoColor = ColourManager.Instance.Ally;
    }

    protected override void ProcessAlive(float delta)
    {
        if (_targetType == TargetType.None)
        {
            SetTarget(TargetType.Ally, Game.Player);
        }
        
        // microturrets
        if (_resourceStats.MicroTurrets)
        {
            _microTurretSearchTimer -= delta;
            if (_microTurretTarget == null && _microTurretSearchTimer < 0.0f)
            {
                // TODO: use quadtree
                float closest = _resourceStats.MicroTurretRange * _resourceStats.MicroTurretRange;
                foreach (BoidEnemyBase enemy in BoidFactory.Instance.EnemyBoids)
                {
                    float dist = (enemy.GlobalPosition - GlobalPosition).LengthSquared();
                    if (dist < closest)
                    {
                        closest = dist;
                        _microTurretTarget = enemy;
                    }
                }

                _microTurretSearchTimer = Utils.Rng.Randf() * 0.1f + 0.1f; // random so all allies aren't synced.
                if (_microTurretTarget != null)
                {
                    _microTurretTarget.OnBoidDestroyed += _OnMicroTurretTargetDestroyed;
                }
            }

            _microTurretCooldown -= delta;
            if (_microTurretTarget != null && _microTurretCooldown < 0.0f)
            {
                _microTurretCooldown = _resourceStats.MicroTurretCooldown;
                Vector2 toTarget = _microTurretTarget.GlobalPosition - GlobalPosition;
                Vector2 dir = toTarget.Normalized();
                if (toTarget.LengthSquared() > _resourceStats.MicroTurretRange * _resourceStats.MicroTurretRange)
                {
                    _microTurretTarget.OnBoidDestroyed -= _OnMicroTurretTargetDestroyed;
                    _microTurretTarget = null;
                }
                else if (_CanShoot(dir))
                {
                    Bullet bullet = _microBulletScene.Instance() as Bullet;
                    Game.Instance.AddChild(bullet);
                    bullet.Init(GlobalPosition, dir * _resourceStats.AttackVelocity, Alignment, _resourceStats.MicroTurretDamage);
                }
            }
        }
        
        base.ProcessAlive(delta);
    }

    private void _OnMicroTurretTargetDestroyed(BoidBase boid)
    {
        boid.OnBoidDestroyed -= _OnMicroTurretTargetDestroyed;
        _microTurretTarget = null;
    }

    protected virtual void _Shoot(Vector2 dir)
    {
        float traumaMod = 1.0f - Mathf.Clamp(BoidFactory.Instance.NumBoids / 100.0f, 0.0f, 0.5f);
        GameCamera.Instance.AddTrauma(_shootTrauma * traumaMod);

        _sfxShootPlayer.Play();
    }

    protected virtual bool _CanShoot(Vector2 dir)
    {
        if (!_acceptInput)
            return false;
        
        // can shoot if there are no other boids in the shoot direction
        bool blocked = false;
        foreach (BoidAllyBase boid in BoidFactory.Instance.AllyBoids)
        {
            if (boid == this || boid.Destroyed || !boid.BlocksShots)
                continue;

            if ((boid.GlobalPosition - GlobalPosition).Normalized().Dot(dir.Normalized()) < 0.9f)
                continue;
            
            blocked = true;
            break;
        }

        return !blocked;
    }

    protected override void _OnGameStateChanged(StateMachine_Game.States state, StateMachine_Game.States prevState)
    {
        base._OnGameStateChanged(state, prevState);

        // if (state == StateMachine_Game.States.Play)
        // {
        //     _OnSkillsChanged(SaveDataPlayer.GetActiveSkills(Id));
        // }
    }

    protected override void _OnSkillsChanged(List<ResourceSkillNode> skillNodes)
    {
        bool microTurretsEnabled = _resourceStats.MicroTurrets;
        
        base._OnSkillsChanged(skillNodes);

        if (!microTurretsEnabled && _resourceStats.MicroTurrets)
        {
            _microTurretCooldown = Utils.Rng.Randf() * _resourceStats.MicroTurretCooldown;
        }
    }
}