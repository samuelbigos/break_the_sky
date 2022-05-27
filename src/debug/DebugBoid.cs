using Godot;
using System;

[Tool]
public class DebugBoid : ImmediateGeometry
{
    public BoidBase Owner;
    
    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        GlobalTransform = new Transform(Basis.Identity, GlobalTransform.origin);
        
        Clear();
        
        // velocity
        {
            Begin(Mesh.PrimitiveType.Lines);
            SetColor(Colors.Blue);
            AddVertex(Transform.origin);
            AddVertex(Transform.origin + Owner.Velocity.To3D() * 1.0f);
            End();
        }
        
        // steering
        {
            Begin(Mesh.PrimitiveType.Lines);
            SetColor(Colors.Red);
            AddVertex(Transform.origin);
            AddVertex(Transform.origin + Owner.Steering.To3D() * 1.0f);
            End();
        }
        
        // boid
        {
            Begin(Mesh.PrimitiveType.Triangles);
            SetColor(Colors.Blue);
            Vector3 forward = Owner.Velocity.Normalized().To3D();
            Vector3 right = new Vector2(forward.z, -forward.x).To3D();
            AddVertex(Transform.origin + forward * -2.5f + right * 2.0f);
            AddVertex(Transform.origin + forward * 2.5f);
            AddVertex(Transform.origin + forward * -2.5f - right * 2.0f);
            End();
        }
    }
}
