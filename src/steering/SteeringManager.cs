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
        AvoidBoids,
        MaintainSpeed,
        Cohesion,
        Alignment,
        Arrive,
        Pursuit,
        Flee,
        EdgeRepulsion,
        Wander,
        COUNT,
    }

    public enum ObstacleShape
    {
        Circle,
    }

    public struct Boid
    {
        public bool Alive;
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
        public float ViewRange;
        public float ViewAngle;
        public Intersection Intersection;
        public float WanderAngle;
        public float WanderCircleDist;
        public float WanderCircleRadius;
        public float WanderVariance;
        public bool IgnoreAllyAvoidance;
    }

    public struct Obstacle
    {
        public int ID;
        public Vector2 Position;
        public ObstacleShape Shape;
        public float Size;
    }

    public struct Intersection
    {
        public bool Intersect;
        public float IntersectTime;
        public Vector2 SurfacePoint;
        public Vector2 SurfaceNormal;
    }

    public Rect2 EdgeBounds;

    private int _numBoids;
    private Boid[] _boidPool = new Boid[1000];
    private List<Obstacle> _obstacles = new(100);

    private System.Collections.Generic.Dictionary<int, int> _boidIdToIndex = new();
    private System.Collections.Generic.Dictionary<int, int> _boidIndexToId = new();
    private System.Collections.Generic.Dictionary<int, int> _obstacleIdToIndex = new();
    private System.Collections.Generic.Dictionary<int, int> _obstacleIndexToId = new();
    
    private int _boidIdGen;
    private int _obstacleIdGen;
    
    private List<Vector3> _vertList = new();
    private List<Color> _colList = new();
    private List<int> _indexList = new();

    public override void _Ready()
    {
        base._Ready();

        _vertList.Capacity = 10000;
        _colList.Capacity = 10000;
        _indexList.Capacity = 20000;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        Span<Boid> boids = _boidPool.AsSpan(0, _numBoids);
        foreach (ref Boid boid in boids)
        {
            if (!boid.Alive)
                continue;

            Vector2 totalForce = Vector2.Zero;

            for (int j = 0; j < (int) COUNT; j++)
            {
                if ((boid.Behaviours & (1 << j)) == 0)
                    continue;
                
                Vector2 force = CalculateSteeringForce((Behaviours) j, ref boid, boids, delta);

                // truncate by max force per unit time
                // https://gamedev.stackexchange.com/questions/173223/framerate-dependant-steering-behaviour
                float totalForceLength = totalForce.Length();
                float forceLength = force.Length();
                float frameMaxForce = boid.MaxForce * delta;
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
    }

    private Vector2 CalculateSteeringForce(Behaviours behaviour, ref Boid boid, Span<Boid> boids, float delta)
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
                force += Steering_Separate(boid, boids, _obstacles, delta);
                break;
            case Arrive:
                force += Steering_Arrive(boid, boid.Target);
                break;
            case Pursuit:
                force += Steering_Pursuit(boid, boid.Target);
                break;
            case Flee:
                break;
            case EdgeRepulsion:
                force += Steering_EdgeRepulsion(boid, EdgeBounds);
                break;
            case AvoidBoids:
                force += Steering_AvoidBoids(ref boid, boids);
                break;
            case AvoidObstacles:
                force += Steering_AvoidObstacles(ref boid, _obstacles);
                break;
            case MaintainSpeed:
                force += Steering_MaintainSpeed(boid);
                break;
            case Wander:
                force += Steering_Wander(ref boid, delta);
                break;
            case COUNT:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return force.Limit(boid.MaxForce * delta) * boid.Weights[(int)behaviour];
    }

    private void RegisterBoid(Boid boid)
    {
        Debug.Assert(!_boidIdToIndex.ContainsKey(boid.Id), $"Boid with this ID ({boid.Id}) already registered.");
        if (_boidIdToIndex.ContainsKey(boid.Id))
            return;
        
        _boidPool[_numBoids++] = boid;
        _boidIdToIndex[boid.Id] = _numBoids - 1;
        _boidIndexToId[_numBoids - 1] = boid.Id;
    }

    private void RegisterObstacle(Obstacle obstacle)
    {
        Debug.Assert(!_obstacleIdToIndex.ContainsKey(obstacle.ID), $"Obstacle with this ID ({obstacle.ID}) already registered.");
        if (_obstacleIdToIndex.ContainsKey(obstacle.ID))
            return;
        
        _obstacles.Add(obstacle);
        _obstacleIdToIndex[obstacle.ID] = _obstacles.Count - 1;
        _obstacleIndexToId[_obstacles.Count - 1] = obstacle.ID;
    }
    
    public int AddBoid(Boid boid)
    {
        boid.Id = _boidIdGen++;
        RegisterBoid(boid);
        return boid.Id;
    }

    public int AddObstacle(Vector2 pos, ObstacleShape shape, float size)
    {
        Obstacle obstacle = new()
        {
            ID = _obstacleIdGen,
            Position = pos,
            Shape = shape,
            Size = size
        };
        _obstacleIdGen++;
        RegisterObstacle(obstacle);
        return obstacle.ID;
    }

    public Boid GetBoid(int id)
    {
        Debug.Assert(_boidIdToIndex.ContainsKey(id), $"Boid with ID doesn't exist.");
        return _boidPool[_boidIdToIndex[id]];
    }
    
    public void SetBoid(Boid boid)
    {
        Debug.Assert(_boidIdToIndex.ContainsKey(boid.Id), $"Boid with ID doesn't exist.");
        _boidPool[_boidIdToIndex[boid.Id]] = boid;
    }

    // private int FindEmptyBoidIndex()
    // {
    //     for (int i = 0; i < _boids.Count; i++)
    //     {
    //         if (!_boids[i].Alive)
    //             return i;
    //     }
    //
    //     return -1;
    // }
}
