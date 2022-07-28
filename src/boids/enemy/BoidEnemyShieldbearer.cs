using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Godot;
using GodotOnReady.Attributes;
using Vector3 = Godot.Vector3;

public partial class BoidEnemyShieldbearer : BoidEnemyBase
{
    [Export] private PackedScene _forcefieldScene;

    private Forcefield _forcefield;

    [OnReady]
    private void Ready()
    {
        _forcefield = _forcefieldScene.Instance<Forcefield>();
        AddChild(_forcefield);
        _forcefield.Init(Alignment, this, 25.0f, _resourceStats.MaxHealth);
    }
    
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