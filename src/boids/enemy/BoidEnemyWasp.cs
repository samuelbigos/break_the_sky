using System;
using System.Collections.Generic;
using Godot;

public class BoidEnemyWasp : BoidEnemyBase
{
    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
    }

    protected override void ProcessAlive(float delta)
    {
        if (_aiState == AIState.Engaged)
        {
            _cachedHeading = (_targetBoid.GlobalPosition - GlobalPosition).Normalized().ToNumerics();
        }
        
        base.ProcessAlive(delta);
    }

    protected override void EnterAIState_Engaged()
    {
        base.EnterAIState_Engaged();
        
        ResetSteeringBehaviours();
        
        ref SteeringManager.Boid steeringBoid = ref SteeringManager.Instance.GetBoid(_steeringId);
        steeringBoid.DesiredOffsetFromTarget = (GlobalPosition - _targetBoid.GlobalPosition).ToNumerics();
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.MaintainOffset, true);
        
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Pursuit, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Wander, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Cohesion, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.Alignment, false);
        SetSteeringBehaviourEnabled(SteeringManager.Behaviours.FlowFieldFollow, false);
    }
}