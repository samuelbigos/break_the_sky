using Godot;
using System;
using System.Collections.Generic;

public class FlockingManager : Node
{
    public static FlockingManager Instance;
    
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
        public float FOV;
        public int Behaviours;
        public float[] Weights;
        public Vector2 Target;
    }

    public float MaxSpeed = 100.0f;

    private List<Boid> _boids = new(1000);
    private Dictionary<BoidBase, int> _boidToIndex = new(); 
    private Dictionary<int, BoidBase> _indexToBoid = new(); 
    
    public override void _Ready()
    {
        base._Ready();

        Instance = this;
    }
    
    public override void _Process(float delta)
    {
        base._Process(delta);

        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];

            Vector2 steering = Vector2.Zero;
            
            if ((boid.Behaviours & (int) Behaviours.Flock) != 0)
                steering += Steering_Flock(boid, 50.0f, boid.Weights[(int) Behaviours.Flock]);
            
            if ((boid.Behaviours & (int) Behaviours.Align) != 0)
                steering += Steering_Align(boid, 50.0f, boid.Weights[(int) Behaviours.Align]);
            
            if ((boid.Behaviours & (int) Behaviours.Separate) != 0)
                steering += Steering_Separate(boid, 10.0f, boid.Weights[(int) Behaviours.Separate]);
            
            if ((boid.Behaviours & (int) Behaviours.Arrive) != 0)
                steering += Steering_Arrive(boid, boid.Target, 50.0f, boid.Weights[(int) Behaviours.Arrive]);

            float speed = steering.Length();
            if (speed > MaxSpeed)
            {
                steering /= speed;
                steering *= MaxSpeed;
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
        _boids.Add(NewBoid(boidObj));
        int i = _boids.Count - 1;
        _boidToIndex[boidObj] = i;
        _indexToBoid[i] = boidObj;
    }

    public void RemoveBoid(BoidBase boidObj)
    {
        int i = _boidToIndex[boidObj];
        _boids.RemoveAt(i);
        _boidToIndex.Remove(boidObj);
        _indexToBoid.Remove(i);
    }

    public void UpdateBoid(BoidBase boid)
    {
        int index = _boidToIndex[boid];
        _boids[index] = NewBoid(boid);
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
            FOV = -0.5f,
            Behaviours = boid.Behaviours,
            Weights = boid.BehaviourWeights,
            Target = boid.TargetPos,
        };
    }

    private bool InFoV(Boid boid, Boid other)
    {
        return boid.Velocity.Normalized().Dot((boid.Position - other.Position).Normalized()) > boid.FOV;
    }

    private Vector2 Steering_Flock(Boid boid, float radius, float weight)
    {
        Vector2 centre = Vector2.Zero;
        int count = 0;
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
        Vector2 meanVel = Vector2.Zero;;
        foreach (Boid other in _boids)
        {
            if ((boid.Position - other.Position).LengthSquared() > radius * radius
                || !InFoV(boid, other))
                continue;

            meanVel += other.Velocity;
        }
        
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
