using Godot;
using System;

public class Test : Spatial
{
    private BoidBase _boid = null;
    
    public override void _Ready()
    {
    }
    
    public override void _Process(float delta)
    {
        if (_boid == null)
        {
            _boid = GetNode<BoidBase>("RigidBody/BoidEnemyDummy");
            RigidBody rb = _boid.GetParent() as RigidBody;
            rb.ApplyTorqueImpulse(Vector3.Right * 100.0f);
        }
    }
}
