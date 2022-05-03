using Godot;

public class Trail : Node
{
    [Export] public float Width = 2.0f;

    private float _alpha = 1.0f;
    private BoidBase _boid = null;

    public void Init(BoidBase owner)
    {
        _boid = owner;
    }
    
    public override void _Ready()
    {
    }

    public override void _Process(float delta)
    {
    }
}