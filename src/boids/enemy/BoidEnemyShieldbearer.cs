using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using GodotOnReady.Attributes;

public partial class BoidEnemyShieldbearer : BoidEnemyBase
{
    protected override void ProcessAlive(float delta)
    {
        if (_aiState == AIState.Engaged)
        {
        }
        
        base.ProcessAlive(delta);
    }

    protected override void OnEnterAIState_Seeking()
    {
    }

    protected override void OnEnterAIState_Engaged()
    {
    }
}