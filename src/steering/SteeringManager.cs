using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static SteeringManager.Behaviours;

public partial class SteeringManager : Singleton<SteeringManager>
{
    public enum Behaviours // in order of importance
    {
        Separation,
        Avoidance,
        Cohesion,
        Alignment,
        Arrive,
        Pursuit,
        Flee,
        EdgeRepulsion,
        MaintainSpeed,
        COUNT,
    }

    public enum ObstacleShape
    {
        Circle,
    }

    public struct Boid
    {
        public bool Alive;
        public int ID;
        public int Alignment;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Steering;
        public Vector2 Heading;
        public float Speed;
        public float DesiredSpeed;
        public float MaxSpeed;
        public float MaxForce;
        public float Radius;
        public float LookAhead;
        public int Behaviours;
        public float[] Weights;
        public Vector2 Target;
        public float ViewAngle;
        public Intersection Intersection;
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

    private List<Boid> _boids = new(1000);
    private List<Obstacle> _obstacles = new(100);

    private Dictionary<int, int> _boidIdToIndex = new();
    private Dictionary<int, int> _boidIndexToId = new();
    private Dictionary<int, int> _obstacleIdToIndex = new();
    private Dictionary<int, int> _obstacleIndexToId = new();
    
    private int _boidIdGen;
    private int _obstacleIdGen;

    public override void _Process(float delta)
    {
        base._Process(delta);

        for (int i = 0; i < _boids.Count; i++)
        {   
            Boid boid = _boids[i];
            if (!boid.Alive)
                continue;

            Vector2 totalForce = Vector2.Zero;

            for (int j = 0; j < (int) COUNT; j++)
            {
                if ((boid.Behaviours & (1 << j)) == 0)
                    continue;
                
                Vector2 force = CalculateSteeringForce((Behaviours) j, ref boid, delta);

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
            //totalForce = AdjustRawSteering(boid, totalForce, boid.MaxSpeed * 0.01f);
            
            // TODO: Add smooth steering.

            boid.Velocity += boid.Steering;
            boid.Velocity.Limit(boid.MaxSpeed);
            
            boid.Steering = totalForce;
            boid.Speed = boid.Velocity.Length();

            // Smooth heading to eliminate rapid heading changes on small velocity adjustments
            if (boid.Speed > boid.MaxSpeed * 0.025f)
            {
                const float smoothing = 0.9f;
                boid.Heading = boid.Velocity.Normalized() * (1.0f - smoothing) + boid.Heading * smoothing;
                boid.Heading = boid.Heading.Normalized();
            }

            boid.Position = WrapPosition(boid.Position + boid.Velocity * delta, EdgeBounds);

            _boids[i] = boid;
        }
    }

    private Vector2 CalculateSteeringForce(Behaviours behaviour, ref Boid boid, float delta)
    {
        Vector2 force = Vector2.Zero;
        switch (behaviour)
        {
            case Cohesion:
                force += Steering_Cohesion(boid, _boids, 50.0f);
                break;
            case Alignment:
                force += Steering_Align(boid, _boids, 50.0f);
                break;
            case Separation:
                force += Steering_Separate(boid, _boids, _obstacles, delta);
                break;
            case Arrive:
                force += Steering_Arrive(boid, boid.Target, 50.0f);
                break;
            case Pursuit:
                force += Steering_Pursuit(boid, boid.Target);
                break;
            case Flee:
                break;
            case EdgeRepulsion:
                force += Steering_EdgeRepulsion(boid, EdgeBounds);
                break;
            case Avoidance:
                force += Steering_Avoidance(ref boid, _boids, _obstacles);
                break;
            case MaintainSpeed:
                force += Steering_MaintainSpeed(boid);
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
        Debug.Assert(!_boidIdToIndex.ContainsKey(boid.ID), $"Boid with this ID ({boid.ID}) already registered.");
        if (_boidIdToIndex.ContainsKey(boid.ID))
            return;
        
        _boids.Add(boid);
        _boidIdToIndex[boid.ID] = _boids.Count - 1;
        _boidIndexToId[_boids.Count - 1] = boid.ID;
    }

    private void RegisterObstacle(Obstacle obstacle)
    {
        Debug.Assert(!_obstacleIdToIndex.ContainsKey(obstacle.ID), $"Obstacle with this ID ({obstacle.ID}) already registered.");
        if (_obstacleIdToIndex.ContainsKey(obstacle.ID))
            return;
        
        _obstacles.Add(obstacle);
        _obstacleIdToIndex[obstacle.ID] = _obstacles.Count - 1;
        _obstacleIndexToId[_boids.Count - 1] = obstacle.ID;
    }
    
    public int AddBoid(Vector2 pos, Vector2 vel, float radius, float maxSpeed, float maxForce, 
        int behaviours, float[] weights, Vector2 target, float fov, int alignment, float desiredSpeed = 0.0f)
    {
        Boid boid = new()
        {
            Alive = true,
            ID = _boidIdGen,
            Alignment = alignment,
            Position = pos,
            Steering = Vector2.Zero,
            Velocity = vel,
            Radius = radius,
            Heading = Vector2.Up,
            DesiredSpeed = desiredSpeed,
            MaxSpeed = maxSpeed,
            MaxForce = maxForce,
            LookAhead = 0.5f,
            Behaviours = behaviours,
            Weights = weights,
            Target = target,
            ViewAngle = fov,
        };
        _boidIdGen++;
        RegisterBoid(boid);
        return boid.ID;
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
        return _boids[_boidIdToIndex[id]];
    }
    
    public void SetBoid(Boid boid)
    {
        Debug.Assert(_boidIdToIndex.ContainsKey(boid.ID), $"Boid with ID doesn't exist.");
        _boids[_boidIdToIndex[boid.ID]] = boid;
    }

    private int FindEmptyBoidIndex()
    {
        for (int i = 0; i < _boids.Count; i++)
        {
            if (!_boids[i].Alive)
                return i;
        }

        return -1;
    }
}
