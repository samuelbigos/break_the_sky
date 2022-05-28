using Godot;
using System;
using System.Collections.Generic;

public class FlockingManager : Singleton<FlockingManager>
{
    public enum Behaviours
    {
        Flock,
        Align,
        Separate,
        Arrive,
        Pursuit,
        Flee,
        COUNT,
    }
    
    private struct Boid
    {
        public Vector2 Position;
        public Vector2 Velocity;
#if TOOLS
        public Vector2 Steering;
#endif
        public float MaxSpeed;
        public float FOV;
        public int Behaviours;
        public float[] Weights;
        public Vector2 Target;
    }

    private List<Boid> _boids = new(1000);
    private Dictionary<BoidBase, int> _boidToIndex = new(); 
    private Dictionary<int, BoidBase> _indexToBoid = new();
    
    public override void _Process(float delta)
    {
        base._Process(delta);

        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];

            Vector2 steering = Vector2.Zero;
            
            if ((boid.Behaviours & (1 << (int) Behaviours.Flock)) != 0)
                steering += Steering_Flock(boid, 50.0f, boid.Weights[(int) Behaviours.Flock]);
            
            if ((boid.Behaviours & (1 << (int) Behaviours.Align)) != 0)
                steering += Steering_Align(boid, 50.0f, boid.Weights[(int) Behaviours.Align]);
            
            if ((boid.Behaviours & (1 << (int) Behaviours.Separate)) != 0)
                steering += Steering_Separate(boid, 10.0f, boid.Weights[(int) Behaviours.Separate]);
            
            if ((boid.Behaviours & (1 << (int) Behaviours.Arrive)) != 0)
                steering += Steering_Arrive(boid, boid.Target, 50.0f, boid.Weights[(int) Behaviours.Arrive]);

            float speed = steering.Length();
            if (speed > boid.MaxSpeed)
            {
                steering /= speed;
                steering *= boid.MaxSpeed;
            }
            boid.Velocity = steering;
#if TOOLS
            boid.Steering = steering;
#endif

            _boids[i] = boid;
        }

        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];
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

    public void AddBoid(BoidBase boidObj)
    {
        if (_boidToIndex.TryGetValue(boidObj, out int _))
        {
            UpdateBoid(boidObj);
            return;
        }
        
        _boids.Add(NewBoid(boidObj));
        int i = _boids.Count - 1;
        _boidToIndex[boidObj] = i;
        _indexToBoid[i] = boidObj;
    }

    public void RemoveBoid(BoidBase boidObj)
    {
        if (!_boidToIndex.TryGetValue(boidObj, out int i))
            return;
        
        _boids.RemoveAt(i);
        _boidToIndex.Remove(boidObj);
        _indexToBoid.Remove(i);
    }

    public void UpdateBoid(BoidBase boidObj)
    {
        if (!_boidToIndex.TryGetValue(boidObj, out int i))
            return;
        
        _boids[i] = NewBoid(boidObj);
    }

    private Boid NewBoid(BoidBase boid)
    {
        return new Boid()
        {
            Position = boid.GlobalTransform.origin.To2D(),
#if TOOLS
            Steering = Vector2.Zero,
#endif
            Velocity = Vector2.Zero,
            MaxSpeed = boid.MaxVelocity,
            Behaviours = boid.Behaviours,
            Weights = boid.BehaviourWeights,
            Target = boid.TargetPos,
        };
    }

    private Vector2 Steering_Flock(Boid boid, float radius, float weight)
    {
        Vector2 centre = boid.Position;
        int count = 1;
        foreach (Boid other in _boids)
        {
            if ((boid.Position - other.Position).LengthSquared() > radius * radius)
                continue;

            centre += other.Position;
            count++;
        }

        centre /= count;
        return (centre - boid.Position) * weight;
    }
    
    private Vector2 Steering_Align(Boid boid, float radius, float weight)
    {
        Vector2 meanVel = Vector2.Zero;
        int count = 0;
        foreach (Boid other in _boids)
        {
            if ((boid.Position - other.Position).LengthSquared() > radius * radius)
                continue;

            meanVel += other.Velocity;
            count++;
        }

        meanVel /= count;
        Vector2 dVel = meanVel - boid.Velocity;
        return dVel * weight;
    }

    private Vector2 Steering_Separate(Boid boid, float radius, float weight)
    {
        Vector2 sumCloseness = Vector2.Zero;;
        foreach (Boid other in _boids)
        {
            if ((boid.Position - other.Position).LengthSquared() > radius * radius)
                continue;

            float closeness = radius - (boid.Position - other.Position).Length();
            sumCloseness += (boid.Position - other.Position) * closeness;
        }

        return sumCloseness * weight;
    }
    
    private Vector2 Steering_Arrive(Boid boid, Vector2 position, float radius, float weight)
    {
        float dist = (boid.Position - position).Length();
        if (dist < radius)
        {
            float stopPercent = 0.5f;
            float t = (dist - stopPercent) / (radius - stopPercent);
            return (position - boid.Position) * t * weight;
        }
        return (position - boid.Position) * weight;
    }
}
