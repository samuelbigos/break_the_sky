using Godot;
using System;
using System.Collections.Generic;
using System.Xml;

public class FlockingManager : Singleton<FlockingManager>
{
    public enum Behaviours
    {
        Cohesion,
        Alignment,
        Separation,
        Arrive,
        Pursuit,
        Flee,
        Wander,
        EdgeRepulsion,
        COUNT,
    }

    private struct Boid
    {
        public bool Alive;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Damping;
#if TOOLS
        public Vector2 Steering;
#endif
        public float WanderAngle;
        public float MaxSpeed;
        public float MaxForce;
        public int Behaviours;
        public float[] Weights;
        public Vector2 Target;
        public float ViewAngle;
    }

    private List<Boid> _boids = new(1000);
    private Dictionary<BoidBase, int> _boidToIndex = new();
    private Dictionary<int, BoidBase> _indexToBoid = new();

    public override void _Process(float delta)
    {
        base._Process(delta);

        Rect2 edgeBounds = Game.Instance.SpawningRect;

        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];
            if (!boid.Alive)
                continue;

            Vector2 steering = Vector2.Zero;

            if ((boid.Behaviours & (1 << (int) Behaviours.Cohesion)) != 0)
                steering += Steering_Cohesion(i, 50.0f, boid.Weights[(int) Behaviours.Cohesion]);

            if ((boid.Behaviours & (1 << (int) Behaviours.Alignment)) != 0)
                steering += Steering_Align(i, 50.0f, boid.Weights[(int) Behaviours.Alignment]);

            if ((boid.Behaviours & (1 << (int) Behaviours.Arrive)) != 0)
                steering += Steering_Arrive(boid, boid.Target, 50.0f, boid.Weights[(int) Behaviours.Arrive]);

            if ((boid.Behaviours & (1 << (int) Behaviours.Pursuit)) != 0)
                steering += Steering_Pursuit(boid, boid.Target, boid.Weights[(int) Behaviours.Pursuit]);

            if ((boid.Behaviours & (1 << (int) Behaviours.Wander)) != 0)
                steering += Steering_Wander(ref boid, boid.Weights[(int) Behaviours.Wander]);

            if ((boid.Behaviours & (1 << (int) Behaviours.EdgeRepulsion)) != 0)
                steering += Steering_EdgeRepulsion(boid, edgeBounds, boid.Weights[(int) Behaviours.EdgeRepulsion]);

            if ((boid.Behaviours & (1 << (int) Behaviours.Separation)) != 0)
                steering += Steering_Separate(i, 10.0f, boid.Weights[(int) Behaviours.Separation]);
            
            steering = steering.Truncate(boid.MaxForce);

            boid.Velocity = (boid.Velocity + steering).Truncate(boid.MaxSpeed);
            boid.Velocity *= Mathf.Pow(1.0f - Mathf.Clamp(boid.Damping, 0.0f, 1.0f), delta * 60.0f); // damping
#if TOOLS
            boid.Steering = steering;
#endif

            _boids[i] = boid;
        }

        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];
            if (!boid.Alive)
                continue;

            BoidBase boidObj = _indexToBoid[i];

            boid.Position += boid.Velocity * delta;
            _boids[i] = boid;

            boidObj.Velocity = boid.Velocity;
#if TOOLS
            boidObj.Steering = boid.Steering;
