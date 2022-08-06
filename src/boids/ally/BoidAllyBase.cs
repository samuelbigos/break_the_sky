using System;
using System.Collections.Generic;
using Godot;
using GodotOnReady.Attributes;

public partial class BoidAllyBase : BoidBase
{
    public enum AIState
    {
        None,
        Idle,
        Stationed,
        Engaged
    }
    
    [Export] private float _destroyTime = 3.0f;
    [Export] private float _shootSize = 1.5f;
    [Export] private float _shootTrauma = 0.05f;
    [Export] private float _destroyTrauma = 0.1f;
    [Export] private PackedScene _microBulletScene;
    [Export] protected float _engageRange;
    
    [OnReadyGet] private AudioStreamPlayer2D _sfxShootMicro;

    public ResourceBoidAlly Data => _data as ResourceBoidAlly;
    public AIState AiState => _aiState;
    public override BoidAlignment Alignment => BoidAlignment.Ally;

    protected AIState _aiState = AIState.None;
    protected BoidBase _engageTarget;

    private float _microTurretSearchTimer;
    private float _microTurretCooldown;
    private BoidEnemyBase _microTurretTarget;

    [OnReady] private void Ready()
    {
        _baseScale = _mesh.Scale;

        SpatialMaterial mat = _selectedIndicator.GetActiveMaterial(0) as SpatialMaterial;
        mat.AlbedoColor = ColourManager.Instance.Ally;
        
        _mesh.AltShaders[0].SetShaderParam("u_outline_colour", ColourManager.Instance.AllyOutline);
    }

    protected override void ProcessAlive(float delta)
    {
        switch (_aiState)
        {
            case AIState.None:
                SwitchAiState(AIState.Idle);
                break;
            case AIState.Stationed:
                AcquireTarget();
                break;
            case AIState.Idle:
                AcquireTarget();
                break;
            case AIState.Engaged:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ProcessMicroturrets(delta);
        
        base.ProcessAlive(delta);
    }

    private void AcquireTarget()
    {
        BoidEnemyBase target = null;
        foreach (BoidEnemyBase enemy in BoidFactory.Instance.EnemyBoids)
        {
            float distSq = (enemy.GlobalPosition - GlobalPosition).LengthSquared();
            if (distSq > Mathf.Pow(_engageRange, 2.0f))
                continue;
            
            target = enemy;
            
            if (enemy.IsTargetted) // prioritise enemies already targetted lower to avoid overkill
                continue;

            target = enemy;
        }
        
        if (!target.Null())
        {
            target.IsTargetted = true;
            SetTarget(TargetType.Enemy, target);
            SwitchAiState(AIState.Engaged);
        }
    }

    private void ProcessMicroturrets(float delta)
    {
        if (_resourceStats.MicroTurrets)
        {
            _microTurretSearchTimer -= delta;
            if (_microTurretTarget.Null() && _microTurretSearchTimer < 0.0f)
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
                if (!_microTurretTarget.Null())
                {
                    _microTurretTarget.OnBoidDestroyed += _OnMicroTurretTargetDestroyed;
                }
            }

            _microTurretCooldown -= delta;
            if (!_microTurretTarget.Null() && _microTurretCooldown < 0.0f)
            {
                _microTurretCooldown = _resourceStats.MicroTurretCooldown;
                Vector2 toTarget = _microTurretTarget.GlobalPosition - GlobalPosition;
                Vector2 dir = toTarget.Normalized();
                if (toTarget.LengthSquared() > _resourceStats.MicroTurretRange * _resourceStats.MicroTurretRange)
                {
                    _microTurretTarget.OnBoidDestroyed -= _OnMicroTurretTargetDestroyed;
                    _microTurretTarget = null;
                }
                else
                {
                    Bullet bullet = _microBulletScene.Instance() as Bullet; // TODO: use bullet pool
                    Game.Instance.AddChild(bullet);
                    bullet.Init(GlobalTransform.origin, dir * _resourceStats.AttackVelocity, Alignment, _resourceStats.MicroTurretDamage);
                }
            }
        }
    }
    
    public void NavigateTowards(Vector2 pos)
    {
        SetTarget(TargetType.Position, null, pos);
        SwitchAiState(AIState.Stationed);
    }

    public void ReturnToPlayer()
    {
        SwitchAiState(AIState.Idle);
    }

    private void SwitchAiState(AIState state)
    {
        if (_aiState == state)
            return;
        
        switch (state)
        {
            case AIState.Idle:
                _OnEnterAIState_Idle();
                break;
            case AIState.Stationed:
                _OnEnterAIState_Stationed();
                break;
            case AIState.Engaged:
                _OnEnterAIState_Engaged();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        _aiState = state;
    }

    protected virtual void _OnEnterAIState_Idle()
    {
        if (!_engageTarget.Null())
        {
            _engageTarget.OnBoidDestroyed -= _OnEngageTargetBoidDestroyed;
            _engageTarget = null;
        }
        ResetSteeringBehaviours();
        SetTarget(TargetType.Ally, Game.Player);
    }
    
    protected virtual void _OnEnterAIState_Stationed()
    {
        if (!_engageTarget.Null())
        {
            _engageTarget.OnBoidDestroyed -= _OnEngageTargetBoidDestroyed;
            _engageTarget = null;
        }
        ResetSteeringBehaviours();
    }
    
    protected virtual void _OnEnterAIState_Engaged()
    {
        _engageTarget = _targetBoid;
        _engageTarget.OnBoidDestroyed += _OnEngageTargetBoidDestroyed;
    }

    private void _OnEngageTargetBoidDestroyed(BoidBase boid)
    {
        SwitchAiState(AIState.Idle);
    }

    protected override void _OnDestroy(Vector2 hitDir, float hitStrength)
    {
        SwitchAiState(AIState.Idle);
        
        base._OnDestroy(hitDir, hitStrength);
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

    private void _OnMicroTurretTargetDestroyed(BoidBase boid)
    {
        boid.OnBoidDestroyed -= _OnMicroTurretTargetDestroyed;
        _microTurretTarget = null;
    }
}