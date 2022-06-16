using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot.Collections;
using static SteeringManager.Behaviours;

public partial class SteeringManager : Singleton<SteeringManager>
{
    public enum Behaviours // in order of importance
    {
        Separation,
        AvoidObstacles,
        AvoidAllies,
        AvoidEnemies,
        MaintainSpeed,
        Cohesion,
        Alignment,
        Arrive,
        Pursuit,
        Flee,
        EdgeRepulsion,
        Wander,
        FlowFieldFollow,
        COUNT,
    }

    public enum ObstacleShape
    {
        Circle,
    }

    public struct Boid
    {
        public int Id;
        public int Alignment;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Steering;
        public Vector2 Heading;
        public float Speed;
        public float DesiredSpeed;
        public float MaxSpeed;
        public float MinSpeed;
        public float MaxForce;
        public float Radius;
        public float LookAhead;
        public int Behaviours;
        public float[] Weights;
        public Vector2 Target;
        public int TargetIndex;
        public Vector2 TargetOffset;
        public float ViewRange;
        public float ViewAngle;
        public Intersection Intersection;
        public float WanderAngle;
        public float WanderCircleDist;
        public float WanderCircleRadius;
        public float WanderVariance;
    }

    public struct Obstacle
    {
        public int ID;
        public Vector2 Position;
        public ObstacleShape Shape;
        public float Size;
    }

    public struct FlowField
    {
        public int ID;
        public FlowFieldResource Resource;
        public int TrackID;
        public Vector2 Position;
        public Vector2 Size;
        public Rect2 Bounds;
    }

    public struct Intersection
    {
        public bool Intersect;
        public float IntersectTime;
        public Vector2 SurfacePoint;
        public Vector2 SurfaceNormal;
    }

    public static Rect2 EdgeBounds;

    private StructPool<Boid> _boidPool = new(1000);
    private int _numObstacles;
    private Obstacle[] _obstaclePool = new Obstacle[100];
    private int _numFlowFields;
    private FlowField[] _flowFieldPool = new FlowField[100];

    private System.Collections.Generic.Dictionary<int, int> _boidIdToIndex = new();
    
    private System.Collections.Generic.Dictionary<int, int> _obstacleIdToIndex = new();
    private System.Collections.Generic.Dictionary<int, int> _obstacleIndexToId = new();
    private System.Collections.Generic.Dictionary<int, int> _flowFieldIdToIndex = new();
    private System.Collections.Generic.Dictionary<int, int> _flowFieldIndexToId = new();
    
    private int _boidIdGen = 1;
    private int _obstacleIdGen = 1;
    private int _flowFieldIdGen = 1;

    public MeshInstance _debugMesh;

    public override void _Ready()
    {
        base._Ready();

        _debugMesh = new MeshInstance();
        SpatialMaterial mat = new();
        mat.FlagsUnshaded = true;
        mat.VertexColorUseAsAlbedo = true;
        mat.VertexColorIsSrgb = true;
        _debugMesh.MaterialOverride = mat;
        AddChild(_debugMesh);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        // move area
        EdgeBounds.Position = Game.Player.GlobalPosition - EdgeBounds.Size * 0.5f;

        Span<Boid> boids = _boidPool.AsSpan();
        Span<Obstacle> obstacles = _obstaclePool.AsSpan(0, _numObstacles);
        Span<FlowField> flowFields = _flowFieldPool.AsSpan(0, _numFlowFields);
        
        foreach (ref FlowField flowField in flowFields)
        {
            flowField.Bounds.Position = flowField.Position - flowField.Size * 0.5f;
            flowField.Bounds.Size = flowField.Size;
            
            if (flowField.TrackID != 0)
            {
                flowField.Position = boids[_boidIdToIndex[flowField.TrackID]].Position;
            }
        }
        
        foreach (ref Boid boid in boids)
        {
            if (boid.Id == 0)
                continue;
            
            Vector2 totalForce = Vector2.Zero;
            
            // update target
            if (boid.TargetIndex != -1)
            {
                Boid targetBoid = boids[_boidIdToIndex[boid.TargetIndex]];
                Vector2 side = new(targetBoid.Heading.y, -targetBoid.Heading.x);
                boid.Target = targetBoid.Position - Utils.GlobaliseOffset(boid.TargetOffset, targetBoid.Heading, side);
            }

            for (int j = 0; j < (int) COUNT; j++)
            {
                if ((boid.Behaviours & (1 << j)) == 0)
                    continue;
                
                Vector2 force = CalculateSteeringForce((Behaviours) j, ref boid, boids, obstacles, flowFields, delta);

                // truncate by max force per unit time
                // https://gamedev.stackexchange.com/questions/173223/framerate-dependant-steering-behaviour
                float totalForceLength = totalForce.Length();
                float forceLength = force.Length();
                float frameMaxForce = boid.MaxForce * delta * 2.0f;
                if (totalForceLength + forceLength > frameMaxForce)
                {
                    force.Limit(frameMaxForce - totalForceLength);
                    totalForce += force;
                    break;
                }

                totalForce += force;
            }
            
            // adjust raw steering force
            totalForce = ApplyMinimumSpeed(boid, totalForce, boid.MinSpeed);
            
            // TODO: Add smooth steering.
            boid.Steering = totalForce;
            
            boid.Velocity += boid.Steering;
            boid.Velocity.Limit(boid.MaxSpeed);

            boid.Speed = boid.Velocity.Length();

            // Smooth heading to eliminate rapid heading changes on small velocity adjustments
            if (boid.Speed > boid.MaxSpeed * 0.025f)
            {
                const float smoothing = 0.9f;
                boid.Heading = boid.Velocity.Normalized() * (1.0f - smoothing) + boid.Heading * smoothing;
                boid.Heading = boid.Heading.Normalized();
            }

            boid.Position = WrapPosition(boid.Position + boid.Velocity * delta, EdgeBounds);
        }
        
        DrawSimulationToMesh(out Mesh mesh);
        _debugMesh.Mesh = mesh;
    }

