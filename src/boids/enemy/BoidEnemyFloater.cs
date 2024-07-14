using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class BoidEnemyFloater : BoidEnemyBase
{
    protected override void ProcessAlive(double delta)
    {
        switch (_aiState)
        {
            case AIState.Engaged:
            {
                break;
            }
            default:
            {
                break;
            }
        }

        base.ProcessAlive(delta);
    }

    protected override void OnEnterAIState_Engaged()
    {
        base.OnEnterAIState_Engaged();
        
        ResetSteeringBehaviours();
        
        // ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetObject<SteeringManager.Boid>(_steeringId);
        // steeringBoid.DesiredDistFromTargetMin = EngageRange * 0.8f;
        // steeringBoid.DesiredDistFromTargetMax = EngageRange;
        // SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainDistance, true);
        //
        // SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, false);
        // SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Wander, false);
        // SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Cohesion, false);
        // SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Alignment, false);
        // SetSteeringBehaviourEnabled(SteeringManager.Behaviours.FlowFieldFollow, false);
    }
}