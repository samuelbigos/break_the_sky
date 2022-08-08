using System;
using System.Collections.Generic;
using Godot;

public partial class BoidEnemyBase : BoidBase
{
    protected enum AIState
    {
        Idle,
        Seeking,
        Engaged,
        Flee
    }
    
    [Export] public float DestroyTrauma = 0.1f;
    [Export] public float HitTrauma = 0.05f;
    [Export] public float EngageRange = 100.0f;
    [Export] public bool SeekPlayerOnSpawn = true;

    public bool IsTargetted = false;
    public ResourceBoidEnemy Data => _data as ResourceBoidEnemy;
    public BoidAllyBase EnemyTarget => _targetType == TargetType.Enemy ? _targetBoid as BoidAllyBase : null;

    public override BoidAlignment Alignment => BoidAlignment.Enemy;
    protected AIState _aiState = AIState.Seeking;

    public override void Init(ResourceBoid data, Action<BoidBase> onDestroy, Vector2 position, Vector2 velocity)
    {
        base.Init(data, onDestroy, position, velocity);
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        steeringBoid.DesiredDistFromTargetMin = EngageRange - EngageRange * 0.05f;
        steeringBoid.DesiredDistFromTargetMax = EngageRange + EngageRange * 0.05f;
        
        if (SeekPlayerOnSpawn)
        {
            SwitchAiState(AIState.Seeking);
        }
        else
        {
            SwitchAiState(AIState.Idle);
        }
        
        _mesh.AltShaders[0].SetShaderParam("u_outline_colour", ColourManager.Instance.EnemyOutline);
    }

    protected override void ProcessAlive(float delta)
    {
        switch (_aiState)
        {
            case AIState.Idle:
            case AIState.Seeking:
            {
                foreach (BoidAllyBase boid in BoidFactory.Instance.AllyBoids)
                {
                    float distSq = (boid.GlobalPosition - GlobalPosition).LengthSquared();
                    if (distSq < EngageRange * EngageRange)
                    {
                        SetTarget(TargetType.Enemy, boid);
                        SwitchAiState(AIState.Engaged);
                    }
                }
                break;
            }
            case AIState.Engaged:
                break;
            case AIState.Flee:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // base.ProcessAlive is called after child processing in-case we are destroyed this frame (so we don't do
        // child processing after being destroyed).
        base.ProcessAlive(delta);
    }

    protected void SwitchAiState(AIState state)
    {
        switch (state)
        {
            case AIState.Idle:
                OnEnterAIState_Idle();
                break;
            case AIState.Seeking:
                OnEnterAIState_Seeking();
                break;
            case AIState.Engaged:
                OnEnterAIState_Engaged();
                break;
            case AIState.Flee:
                OnEnterAIState_Flee();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        _aiState = state;
    }

    protected virtual void OnEnterAIState_Idle()
    {
        SetTarget(TargetType.None);
        ResetSteeringBehaviours();
    }

    protected virtual void OnEnterAIState_Seeking()
    {
        SetTarget(TargetType.Enemy, Game.Player);
        ResetSteeringBehaviours();
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, true);
    }
    
    protected virtual void OnEnterAIState_Engaged()
    {
    }
    
    protected virtual void OnEnterAIState_Flee()
    {
        SetTarget(TargetType.Enemy, Game.Player);
        ResetSteeringBehaviours();
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Flee, true);
    }

    protected override void _OnHit(float damage, Vector2 bulletVel, Vector2 pos)
    {
        base._OnHit(damage, bulletVel, pos);

        GameCamera.Instance.AddTrauma(HitTrauma);
    }
    
    protected override void _OnDestroy(Vector2 hitDir, float hitStrength)
    {
        base._OnDestroy(hitDir, hitStrength);

        for (int i = 0; i < Data.MaterialDropCount; i++)
        {
            PickupMaterial drop = _pickupMaterialScene.Instance<PickupMaterial>();
            Game.Instance.RegisterPickup(drop);
            drop.GlobalTransform = GlobalTransform;
            float eject = 25.0f;
            drop.Init(new Vector2(Utils.RandfUnit(), Utils.RandfUnit()).Normalized() * eject, Game.Player);
        }

        SaveDataPlayer.Experience += Data.Experience;
    }

    protected override void _OnTargetBoidDestroyed(BoidBase boid)
    {
        base._OnTargetBoidDestroyed(boid);
        
        SetTarget(TargetType.None);
        SwitchAiState(AIState.Seeking);
    }
}