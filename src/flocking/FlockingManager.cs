using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using Array = Godot.Collections.Array;

public partial class FlockingManager : Singleton<FlockingManager>
{
    public enum Behaviours
    {
        Separation,
        Avoidance,
        Cohesion,
        Alignment,
        Arrive,
        Pursuit,
        Flee,
        EdgeRepulsion,
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
        public float SeparationRadius;
        public float MaxSpeed;
        public float MaxForce;
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
        public float Range;
        public float Distance;
        public Vector2 SurfacePoint;
        public Vector2 SurfaceNormal;
        public bool BoidOutside;
    }

    public Rect2 EdgeBounds;

    private List<Boid> _boids = new(1000);
    private List<Obstacle> _obstacles = new List<Obstacle>(100);
    private Dictionary<BoidBase, int> _boidToIndex = new();
    private Dictionary<int, BoidBase> _indexToBoid = new();
    
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

            for (int j = 0; j < (int) Behaviours.COUNT; j++)
            {
                if ((boid.Behaviours & (1 << j)) == 0)
                    continue;
                
                Behaviours behaviour = (Behaviours) j;
                Vector2 force = Vector2.Zero;
                switch (behaviour)
                {
                    case Behaviours.Cohesion:
                        force += Steering_Cohesion(i, delta, 50.0f) * boid.Weights[(int) Behaviours.Cohesion];
                        break;
                    case Behaviours.Alignment:
                        force += Steering_Align(i, delta, 50.0f) * boid.Weights[(int) Behaviours.Alignment];
                        break;
                    case Behaviours.Separation:
                        force += Steering_Separate(i, delta) * boid.Weights[(int) Behaviours.Separation];
                        break;
                    case Behaviours.Arrive:
                        force += Steering_Arrive(boid, delta, boid.Target, 50.0f) * boid.Weights[(int) Behaviours.Arrive];
                        break;
                    case Behaviours.Pursuit:
                        force += Steering_Pursuit(boid, delta, boid.Target) * boid.Weights[(int) Behaviours.Pursuit];
                        break;
                    case Behaviours.Flee:
                        break;
                    case Behaviours.EdgeRepulsion:
                        force += Steering_EdgeRepulsion(boid, delta, EdgeBounds, boid.Weights[(int) Behaviours.EdgeRepulsion]);
                        break;
                    case Behaviours.Avoidance:
                        force += Steering_Avoidance(ref boid, delta, boid.Weights[(int) Behaviours.EdgeRepulsion]);
                        break;
                    case Behaviours.COUNT:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

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

            if (boid.Speed > boid.MaxSpeed * 0.025f)
            {
                const float smoothing = 0.9f;
                boid.Heading = boid.Velocity.Normalized() * (1.0f - smoothing) + boid.Heading * smoothing;
                boid.Heading = boid.Heading.Normalized();
            }

            boid.Position = WrapPosition(boid.Position + boid.Velocity * delta);

            _boids[i] = boid;
        }

//         for (int i = 0; i < _boids.Count; i++)
//         {
//             Boid boid = _boids[i];
//             if (!boid.Alive)
//                 continue;
//
//             BoidBase boidObj = _indexToBoid[i];
//
//             boid.Position += boid.Velocity * delta;
//             _boids[i] = boid;
//
//             boidObj.Velocity = boid.Velocity;
// #if TOOLS
//             boidObj.Steering = boid.Steering;
// #endif
//
//             boidObj.GlobalTransform = new Transform(Basis.Identity, boid.Position.To3D());
//             boidObj.Rotation = new Vector3(0.0f, -Mathf.Atan2(boid.Velocity.x, -boid.Velocity.y), 0.0f);
//         }
    }
    
    private Vector2 AdjustRawSteering(Boid boid, Vector2 force, float minSpeed)
    {
        float speed = boid.Velocity.Length();
        if (boid.Velocity.Length() > minSpeed || force == Vector2.Zero)
        {
            return force;
        }
        else
        {
            float range = speed / minSpeed;
            float cosine = Mathf.Lerp(1.0f, -1.0f, Mathf.Pow(range, 50));
            return Utils.VecLimitDeviationAngleUtility(true, force, cosine, boid.Velocity.Normalized());
        }
    }

