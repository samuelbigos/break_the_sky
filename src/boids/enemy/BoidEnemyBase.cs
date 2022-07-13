using System;
using System.Collections.Generic;
using Godot;

public class BoidEnemyBase : BoidBase
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
    
    private int _cachedBehaviours;

    protected override void SetMeshColour()
    {
        _meshMaterial?.SetShaderParam("u_primary_colour", ColourManager.Instance.Secondary);
        _meshMaterial?.SetShaderParam("u_secondary_colour", ColourManager.Instance.Red);
    }

    public override void _Ready()
    {
        base._Ready();

        _cachedBehaviours = _behaviours;
    }

    public override void Init(ResourceBoid data, Action<BoidBase> onDestroy, Vector2 position, Vector2 velocity)
    {
        base.Init(data, onDestroy, position, velocity);
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetBoid(_steeringId);
        steeringBoid.DesiredDistFromTarget = EngageRange;
        
        EnterAIState(AIState.Seeking);
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
                        EnterAIState(AIState.Engaged);
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

    protected void EnterAIState(AIState state)
    {
        switch (state)
        {
            case AIState.Seeking:
                EnterAIState_Seeking();
                break;
            case AIState.Engaged:
                EnterAIState_Engaged();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        _aiState = state;
    }

    protected virtual void EnterAIState_Seeking()
    {
        // set target as player
        SetTarget(TargetType.Enemy, Game.Player);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, true);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Wander, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Cohesion, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Alignment, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.FlowFieldFollow, false);
    }
    
    protected virtual void EnterAIState_Engaged()
    {
    }

    protected void ResetSteeringBehaviours()
    {
        _behaviours = _cachedBehaviours;
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetBoid(_steeringId);
        steeringBoid.Behaviours = _behaviours;
    }

    protected override void _OnHit(float damage, Vector2 bulletVel, Vector2 pos)
    {
        base._OnHit(damage, bulletVel, pos);

        GameCamera.Instance.AddTrauma(HitTrauma);
    }
    
    protected override void _Destroy(Vector2 hitDir, float hitStrength)
    {
        base._Destroy(hitDir, hitStrength);

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
        
        EnterAIState(AIState.Seeking);
    }
}