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
        SteeringManager.Instance.EdgeBounds = edgeBounds;
        
        float maxSpeed = 50.0f;
        float maxForce = 100.0f;
        float radius = 5.0f;
        
        int behaviours = 0;
        behaviours |= 1 << (int) SteeringManager.Behaviours.Separation;
        behaviours |= 1 << (int) SteeringManager.Behaviours.Alignment;
        behaviours |= 1 << (int) SteeringManager.Behaviours.AvoidObstacles;
        behaviours |= 1 << (int) SteeringManager.Behaviours.AvoidBoids;
        behaviours |= 1 << (int) SteeringManager.Behaviours.Cohesion;
        
        float[] weights = new float[(int) SteeringManager.Behaviours.COUNT];
        weights[(int) SteeringManager.Behaviours.Separation] = 2.0f;
        weights[(int) SteeringManager.Behaviours.AvoidObstacles] = 2.0f;
        weights[(int) SteeringManager.Behaviours.AvoidBoids] = 1.0f;
        weights[(int) SteeringManager.Behaviours.Arrive] = 1.0f;
        weights[(int) SteeringManager.Behaviours.Wander] = 0.1f;
        weights[(int) SteeringManager.Behaviours.Alignment] = 0.1f;
        weights[(int) SteeringManager.Behaviours.Cohesion] = 0.1f;
        weights[(int) SteeringManager.Behaviours.MaintainSpeed] = 0.1f;
        
        // allies
        {
            // drones
            int droneBehaviours = 0;
            droneBehaviours |= 1 << (int) SteeringManager.Behaviours.Separation;
            droneBehaviours |= 1 << (int) SteeringManager.Behaviours.Alignment;
            droneBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidObstacles;
            droneBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidBoids;
            droneBehaviours |= 1 << (int) SteeringManager.Behaviours.Cohesion;
            droneBehaviours |= 1 << (int) SteeringManager.Behaviours.Arrive;
            
            for (int i = 0; i < 10; i++)
            {
                Vector2 spawnPos = Vector2.Left * 50.0f;
                SteeringManager.Boid boid = new()
                {
                    Alive = true,
                    Alignment = 0,
                    Position = spawnPos,
                    Velocity = Vector2.Zero,
                    Radius = radius,
                    Heading = Vector2.Up,
                    MaxSpeed = maxSpeed,
                    MaxForce = maxForce,
                    LookAhead = 0.5f,
                    Behaviours = droneBehaviours,
                    Weights = weights,
                    Target = Vector2.Zero,
                    ViewRange = 50.0f,
                    ViewAngle = 360.0f,
                    IgnoreAllyAvoidance = true,
                };
                _boidIds.Add(SteeringManager.Instance.AddBoid(boid));
            }
            
            // bombers
            int bomberBehaviours = 0;
            bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.Separation;
            bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.Alignment;
            bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidObstacles;
            bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidBoids;
            bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.Cohesion;
            bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.Arrive;
            bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidBoids;
            
            for (int i = 0; i < 10; i++)
            {
                Vector2 randPos = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * edgeBounds.Size * 0.25f;
                SteeringManager.Boid boid = new()
                {
                    Alive = true,
                    Alignment = 0,
                    Position = randPos,
                    Velocity = Vector2.Zero,
                    Radius = radius,
                    Heading = Vector2.Up,
                    MaxSpeed = 150.0f,
                    MinSpeed = 50.0f,
                    MaxForce = 66.0f,
                    LookAhead = 1.0f,
                    Behaviours = bomberBehaviours,
                    Weights = weights,
                    Target = Vector2.Zero,
                    ViewRange = 50.0f,
                    ViewAngle = 360.0f,
                };
                _boidIds.Add(SteeringManager.Instance.AddBoid(boid));
            }
        }
        
        // enemies
        {
            for (int i = 0; i < 50; i++)
            {
                Vector2 randPos = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * edgeBounds.Size * 0.25f;
                Vector2 randVel = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * maxSpeed;
                SteeringManager.Boid boid = new()
                {
                    Alive = true,
                    Alignment = 1,
                    Position = randPos,
                    Velocity = randVel,
                    Radius = radius,
                    Heading = Vector2.Up,
                    MaxSpeed = maxSpeed,
                    DesiredSpeed = maxSpeed,
                    MaxForce = maxForce,
                    LookAhead = 0.5f,
                    Behaviours = behaviours | 1 << (int) SteeringManager.Behaviours.MaintainSpeed,
                    Weights = weights,
                    Target = Vector2.Zero,
                    ViewRange = 50.0f,
                    ViewAngle = 240.0f,
                };
                _boidIds.Add(SteeringManager.Instance.AddBoid(boid));
            }
        }
        
        // leaders
        {
            int leaderBehaviours = 0;
            //leaderBehaviours |= 1 << (int) SteeringManager.Behaviours.Separation;
            leaderBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidObstacles;
            leaderBehaviours |= 1 << (int) SteeringManager.Behaviours.MaintainSpeed;
            leaderBehaviours |= 1 << (int) SteeringManager.Behaviours.Wander;
            //leaderBehaviours |= 1 << (int) SteeringManager.Behaviours.Arrive;
            
            for (int i = 0; i < 1; i++)
            {
                Vector2 randPos = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * edgeBounds.Size * 0.25f;
                Vector2 randVel = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * maxSpeed;
                SteeringManager.Boid boid = new()
                {
                    Alive = true,
                    Alignment = 1,
                    Position = randPos,
                    Velocity = randVel,
                    Radius = radius * 3.0f,
                    Heading = Vector2.Up,
                    MaxSpeed = maxSpeed * 0.5f,
                    DesiredSpeed = maxSpeed * 0.5f,
                    MaxForce = maxForce,
                    LookAhead = 0.5f,
                    Behaviours = leaderBehaviours,
                    Weights = weights,
                    Target = Vector2.Zero,
                    ViewRange = 50.0f,
                    ViewAngle = 270.0f,
                    WanderCircleDist = 25.0f,
                    WanderCircleRadius = 5.0f,
                    WanderVariance = 50.0f,
                };
                _boidIds.Add(SteeringManager.Instance.AddBoid(boid));
            }
        }
        
        // obstacles
        {
            for (int i = 0; i < 0; i++)
            {
                Vector2 randPos = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * edgeBounds.Size * 0.25f;
                float randRad = (Utils.Rng.Randf() + 0.5f) * 40.0f;
                _obstacleIds.Add(SteeringManager.Instance.AddObstacle(randPos, SteeringManager.ObstacleShape.Circle, randRad));
            }
        }
        
        // _boidIds.Add(SteeringManager.Instance.AddBoid(new Vector2(-100.0f, 0.0f), new Vector2(100.0f, 0.0f), radius, maxSpeed, maxForce, behaviours,
        //     weights, Vector2.Zero, 360.0f, 1));
        
        // _boidIds.Add(SteeringManager.Instance.AddBoid(new Vector2(100.0f, 0.0f), new Vector2(-100.0f, 0.0f), radius, maxSpeed, maxForce, behaviours,
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
                SteeringManager.Boid boid = SteeringManager.Instance.GetBoid(id);
                Vector2 pos = GetViewport().GetMousePosition();
                Vector3 origin = _camera.ProjectRayOrigin(pos);
                Vector3 normal = _camera.ProjectRayNormal(pos);
                Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * _camera.GlobalTransform.origin.y;
                Vector2 target = new(hit.x, hit.z);
                boid.Target = target;
                SteeringManager.Instance.SetBoid(boid);
            }
        }

        SteeringManager.Instance.DrawSimulationToMesh(out Mesh mesh);
        _boidsMesh.Mesh = mesh;
    }
}
