using System;
using System.Collections.Generic;
using Godot;

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
    
    [Export] private AudioStreamPlayer2D _sfxShootMicro;

    public ResourceBoidAlly Data => _data as ResourceBoidAlly;
    public AIState AiState => _aiState;
    public override BoidAlignment Alignment => BoidAlignment.Ally;

    protected bool _isPlayer;
    protected AIState _aiState = AIState.None;
    protected BoidBase _engageTarget;

    private double _microTurretSearchTimer;
    private double _microTurretCooldown;
    private BoidEnemyBase _microTurretTarget;

    public override void _Ready()
    {
        base._Ready();
        
        StandardMaterial3D mat = _selectedIndicator.GetActiveMaterial(0) as StandardMaterial3D;
        mat.AlbedoColor = ColourManager.Instance.Ally;
        
        _mesh.AltShaders[0].SetShaderParameter("u_outline_colour", ColourManager.Instance.AllyOutline);
    }

    public void SetIsPlayer(bool isPlayer)
    {
        _isPlayer = isPlayer;
    }

    protected override void ProcessAlive(double delta)
    {
#if !FINAL
        // Hack-fix this because I don't know why it's happening :(
        if (_targetBoid.Null() || _engageTarget.Null() ||
            !SteeringManager.Instance.HasObject<SteeringManager.Boid>(SteeringBoid.TargetIndex))
        {
            SwitchAiState(AIState.Idle);
        }
#endif

        if (_isPlayer)
        {
            DoPlayerInput();
        }
        else
        {
            DoAIInput();
        }

        ProcessMicroturrets(delta);
        
        base.ProcessAlive(delta);
    }

    private void DoPlayerInput()
    {
        if (_acceptInput)
        {
            Vector2 forward = new(0.0f, -1.0f);
            Vector2 left = new(-1.0f, 0.0f);

            Vector2 dir = new(0.0f, 0.0f);
            if (Input.IsActionPressed("w")) dir += forward;
            if (Input.IsActionPressed("s")) dir += -forward;
            if (Input.IsActionPressed("a")) dir += left;
            if (Input.IsActionPressed("d")) dir += -left;

            ref SteeringManager.Boid boid = ref SteeringBoid;
            
            float boost = Input.IsActionPressed("boost") ? 2.0f : 1.0f;
            boid.MaxSpeed = MaxVelocity * boost;
            boid.MaxForce = MaxForce * boost;
            
            if (dir != new Vector2(0.0f, 0.0f))
            {
                dir = dir.Normalized();
                boid.DesiredVelocityOverride = dir.ToNumerics() * 5000.0f;
                SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Stop, false);
                SetSteeringBehaviourEnabled(SteeringManager.Behaviours.DesiredVelocityOverride, true);
            }
            else
            {
                boid.DesiredVelocityOverride = System.Numerics.Vector2.Zero;
                SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Stop, true);
                SetSteeringBehaviourEnabled(SteeringManager.Behaviours.DesiredVelocityOverride, false);
            }
        }
    }

    private void DoAIInput()
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
            EngageEnemy(target);
        }
    }

    private void ProcessMicroturrets(double delta)
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
                if (toTarget.LengthSquared() > _resourceStats.MicroTurretRange * _resourceStats.MicroTurretRange)
                {
                    _microTurretTarget.OnBoidDestroyed -= _OnMicroTurretTargetDestroyed;
                    _microTurretTarget = null;
                }
                else
                {
                    Bullet bullet = _microBulletScene.Instantiate() as Bullet; // TODO: use bullet pool
                    Game.Instance.AddChild(bullet);
                    bullet.Init(GlobalTransform.Origin, _microTurretTarget, true, _resourceStats.MicroTurretVelocity, _resourceStats.MicroTurretDamage, Alignment);
                }
            }
        }
    }
    
    public void RegisterPickup(PickupMaterial pickup)
    {
        pickup.OnCollected += _OnPickupCollected;
    }
    
    public void NavigateTowards(Vector2 pos)
    {
        SetTarget(TargetType.Position, null, pos);
        SwitchAiState(AIState.Stationed);
    }

    public void EngageEnemy(BoidEnemyBase enemy)
    {
        SetTarget(TargetType.Enemy, enemy);
        SwitchAiState(AIState.Engaged);
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

    protected override void ResetSteeringBehaviours()
    {
        base.ResetSteeringBehaviours();

        if (_isPlayer)
        {
            ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
            steeringBoid.Behaviours = 0;
        }
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
    
    private void _OnPickupCollected(PickupMaterial pickup)
    {
        SaveDataPlayer.MaterialCount += 1;
        AudioManager.Instance.SFXPickup.Play();
    }
}