    private Vector2 WrapPosition(Vector2 pos)
    {
        pos -= EdgeBounds.Position;
        pos += EdgeBounds.Size;
        pos.x %= EdgeBounds.Size.x;
        pos.y %= EdgeBounds.Size.y;
        pos += EdgeBounds.Position;
        return pos;
    }

    // public void AddBoid(BoidBase boidObj, Vector2 vel)
    // {
    //     if (_boidToIndex.TryGetValue(boidObj, out int _))
    //     {
    //         UpdateBoid(boidObj);
    //         return;
    //     }
    //
    //     int i = FindEmptyBoidIndex();
    //     if (i == -1)
    //     {
    //         _boids.Add(NewBoid(boidObj, vel));
    //         i = _boids.Count - 1;
    //     }
    //
    //     _boids[i] = NewBoid(boidObj, vel);
    //     _boidToIndex[boidObj] = i;
    //     _indexToBoid[i] = boidObj;
    // }
    //
    // public void RemoveBoid(BoidBase boidObj)
    // {
    //     if (!_boidToIndex.TryGetValue(boidObj, out int i))
    //         return;
    //
    //     _boids[i] = new Boid
    //     {
    //         Alive = false
    //     };
    //     _boidToIndex.Remove(boidObj);
    //     _indexToBoid.Remove(i);
    // }
    //
    // public void UpdateBoid(BoidBase boidObj)
    // {
    //     if (!_boidToIndex.TryGetValue(boidObj, out int i))
    //         return;
    //
    //     Boid boid = NewBoid(boidObj, _boids[i].Velocity);
    //     boid.WanderAngle = _boids[i].WanderAngle;
    //     _boids[i] = boid;
    // }

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
    