#endif

            boidObj.GlobalTransform = new Transform(Basis.Identity, boid.Position.To3D());
            boidObj.Rotation = new Vector3(0.0f, -Mathf.Atan2(boid.Velocity.x, -boid.Velocity.y), 0.0f);
        }
    }

    public void AddBoid(BoidBase boidObj, Vector2 vel)
    {
        if (_boidToIndex.TryGetValue(boidObj, out int _))
        {
            UpdateBoid(boidObj);
            return;
        }

        int i = FindEmptyBoidIndex();
        if (i == -1)
        {
            _boids.Add(NewBoid(boidObj, vel));
            i = _boids.Count - 1;
        }

        _boids[i] = NewBoid(boidObj, vel);
        _boidToIndex[boidObj] = i;
        _indexToBoid[i] = boidObj;
    }

    public void RemoveBoid(BoidBase boidObj)
    {
        if (!_boidToIndex.TryGetValue(boidObj, out int i))
            return;

        _boids[i] = new Boid
        {
            Alive = false
        };
        _boidToIndex.Remove(boidObj);
        _indexToBoid.Remove(i);
    }

    public void UpdateBoid(BoidBase boidObj)
    {
        if (!_boidToIndex.TryGetValue(boidObj, out int i))
            return;

        Boid boid = NewBoid(boidObj, _boids[i].Velocity);
        boid.WanderAngle = _boids[i].WanderAngle;
        _boids[i] = boid;
    }

    private Boid NewBoid(BoidBase boid, Vector2 vel)
    {
        return new Boid()
        {
            Alive = true,
            Position = boid.GlobalTransform.origin.To2D(),
#if TOOLS
            Steering = Vector2.Zero,
#endif
            Velocity = vel,
            Damping = boid.Damping,
            WanderAngle = Utils.Rng.Randf() * Mathf.Pi * 2.0f,
            MaxSpeed = boid.MaxVelocity,
            MaxForce = 1.0f,
            Behaviours = boid.Behaviours,
            Weights = boid.BehaviourWeights,
            Target = boid.TargetPos,
            ViewAngle = boid.FieldOfView,
        };
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

    private Vector2 Steering_Seek(Boid boid, Vector2 position)
    {
        Vector2 desired = (position - boid.Position);
        desired *= boid.MaxSpeed / desired.Length();

        Vector2 force = desired - boid.Velocity;
        return force * (boid.MaxForce / boid.MaxSpeed);
    }

    private Vector2 Steering_Cohesion(int i, float radius, float weight)
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
        return Steering_Seek(boid, centre) * weight;
    }

    private Vector2 Steering_Align(int i, float radius, float weight)
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
        return (desired - boid.Velocity).Truncate(boid.MaxForce) * weight;
    }

    private Vector2 Steering_Separate(int i, float radius, float weight)
    {
        Boid boid = _boids[i];

        // Vector2 totalForce = Vector2.Zero;
        // int count = 0;
        // for (int j = 0; j < _boids.Count; j++)
        // {
        //     if (i == j) continue;
        //     Boid other = _boids[j];
        //     float dist = (boid.Position - other.Position).Length();
        //     if (dist > radius)
        //         continue;
        //
        //     Vector2 pushForce = (boid.Position - other.Position);
        //     totalForce += Vector2.One - (pushForce / radius);
        // }
        //
        // if (count == 0)
        //     return Vector2.Zero;
        //
        // totalForce /= count;
        // return totalForce * boid.MaxForce;

        Vector2 desiredSum = Vector2.Zero;
        int count = 0;
        for (int j = 0; j < _boids.Count; j++)
        {
            if (i == j) continue;
            Boid other = _boids[j];
            float dist = (boid.Position - other.Position).Length();
            if (dist > radius)
                continue;
        
            Vector2 desired = (boid.Position - other.Position).Normalized() * boid.MaxSpeed * (dist / radius);
            desiredSum += desired;
            count++;
        }   
        
        if (count == 0)
            return Vector2.Zero;
        
        return (desiredSum - boid.Velocity) * weight;
    }
    
    private Vector2 Steering_Arrive(Boid boid, Vector2 position, float radius, float weight)
    {
        float dist = (boid.Position - position).Length();
        if (dist > radius)
        {
            return Steering_Seek(boid, position) * weight;
        }

        Vector2 desired = position - boid.Position;
        desired = desired.Normalized() * boid.MaxSpeed * (dist / radius);
        return (desired - boid.Velocity) * weight;
    }
    
    private Vector2 Steering_Pursuit(Boid boid, Vector2 position, float weight)
    {
        return Steering_Seek(boid, position) * weight;
    }
    
    private Vector2 Steering_Wander(ref Boid boid, float weight)
    {
        Vector2 circleCentre = (boid.Velocity == Vector2.Zero ? Vector2.Up : boid.Velocity.Normalized()) * 5.0f;
        float circleRadius = 5.0f;
        Vector2 displacement = new Vector2(Mathf.Cos(boid.WanderAngle), Mathf.Sin(boid.WanderAngle)).Normalized() * circleRadius;
        boid.WanderAngle += (Utils.Rng.Randf() - 0.5f) * TimeSystem.Delta * 100.0f;
        
        Vector2 desired = circleCentre + displacement;
        return desired * weight;
    }
    
    private Vector2 Steering_EdgeRepulsion(Boid boid, Rect2 bounds, float weight)
    {
        if (bounds.HasPoint(boid.Position))
            return Vector2.Zero;

        Vector2 closestPointOnEdge = new Vector2(Mathf.Max(Mathf.Min(boid.Position.x, bounds.End.x), bounds.Position.x),
            Mathf.Max(Mathf.Min(boid.Position.y, bounds.End.y), bounds.Position.y));

        return Steering_Seek(boid, closestPointOnEdge) * weight;
    }
}
