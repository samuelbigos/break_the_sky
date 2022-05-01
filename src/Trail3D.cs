using System.Collections.Generic;
using Godot;

public class Trail3D : Node
{
    [Export] public float Width = 2.0f;

    private float _alpha = 1.0f;
    private BoidBase3D _boid = null;

    public void Init(BoidBase3D owner)
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