    public int AddBoid(Vector2 pos, Vector2 vel, float maxSpeed, float maxForce, 
        int behaviours, float[] weights, Vector2 target, float fov, int alignment)
    {
        Boid boid = new()
        {
            Alive = true,
            ID = _boidIdGen,
            Alignment = alignment,
            Position = pos,
            Steering = Vector2.Zero,
            Velocity = vel,
            SeparationRadius = 10.0f,
            Heading = Vector2.Up,
            MaxSpeed = maxSpeed,
            MaxForce = maxForce,
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

    private bool InView(Boid boid, Boid other, float viewDegrees)
    {
        Vector2 toOther = other.Position - boid.Position;
        Vector2 viewDir = Vector2.Up;
        if (boid.Velocity != Vector2.Zero)
            viewDir = boid.Velocity.Normalized();
        float angleRad = viewDir.AngleTo(toOther);
        return Mathf.Abs(angleRad) < Mathf.Deg2Rad(viewDegrees * 0.5f);
    }

    private static Vector2 Steering_Seek(Boid boid, float delta, Vector2 position)
    {
        Vector2 desired = position - boid.Position;
        desired *= boid.MaxSpeed / desired.Length();

        Vector2 force = desired - boid.Velocity;
        return force.SetMag(boid.MaxForce * delta);
    }
    
    private static Vector2 Steering_Arrive(Boid boid, float delta, Vector2 position, float radius)
    {
        float dist = (boid.Position - position).Length();
        if (dist > radius)
        {
            return Steering_Seek(boid, delta, position);
        }
        Vector2 desired = position - boid.Position;
        desired.SetMag(boid.MaxSpeed * (dist / radius));
        Vector2 force = desired - boid.Velocity;
        return force.Limit(boid.MaxForce * delta);
    }

    private Vector2 Steering_Cohesion(int i, float delta, float radius)
    {
        Boid boid = _boids[i];
        Vector2 centre = Vector2.Zero;
        int count = 0;
        for (int j = 0; j < _boids.Count; j++)
        {
            Boid other = _boids[j];
            if (i == j) continue;
            if ((boid.Position - other.Position).LengthSquared() > radius * radius) continue;
            if (!InView(boid, other, boid.ViewAngle)) continue;

            centre += other.Position;
            count++;
        }

        if (count == 0)
            return Vector2.Zero;

        centre /= count;
        return Steering_Seek(boid, delta, centre);
    }

    private Vector2 Steering_Align(int i, float delta, float radius)
    {
        Boid boid = _boids[i];
        Vector2 desired = Vector2.Zero;
        int count = 0;
        for (int j = 0; j < _boids.Count; j++)
        {
            Boid other = _boids[j];
            if (i == j) continue;
            if ((boid.Position - other.Position).LengthSquared() > radius * radius) continue;
            if (!InView(boid, other, boid.ViewAngle)) continue;

            desired += other.Velocity;
            count++;
        }

        if (count == 0)
            return Vector2.Zero;

        desired /= count;
        Vector2 force = desired - boid.Velocity;
        return force.Limit(boid.MaxForce * delta);
    }

    private Vector2 Steering_Separate(int i, float delta)
    {
        Boid boid = _boids[i];
        
        Vector2 forceSum = Vector2.Zero;
        int count = 0;
        for (int j = 0; j < _boids.Count; j++)
        {
            if (i == j) continue;
            Boid other = _boids[j];
            float dist = (boid.Position - other.Position).Length();
            float radius = boid.SeparationRadius + other.SeparationRadius;
            if (dist > radius)
                continue;

            Vector2 desired = boid.Position - other.Position;
            desired.SetMag(boid.MaxSpeed);
            float t = 1.0f - Mathf.Pow(dist / radius, 3.0f);
            Vector2 force = desired.Limit(boid.MaxForce * delta * t);
            forceSum += force;
            count++;
        }   
        
        if (count == 0)
            return Vector2.Zero;

        return forceSum.Limit(boid.MaxForce * delta);
    }

    private Vector2 Steering_Pursuit(Boid boid, float delta, Vector2 position)
    {
        return Steering_Seek(boid, delta, position);
    }
    
    private Vector2 Steering_EdgeRepulsion(Boid boid, float delta, Rect2 bounds, float weight)
    {
        if (bounds.HasPoint(boid.Position))
            return Vector2.Zero;

        Vector2 closestPointOnEdge = new(Mathf.Max(Mathf.Min(boid.Position.x, bounds.End.x), bounds.Position.x),
            Mathf.Max(Mathf.Min(boid.Position.y, bounds.End.y), bounds.Position.y));

        return Steering_Seek(boid, delta, closestPointOnEdge) * weight;
    }
    
    private Vector2 Steering_Avoidance(ref Boid boid, float delta, float weight)
    {
        // look-ahead range is proportional to its ability to steer away.
        float range = boid.Speed * 100.0f / boid.MaxForce + boid.SeparationRadius;
        
        Intersection intersection = default;
        float nearestDistance = 999999.0f;
        foreach (Obstacle obstacle in _obstacles)
        {
            switch (obstacle.Shape)
            {
                case ObstacleShape.Circle:
                {
                    Intersection intersectionTest = CircleIntersection(boid, obstacle, range);
                    
                    float dist = (intersectionTest.SurfacePoint - boid.Position).LengthSquared();
                    if (intersectionTest.Intersect && (intersectionTest.SurfacePoint - boid.Position).LengthSquared() < nearestDistance)
                    {
                        nearestDistance = dist;
                        intersection = intersectionTest;
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        // if nearby intersection found, steer away from it, otherwise no steering
        boid.Intersection = intersection;
        boid.Intersection.Range = range;
        if (intersection.Intersect)
        {
            Vector2 lateral = intersection.SurfaceNormal.PerpendicularComponent(boid.Heading);
            return lateral.Normalized() * boid.MaxForce * delta;
        }
        return Vector2.Zero;
    }
}
