using Godot;
using System;
using System.Diagnostics;
using Vector3 = System.Numerics.Vector3;

public partial class BoidAllyBomber : BoidAllyBase
{
    private enum BomberState
    {
        Bomb,
        Flee,
        Resupply
    }
    
    [Export] private PackedScene _bulletScene;
    [Export] private float _targetAcquireRadius = 100.0f;
    [Export] private float _resupplyRadius = 100.0f;
    [Export] private float _shootRange = 50.0f;
    [Export] private float _shootTargetAlignment = 0.8f;
    [Export] private float _fleeTime = 0.5f;
    
    [Export] private AudioStreamPlayer2D _sfxShoot;

    private BomberState _bomberState = BomberState.Bomb;
    private double _fleeTimer;
    private BulletBomber _bomb;

    public override void _Ready()
    {
        base._Ready();
        
        Resupply();
    }

    protected override void ProcessAlive(double delta)
    {
        switch (_aiState)
        {
            case AIState.Engaged:
            {
                ProcessBomber(delta);
                break;
            }
            case AIState.Idle:
            {
                break;
            }
        }
        
        // resupply
        if (_bomb.Null())
        {
            float dist = (Game.Player.GlobalPosition - GlobalPosition).LengthSquared();
            if (dist < Mathf.Pow(_resupplyRadius, 2.0f))
            {
                Resupply();
            }
        }

        base.ProcessAlive(delta);
    }

    private void ProcessBomber(double delta)
    {
        switch (_bomberState)
        {
            case BomberState.Bomb:
            {
                if (_bomb.Null())
                {
                    EnterResupplyState();
                    return;
                }
                
                float distSq = (_targetBoid.GlobalPosition - GlobalPosition).LengthSquared();
                float dot = _cachedVelocity.Normalized().Dot((TargetPos - GlobalPosition).Normalized());
                if (!_bomb.Null() && distSq < Mathf.Pow(_shootRange, 2.0f) && dot > _shootTargetAlignment)
                {
                    Shoot((TargetPos - GlobalPosition).Normalized());

                    SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Flee, true);
                    SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, false);
                    _fleeTimer = _fleeTime;
                    _bomberState = BomberState.Flee;
                }
                
                break;
            }
            case BomberState.Flee:
            {
                _fleeTimer -= delta;
                if (_fleeTimer <= 0.0f)
                {
                    SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Flee, false);
                    SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, true);
                    EnterResupplyState();
                }
                break;
            }
            case BomberState.Resupply:
            {
                if (!_bomb.Null())
                {
                    SetTarget(TargetType.Enemy, _engageTarget);
                    _bomberState = BomberState.Bomb;
                }
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void EnterResupplyState()
    {
        SetTarget(TargetType.Ally, Game.Player);
        _bomberState = BomberState.Resupply;
    }

    private void Resupply()
    {
        _bomb = _bulletScene.Instantiate<BulletBomber>();
        AddChild(_bomb);
        _bomb.Init((GlobalTransform.Origin - GlobalTransform.Basis.Z * 2.1f).To2D().To3D(), this, false, 0.0f, 0.0f, BoidAlignment.Ally);
        _bomb.Parent = this;
        _bomb.OnBulletDestroyed += OnBulletDestroyed;
    }

    private void OnBulletDestroyed(Bullet obj)
    {
        _bomb.OnBulletDestroyed -= OnBulletDestroyed;
        _bomb = null;
        
        if (_aiState == AIState.Engaged)
        {
            EnterResupplyState();
        }
    }

    private void Shoot(Vector2 dir)
    {
        Vector2 pos = _bomb.GlobalPosition;
        RemoveChild(_bomb);
        Game.Instance.AddChild(_bomb);
        _bomb.Init(pos.To3D(), _targetBoid, false, _resourceStats.AttackVelocity, _resourceStats.AttackDamage, Alignment);
        _bomb.OnBulletDestroyed -= OnBulletDestroyed;
        _bomb = null;
    }

    protected override void _OnEnterAIState_Engaged()
    {
        base._OnEnterAIState_Engaged();

        _engageTarget = _targetBoid;
        _bomberState = BomberState.Resupply;
    }
    
    protected override void _OnEnterAIState_Idle()
    {
        base._OnEnterAIState_Idle();

        _engageTarget = null;
    }
}
