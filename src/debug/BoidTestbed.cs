using Godot;
using System;
using System.Collections.Generic;
using GodotOnReady.Attributes;

public partial class BoidTestbed : Spatial
{
    [OnReadyGet] private MeshInstance _boidsMesh;
    [OnReadyGet] private Camera _camera;
    
    [Export] private Rect2 _playArea;
    
    private List<int> _boidIds = new();
    private List<int> _obstacleIds = new();
    
    [OnReady] private void Ready()
    {
        // Vector2 screenBounds = GetViewport().Size - Vector2.One * 20.0f;
        // Vector3 origin = _camera.ProjectRayOrigin(screenBounds);
        // Vector3 normal = _camera.ProjectRayNormal(screenBounds);
        // Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * _camera.GlobalTransform.origin.y;
        // Rect2 edgeBounds = new Rect2(-hit.x, -hit.z, hit.x * 2.0f, hit.z * 2.0f);
        //
        // edgeBounds = _playArea;
        // SteeringManager.EdgeBounds = _playArea;
        // int sharedPropertiesId = 0;
        //
        // float[] weights = new float[(int) SteeringManager.Behaviours.COUNT];
        // weights[(int) SteeringManager.Behaviours.Separation] = 2.0f;
        // weights[(int) SteeringManager.Behaviours.AvoidObstacles] = 2.0f;
        // weights[(int) SteeringManager.Behaviours.AvoidAllies] = 1.0f;
        // weights[(int) SteeringManager.Behaviours.AvoidEnemies] = 1.0f;
        // weights[(int) SteeringManager.Behaviours.Arrive] = 1.0f;
        // weights[(int) SteeringManager.Behaviours.Wander] = 0.1f;
        // weights[(int) SteeringManager.Behaviours.Alignment] = 0.1f;
        // weights[(int) SteeringManager.Behaviours.Cohesion] = 0.1f;
        // weights[(int) SteeringManager.Behaviours.MaintainSpeed] = 0.1f;
        // weights[(int) SteeringManager.Behaviours.FlowFieldFollow] = 1.0f;
        //
        // // allies
        // {
        //     SteeringManager.BoidSharedProperties boidSharedProperties = new()
        //     {
        //         Id = sharedPropertiesId++,
        //         MaxSpeed = 75.0f,
        //         MaxForce = 125.0f,
        //         DesiredSpeed = 75.0f,
        //         LookAhead = 0.5f,
        //         Weights = weights,
        //         ViewRange = 50.0f,
        //         ViewAngle = 360.0f,
        //     };
        //     SteeringManager.Instance.RegisterSharedProperties(boidSharedProperties);
        //     
        //     // drones
        //     int droneBehaviours = 0;
        //     droneBehaviours |= 1 << (int) SteeringManager.Behaviours.Separation;
        //     droneBehaviours |= 1 << (int) SteeringManager.Behaviours.Alignment;
        //     droneBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidObstacles;
        //     //droneBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidAllies;
        //     droneBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidEnemies;
        //     droneBehaviours |= 1 << (int) SteeringManager.Behaviours.Cohesion;
        //     droneBehaviours |= 1 << (int) SteeringManager.Behaviours.Arrive;
        //     
        //     for (int i = 0; i < 0; i++)
        //     {
        //         Vector2 spawnPos = Vector2.Left * 50.0f;
        //         SteeringManager.Boid boid = new()
        //         {
        //             Alignment = 0,
        //             Radius = 5.0f,
        //             SharedPropertiesIdx = (short)boidSharedProperties.Id,
        //             Position = spawnPos,
        //             Velocity = Vector2.Zero,
        //             Heading = Vector2.Up,
        //             Behaviours = droneBehaviours,
        //             Target = Vector2.Zero,
        //         };
        //         _boidIds.Add(SteeringManager.Instance.RegisterBoid(boid));
        //     }
        //     
        //     // bombers
        //     int bomberBehaviours = 0;
        //     bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.Separation;
        //     bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.Alignment;
        //     bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidObstacles;
        //     bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidAllies;
        //     bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidEnemies;
        //     bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.Cohesion;
        //     bomberBehaviours |= 1 << (int) SteeringManager.Behaviours.Arrive;
        //
        //     for (int i = 0; i < 0; i++)
        //     {
        //         Vector2 randPos = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * edgeBounds.Size * 0.25f;
        //         SteeringManager.Boid boid = new()
        //         {
        //             SharedPropertiesIdx = (short)boidSharedProperties.Id,
        //             Position = randPos,
        //             Velocity = Vector2.Zero,
        //             Heading = Vector2.Up,
        //             Behaviours = bomberBehaviours,
        //             Target = Vector2.Zero,
        //         };
        //         _boidIds.Add(SteeringManager.Instance.RegisterBoid(boid));
        //     }
        // }
        //
        // // enemies
        // {
        //     SteeringManager.BoidSharedProperties enemySharedProperties = new()
        //     {
        //         Id = sharedPropertiesId++,
        //         MaxSpeed = 75.0f,
        //         DesiredSpeed = 75.0f,
        //         MaxForce = 125.0f,
        //         LookAhead = 0.5f,
        //         Weights = weights,
        //         ViewRange = 50.0f,
        //         ViewAngle = 240.0f,
        //     };
        //     SteeringManager.Instance.RegisterSharedProperties(enemySharedProperties);
        //     
        //     int enemyBehaviours = 0;
        //     enemyBehaviours |= 1 << (int) SteeringManager.Behaviours.Separation;
        //     enemyBehaviours |= 1 << (int) SteeringManager.Behaviours.Alignment;
        //     enemyBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidObstacles;
        //     enemyBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidAllies;
        //     //enemyBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidEnemies;
        //     enemyBehaviours |= 1 << (int) SteeringManager.Behaviours.Cohesion;
        //     enemyBehaviours |= 1 << (int) SteeringManager.Behaviours.MaintainSpeed;
        //     enemyBehaviours |= 1 << (int) SteeringManager.Behaviours.FlowFieldFollow;
        //     
        //     for (int i = 0; i < 100; i++)
        //     {
        //         Vector2 randPos = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * edgeBounds.Size * 0.33f;
        //         Vector2 randVel = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * 75.0f;
        //         SteeringManager.Boid boid = new()
        //         {
        //             Alignment = 1,
        //             Radius = 7.0f,
        //             SharedPropertiesIdx = (short)enemySharedProperties.Id,
        //             Position = randPos,
        //             Velocity = randVel,
        //             Heading = Vector2.Up,
        //             Behaviours = enemyBehaviours,
        //             Target = Vector2.Zero,
        //             //FlowFieldDist = 25.0f,
        //         };
        //         _boidIds.Add(SteeringManager.Instance.RegisterBoid(boid));
        //     }
        // }
        //
        // // leaders
        // int leaderId = 0;
        // {
        //     SteeringManager.BoidSharedProperties leaderSharedProperties = new()
        //     {
        //         Id = sharedPropertiesId++,
        //         MaxSpeed = 25.0f,
        //         DesiredSpeed = 15.0f,
        //         MaxForce = 100.0f,
        //         LookAhead = 0.5f,
        //         Weights = weights,
        //         ViewRange = 50.0f,
        //         ViewAngle = 270.0f,
        //         WanderCircleDist = 25.0f,
        //         WanderCircleRadius = 5.0f,
        //         WanderVariance = 50.0f,
        //     };
        //     SteeringManager.Instance.RegisterSharedProperties(leaderSharedProperties);
        //     
        //     int leaderBehaviours = 0;
        //     //leaderBehaviours |= 1 << (int) SteeringManager.Behaviours.Separation;
        //     leaderBehaviours |= 1 << (int) SteeringManager.Behaviours.AvoidObstacles;
        //     leaderBehaviours |= 1 << (int) SteeringManager.Behaviours.MaintainSpeed;
        //     leaderBehaviours |= 1 << (int) SteeringManager.Behaviours.Wander;
        //     //leaderBehaviours |= 1 << (int) SteeringManager.Behaviours.Arrive;
        //     
        //     for (int i = 0; i < 1; i++)
        //     {
        //         Vector2 randPos = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * edgeBounds.Size * 0.1f;
        //         Vector2 randVel = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * 25.0f;
        //         SteeringManager.Boid boid = new()
        //         {
        //             Alignment = 1,
        //             Radius = 15.0f,
        //             SharedPropertiesIdx = (short)leaderSharedProperties.Id,
        //             Position = randPos,
        //             Velocity = randVel,
        //             Heading = Vector2.Up,
        //             Behaviours = leaderBehaviours,
        //             Target = Vector2.Zero,
        //         };
        //         leaderId = SteeringManager.Instance.RegisterBoid(boid);
        //     }
        // }
        //
        // // obstacles
        // {
        //     for (int i = 0; i < 0; i++)
        //     {
        //         Vector2 randPos = new Vector2(Utils.RandfUnit(), Utils.RandfUnit()) * edgeBounds.Size * 0.25f;
        //         float randRad = (Utils.Rng.Randf() + 0.5f) * 40.0f;
        //         SteeringManager.Obstacle obstacle = new()
        //         {
        //             Position = randPos,
        //             Shape = SteeringManager.ObstacleShape.Circle,
        //             Size = randRad
        //         };
        //         _obstacleIds.Add(SteeringManager.Instance.RegisterObstacle(obstacle));
        //     }
        // }
        //
        // // flow fields
        // SteeringManager.FlowField flowField = new()
        // {
        //     Resource = ResourceLoader.Load<FlowFieldResource>("res://assets/flowfields/star.res"),
        //     TrackID = leaderId,
        //     Size = new Vector2(500, 500),
        // };
        // SteeringManager.Instance.RegisterFlowField(flowField);
        //
        // // _boidIds.Add(SteeringManager.Instance.AddBoid(new Vector2(-100.0f, 0.0f), new Vector2(100.0f, 0.0f), radius, maxSpeed, maxForce, behaviours,
        // //     weights, Vector2.Zero, 360.0f, 1));
        //
        // // _boidIds.Add(SteeringManager.Instance.AddBoid(new Vector2(100.0f, 0.0f), new Vector2(-100.0f, 0.0f), radius, maxSpeed, maxForce, behaviours,
        // //     weights, Vector2.Zero, 360.0f, 1));
        //
        // Engine.TimeScale = 1.0f;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (Input.IsActionPressed("tactical_select"))
        {
            foreach (int id in _boidIds)
            {
                if (SteeringManager.Instance.HasBoid(id))
                {
                    SteeringManager.Boid boid = SteeringManager.Instance.GetBoid(id);
                    Vector2 pos = GetViewport().GetMousePosition();
                    Vector3 origin = _camera.ProjectRayOrigin(pos);
                    Vector3 normal = _camera.ProjectRayNormal(pos);
                    Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * _camera.GlobalTransform.origin.y;
                    Vector2 target = new(hit.x, hit.z);
                    boid.Target = target.ToNumerics();
                    //SteeringManager.Instance.SetBoid(boid);
                }
            }
        }

        SteeringManager.Instance.DrawSimulationToMesh(out Mesh mesh);
        _boidsMesh.Mesh = mesh;
    }
}
