using Godot;
using System;
using System.Diagnostics;
using GodotOnReady.Attributes;
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
    
    [OnReadyGet] private AudioStreamPlayer2D _sfxShoot;

    private BomberState _bomberState = BomberState.Bomb;
    private float _fleeTimer;
    private BulletBomber _bomb;

    [OnReady] private void Ready()
    {
        Resupply();
    }

    protected override void ProcessAlive(float delta)
    {
        switch (_aiState)
        {
            case AIState.Engaged:
            {
                ProcessBomber(delta);
                break;
            }
            case AIState.Idle:
                break;
        }
        
        // resupply
        if (_bomb == null)
        {
            float dist = (Game.Player.GlobalPosition - GlobalPosition).LengthSquared();
            if (dist < Mathf.Pow(_resupplyRadius, 2.0f))
            {
                Resupply();
            }
        }

        base.ProcessAlive(delta);
    }

    private void ProcessBomber(float delta)
    {
        switch (_bomberState)
        {
            case BomberState.Bomb:
            {
                float distSq = (_targetBoid.GlobalPosition - GlobalPosition).LengthSquared();
                float dot = _cachedVelocity.Normalized().Dot((TargetPos - GlobalPosition).Normalized());
                if (distSq < Mathf.Pow(_shootRange, 2.0f) && dot > _shootTargetAlignment)
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
                    SetTarget(TargetType.Ally, Game.Player);
                    _bomberState = BomberState.Resupply;
                }
                break;
            }
            case BomberState.Resupply:
            {
                if (_bomb != null)
                {
                    DebugUtils.Assert(_engageTarget != null, "_engageTarget != null");
                    DebugUtils.Assert(!_engageTarget.Destroyed, "!_engageTarget.Destroyed");
                    SetTarget(TargetType.Enemy, _engageTarget);
                    _bomberState = BomberState.Bomb;
                }
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Resupply()
    {
        _bomb = _bulletScene.Instance() as BulletBomber;
        AddChild(_bomb);
        _bomb.Init((GlobalTransform.origin - GlobalTransform.basis.z * 2.1f).To2D().To3D(), Vector2.Zero, BoidAlignment.Ally, 0.0f);
        _bomb.Parent = this;
        _bomb.OnBulletDestroyed += OnBulletDestroyed;
    }

    private void OnBulletDestroyed(Bullet obj)
    {
        _bomb.OnBulletDestroyed -= OnBulletDestroyed;
        _bomb = null;
    }

    private void Shoot(Vector2 dir)
    {
        DebugUtils.Assert(IsInstanceValid(_bomb), "IsInstanceValid(_bomb)");
        if (!IsInstanceValid(_bomb))
            return;
        
        Vector2 pos = _bomb.GlobalPosition;
        RemoveChild(_bomb);
        Game.Instance.AddChild(_bomb);
        _bomb.Init(pos.To3D(), dir * _resourceStats.AttackVelocity, Alignment, _resourceStats.AttackDamage);
        _bomb.Target = _targetBoid;
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
