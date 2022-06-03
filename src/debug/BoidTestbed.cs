using Godot;
using System;
using System.Collections.Generic;
using GodotOnReady.Attributes;

public partial class BoidTestbed : Spatial
{
    [OnReadyGet] private MeshInstance _boidsMesh;
    [OnReadyGet] private Camera _camera;
    
    private List<int> _boidIds = new();
    private List<int> _obstacleIds = new();
    
    [OnReady] private void Ready()
    {
        Vector2 screenBounds = GetViewport().Size - Vector2.One * 20.0f;
        Vector3 origin = _camera.ProjectRayOrigin(screenBounds);
        Vector3 normal = _camera.ProjectRayNormal(screenBounds);
        Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * _camera.GlobalTransform.origin.y;
        Rect2 edgeBounds = new Rect2(-hit.x, -hit.z, hit.x * 2.0f, hit.z * 2.0f);
        FlockingManager.Instance.EdgeBounds = edgeBounds;
        
        float maxSpeed = 100.0f;
        float maxForce = 300.0f;
        float radius = 5.0f;
        
        int behaviours = 0;
        behaviours |= 1 << (int) FlockingManager.Behaviours.Separation;
        behaviours |= 1 << (int) FlockingManager.Behaviours.Alignment;
        behaviours |= 1 << (int) FlockingManager.Behaviours.Avoidance;
        behaviours |= 1 << (int) FlockingManager.Behaviours.Cohesion;
        
        float[] weights = new float[(int) FlockingManager.Behaviours.COUNT];
        weights[(int) FlockingManager.Behaviours.Separation] = 2.5f;
        weights[(int) FlockingManager.Behaviours.EdgeRepulsion] = 1.0f;
        weights[(int) FlockingManager.Behaviours.Arrive] = 1.0f;
        weights[(int) FlockingManager.Behaviours.Alignment] = 0.1f;
        weights[(int) FlockingManager.Behaviours.Cohesion] = 0.1f;
        
        // allies
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 spawnPos = Vector2.Left * 50.0f;
                _boidIds.Add(FlockingManager.Instance.AddBoid(spawnPos, Vector2.Zero, radius, maxSpeed, maxForce, 
                    behaviours | 1 << (int) FlockingManager.Behaviours.Arrive,
                    weights, Vector2.Zero, 360.0f, 0));
            }
        }
        
        // enemies
        {
            for (int i = 0; i < 50; i++)
            {
                Vector2 randPos = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * edgeBounds.Size * 0.25f;
                Vector2 randVel = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * maxSpeed;
                _boidIds.Add(FlockingManager.Instance.AddBoid(randPos, randVel, radius, maxSpeed, maxForce, behaviours,
                    weights, Vector2.Zero, 180.0f, 1));
            }
        }
        
        // obstacles
        {
            for (int i = 0; i < 5; i++)
            {
                Vector2 randPos = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * edgeBounds.Size * 0.25f;
                float randRad = (Utils.Rng.Randf() + 0.5f) * 40.0f;
                _obstacleIds.Add(FlockingManager.Instance.AddObstacle(randPos, FlockingManager.ObstacleShape.Circle, randRad));
            }
        }
        
        // _boidIds.Add(FlockingManager.Instance.AddBoid(new Vector2(-100.0f, 0.0f), new Vector2(100.0f, 0.0f), radius, maxSpeed, maxForce, behaviours,
        //     weights, Vector2.Zero, 360.0f, 1));
        
        // _boidIds.Add(FlockingManager.Instance.AddBoid(new Vector2(100.0f, 0.0f), new Vector2(-100.0f, 0.0f), radius, maxSpeed, maxForce, behaviours,
        //     weights, Vector2.Zero, 360.0f, 1));
        
        Engine.TimeScale = 1.0f;
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
                boid.Target = target;
                FlockingManager.Instance.SetBoid(boid);
            }
        }
        
        FlockingManager.Instance.DrawSimulationToMesh(out Mesh mesh);
        _boidsMesh.Mesh = mesh;
    }
}
