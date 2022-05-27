using Godot;
using System;
using System.Collections.Generic;

public class FlockingManager : Node
{
    public static FlockingManager Instance;
    
    private struct Boid
    {
        public Vector2 Position;
        public Vector2 Velocity;
#if TOOLS
        public Vector2 Steering;
#endif
    }

    public Vector2 FlockPosition;
    public float MaxSpeed = 50.0f;

    private List<Boid> _boids = new List<Boid>(1000);
    private Dictionary<BoidBase, int> _boidToIndex = new Dictionary<BoidBase, int>(); 
    private Dictionary<int, BoidBase> _indexToBoid = new Dictionary<int, BoidBase>(); 
    
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
            steering += Steering_Flock(boid, 50.0f, 0.3f);
            steering += Steering_Align(boid, 50.0f, 0.5f);
            steering += Steering_Avoid(boid, 10.0f, 1.0f);
            steering *= 1.0f;
            
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
        _boids.Add(new Boid());
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

    private Vector2 Steering_Flock(Boid boid, float radius, float weight)
    {
        Vector2 deltaCenter = FlockPosition - boid.Position;
        return (deltaCenter * weight);
    }
    
    private Vector2 Steering_Align(Boid boid, float radius, float weight)
    {
        Vector2 meanVel = Vector2.Zero;;
        foreach (Boid other in _boids)
        {
            if ((boid.Position - other.Position).LengthSquared() > radius * radius)
                continue;

            meanVel += other.Velocity;
        }
        
        Vector2 dVel = meanVel - boid.Velocity;
        return dVel * weight;
    }

    private Vector2 Steering_Avoid(Boid boid, float radius, float weight)
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
}
