using System;
using System.Collections.Generic;
using Godot;

public partial class BoidEnemyBase : BoidBase
{
    protected enum AIState
    {
        Seeking,
        Engaged
    }
    
    [Export] public float DestroyTrauma = 0.1f;
    [Export] public float HitTrauma = 0.05f;
    [Export] public float EngageRange = 100.0f;

    public bool IsTargetted = false;
    public ResourceBoidEnemy Data => _data as ResourceBoidEnemy;

    public override BoidAlignment Alignment => BoidAlignment.Enemy;
    protected AIState _aiState = AIState.Seeking;

    protected override void SetMeshColour()
    {
        _meshMaterial?.SetShaderParam("u_primary_colour", ColourManager.Instance.Secondary);
        _meshMaterial?.SetShaderParam("u_secondary_colour", ColourManager.Instance.Red);
    }

    public override void Init(ResourceBoid data, Action<BoidBase> onDestroy, Vector2 position, Vector2 velocity)
    {
        base.Init(data, onDestroy, position, velocity);
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        steeringBoid.DesiredDistFromTargetMin = EngageRange - EngageRange * 0.05f;
        steeringBoid.DesiredDistFromTargetMax = EngageRange + EngageRange * 0.05f;
        
        SwitchAiState(AIState.Seeking);
    }

    protected override void ProcessAlive(float delta)
    {
        switch (_aiState)
        {
            case AIState.Seeking:
                // if we're seeking, we move generally towards the player looking for a target.
                List<BoidAllyBase> allyBoids = BoidFactory.Instance.AllyBoids;
                foreach (BoidAllyBase boid in allyBoids)
                {
                    float distSq = (boid.GlobalPosition - GlobalPosition).LengthSquared();
                    if (distSq < EngageRange * EngageRange)
                    {
                        SetTarget(TargetType.Enemy, boid);
                        SwitchAiState(AIState.Engaged);
                    }
                }
                break;
            case AIState.Engaged:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        // switch (_targetType)
        // {
        //     case TargetType.None:
        //     {
        //         // TODO: optimise
        //         List<BoidAllyBase> allyBoids = BoidFactory.Instance.AllyBoids;
        //         foreach (BoidAllyBase boid in allyBoids)
        //         {
        //             float distSq = (boid.GlobalPosition - GlobalPosition).LengthSquared();
        //             if (distSq < EngageRange * EngageRange)
        //             {
        //                 SetTarget(TargetType.Enemy, boid);
        //                 SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, true);
        //                 SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Wander, false);
        //                 SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Cohesion, false);
        //                 SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Alignment, false);
        //                 SetSteeringBehaviourEnabled(SteeringManager.Behaviours.FlowFieldFollow, false);
        //             }
        //         }
        //         break;
        //     }
        //     case TargetType.Ally:
        //     case TargetType.Enemy:
        //     case TargetType.Position:
        //         break;
        //     default:
        //         throw new ArgumentOutOfRangeException();
        // }
        
        // base.ProcessAlive is called after child processing in-case we are destroyed this frame (so we don't do
        // child processing after being destroyed).
        base.ProcessAlive(delta);
    }

    protected void SwitchAiState(AIState state)
    {
        switch (state)
        {
            case AIState.Seeking:
                OnEnterAIState_Seeking();
                break;
            case AIState.Engaged:
                OnEnterAIState_Engaged();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        _aiState = state;
    }

    protected virtual void OnEnterAIState_Seeking()
    {
        // set target as player
        SetTarget(TargetType.Enemy, Game.Player);
        ResetSteeringBehaviours();
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, true);
    }
    
    protected virtual void OnEnterAIState_Engaged()
    {
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