    private static Vector2 CalculateSteeringForce(Behaviours behaviour, ref Boid boid, Span<Boid> boids, Span<Obstacle> obstacles, Span<FlowField> flowFields, float delta)
    {
        Vector2 force = Vector2.Zero;
        switch (behaviour)
        {
            case Cohesion:
                force += Steering_Cohesion(boid, boids, boid.ViewRange);
                break;
            case Alignment:
                force += Steering_Align(boid, boids, boid.ViewRange);
                break;
            case Separation:
                force += Steering_Separate(boid, boids, obstacles, delta);
                break;
            case Arrive:
                force += Steering_Arrive(boid);
                break;
            case Pursuit:
                force += Steering_Pursuit(boid);
                break;
            case Flee:
                break;
            case EdgeRepulsion:
                force += Steering_EdgeRepulsion(boid, EdgeBounds);
                break;
            case AvoidAllies:
                force += Steering_AvoidAllies(ref boid, boids);
                break;
            case AvoidEnemies:
                force += Steering_AvoidEnemies(ref boid, boids);
                break;
            case AvoidObstacles:
                force += Steering_AvoidObstacles(ref boid, obstacles);
                break;
            case MaintainSpeed:
                force += Steering_MaintainSpeed(boid);
                break;
            case Wander:
                force += Steering_Wander(ref boid, delta);
                break;
            case FlowFieldFollow:
                force += Steering_FlowFieldFollow(boid, flowFields);
                break;
            case COUNT:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return force.Limit(boid.MaxForce * delta) * boid.Weights[(int)behaviour];
    }

    public int RegisterBoid(Boid boid)
    {
        Debug.Assert(!_boidIdToIndex.ContainsKey(boid.Id), $"Boid with this ID ({boid.Id}) already registered.");
        if (_boidIdToIndex.ContainsKey(boid.Id))
            return -1;

        boid.Id = _boidIdGen++;
        int index = _boidPool.Add(boid);
        _boidIdToIndex[boid.Id] = index;
        return boid.Id;
    }

    public int RegisterObstacle(Obstacle obstacle)
    {
        Debug.Assert(!_obstacleIdToIndex.ContainsKey(obstacle.ID), $"Obstacle with this ID ({obstacle.ID}) already registered.");
        if (_obstacleIdToIndex.ContainsKey(obstacle.ID))
            return -1;
        
        obstacle.ID =  _obstacleIdGen++;
        _obstaclePool[_numObstacles++] = obstacle;
        _obstacleIdToIndex[obstacle.ID] = _numObstacles - 1;
        _obstacleIndexToId[_numObstacles - 1] = obstacle.ID;
        return obstacle.ID;
    }

    public int RegisterFlowField(FlowField flowField)
    {
        Debug.Assert(!_flowFieldIdToIndex.ContainsKey(flowField.ID), $"Obstacle with this ID ({flowField.ID}) already registered.");
        if (_flowFieldIdToIndex.ContainsKey(flowField.ID))
            return -1;

        flowField.ID = _flowFieldIdGen++;
        _flowFieldPool[_numFlowFields++] = flowField;
        _flowFieldIdToIndex[flowField.ID] = _numFlowFields - 1;
        _flowFieldIndexToId[_numFlowFields - 1] = flowField.ID;
        return flowField.ID;
    }

    public bool HasBoid(int id)
    {
        return _boidIdToIndex.ContainsKey(id);
    }

    public ref Boid GetBoid(int id)
    {
        Debug.Assert(_boidIdToIndex.ContainsKey(id), $"Boid doesn't exist.");
        return ref _boidPool.AsSpan()[_boidIdToIndex[id]];
    }

    public void RemoveBoid(int id)
    {
        Debug.Assert(HasBoid(id));
        int i = _boidIdToIndex[id];
        _boidPool.Remove(i);
        _boidIdToIndex.Remove(id);
    }
}
