using Godot;
using System;
using System.Collections.Generic;
using GodotOnReady.Attributes;

public partial class BoidTestbed : Spatial
{
    [OnReadyGet] private MeshInstance _boidsMesh;
    [OnReadyGet] private Camera _camera;
    
    private List<int> _boidIds = new();
    
    [OnReady] private void Ready()
    {
        int behaviours = 0;
        behaviours |= 1 << (int) FlockingManager.Behaviours.Separation;
        behaviours |= 1 << (int) FlockingManager.Behaviours.EdgeRepulsion;
        behaviours |= 1 << (int) FlockingManager.Behaviours.Arrive;

        float[] weights = new[] {1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f};
        float maxSpeed = 100.0f;
        float maxForce = 1.0f;
        
        FlockingManager.Instance.EdgeBounds = new Rect2(-120.0f, -80.0f, 240.0f, 160.0f);

        // for (int i = 0; i < 10; i++)
        // {
        //     FlockingManager.Instance.AddBoid(new Vector2((i - 5) * 10.0f, 0.0f), Vector2.Up * 100.0f, maxSpeed, maxForce, behaviours,
        //         weights, Vector2.Zero, 270.0f);
        // }
        
        // FlockingManager.Instance.AddBoid(new Vector2(-50.0f, 0.0f), Vector2.Right * 50.0f, maxSpeed, maxForce, behaviours,
        //     weights, Vector2.Zero, 270.0f);

        _boidIds.Add(FlockingManager.Instance.AddBoid(new Vector2(50.0f, 0.0f), Vector2.Left * 50.0f, maxSpeed,
            maxForce, behaviours, weights, Vector2.Zero, 270.0f));
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (Input.IsActionPressed("tactical_select"))
        {
            foreach (int id in _boidIds)
            {
                FlockingManager.Boid boid = FlockingManager.Instance.GetBoid(id);
                Vector2 pos = GetViewport().GetMousePosition();
                Vector3 origin = _camera.ProjectRayOrigin(pos);
                Vector3 normal = _camera.ProjectRayNormal(pos);
                Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * _camera.GlobalTransform.origin.y;
                Vector2 target = new(hit.x, hit.z);
                GD.Print(target);
                boid.Target = target;
                FlockingManager.Instance.SetBoid(boid);
            }
        }
        
        FlockingManager.Instance.DrawSimulationToMesh(out Mesh mesh);
        _boidsMesh.Mesh = mesh;
    }
}
