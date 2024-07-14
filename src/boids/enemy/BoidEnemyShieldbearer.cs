using Godot;
using Vector2 = Godot.Vector2;

public partial class BoidEnemyShieldbearer : BoidEnemyBase
{
    [Export] private PackedScene _forcefieldScene;

    private Forcefield _forcefield;

    public override void _Ready()
    {
        _forcefield = _forcefieldScene.Instantiate<Forcefield>();
        AddChild(_forcefield);
        _forcefield.Init(Alignment, this, 25.0f, _resourceStats.MaxHealth);
    }
    
    protected override void ProcessAlive(double delta)
    {
        if (_aiState == AIState.Engaged)
        {
        }

        base.ProcessAlive(delta);
    }

    protected override void _OnDestroy(Vector2 hitDir, float hitStrength)
    {
        base._OnDestroy(hitDir, hitStrength);
        
        _forcefield.QueueFree();
    }

    protected override void OnEnterAIState_Seeking()
    {
    }

    protected override void OnEnterAIState_Engaged()
    {
    }
}