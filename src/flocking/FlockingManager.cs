using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using Array = Godot.Collections.Array;

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
        EdgeRepulsion,
        COUNT,
    }

    public struct Boid
    {
        public bool Alive;
        public int ID;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Steering;
        public Vector2 Heading;
        public float MaxSpeed;
        public float MaxForce;
        public int Behaviours;
        public float[] Weights;
        public Vector2 Target;
        public float ViewAngle;
    }

    public Rect2 EdgeBounds;

    private List<Boid> _boids = new(1000);
    private Dictionary<BoidBase, int> _boidToIndex = new();
    private Dictionary<int, BoidBase> _indexToBoid = new();
    
    private Dictionary<int, int> _idToIndex = new();
    private Dictionary<int, int> _indexToId = new();
    private int _idGen;

    public override void _Process(float delta)
    {
        base._Process(delta);

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
                steering += Steering_Arrive(boid, boid.Target, 50.0f) * boid.Weights[(int) Behaviours.Arrive];

            if ((boid.Behaviours & (1 << (int) Behaviours.Pursuit)) != 0)
                steering += Steering_Pursuit(boid, boid.Target, boid.Weights[(int) Behaviours.Pursuit]);

            if ((boid.Behaviours & (1 << (int) Behaviours.EdgeRepulsion)) != 0)
                steering += Steering_EdgeRepulsion(boid, EdgeBounds, boid.Weights[(int) Behaviours.EdgeRepulsion]);

            if ((boid.Behaviours & (1 << (int) Behaviours.Separation)) != 0)
                steering += Steering_Separate(i, 10.0f) * boid.Weights[(int) Behaviours.Separation];
            
            // truncate by max force per unit time
            // https://gamedev.stackexchange.com/questions/173223/framerate-dependant-steering-behaviour
            steering.Limit(boid.MaxForce * delta);

            boid.Velocity += steering;
            boid.Velocity.Limit(boid.MaxSpeed);
            
            boid.Steering = steering;
            if (boid.Velocity != Vector2.Zero)
                boid.Heading = boid.Velocity.Normalized();

            boid.Position += boid.Velocity * delta;

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
        Debug.Assert(!_idToIndex.ContainsKey(boid.ID), $"Boid with this ID ({boid.ID}) already registered.");
        if (_idToIndex.ContainsKey(boid.ID))
            return;
        
        _boids.Add(boid);
        _idToIndex[boid.ID] = _boids.Count - 1;
        _indexToId[_boids.Count - 1] = boid.ID;
    }

    public int AddBoid(Vector2 pos, Vector2 vel, float maxSpeed, float maxForce, int behaviours, float[] weights, Vector2 target, float fov)
    {
        Boid boid = new()
        {
            Alive = true,
            ID = _idGen,
            Position = pos,
            Steering = Vector2.Zero,
            Velocity = vel,
            Heading = Vector2.Up,
            MaxSpeed = maxSpeed,
            MaxForce = maxForce,
            Behaviours = behaviours,
            Weights = weights,
            Target = target,
            ViewAngle = fov,
        };
        _idGen++;
        RegisterBoid(boid);
        return boid.ID;
    }

    public Boid GetBoid(int id)
    {
        Debug.Assert(_idToIndex.ContainsKey(id), $"Boid with ID doesn't exist.");
        return _boids[_idToIndex[id]];
    }
    
    public void SetBoid(Boid boid)
    {
        Debug.Assert(_idToIndex.ContainsKey(boid.ID), $"Boid with ID doesn't exist.");
        _boids[_idToIndex[boid.ID]] = boid;
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

    private static Vector2 Steering_Seek(Boid boid, Vector2 position)
    {
        Vector2 desired = position - boid.Position;
        desired *= boid.MaxSpeed / desired.Length();

        Vector2 force = desired - boid.Velocity;
        return force.Normalized() * boid.MaxForce;
    }
    
    private static Vector2 Steering_Arrive(Boid boid, Vector2 position, float radius)
    {
        float dist = (boid.Position - position).Length();
        if (dist > radius)
        {
            return Steering_Seek(boid, position);
        }
        Vector2 desired = position - boid.Position;
        desired.SetMag(boid.MaxSpeed * (dist / radius));
        Vector2 force = desired - boid.Velocity;
        return force.Limit(boid.MaxForce);
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
        Vector2 force = desired - boid.Velocity;
        return force.Limit(boid.MaxForce) * weight;
    }

    private Vector2 Steering_Separate(int i, float radius)
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

        Vector2 force = desiredSum - boid.Velocity;
        return force.Limit(boid.MaxForce);
    }

    private Vector2 Steering_Pursuit(Boid boid, Vector2 position, float weight)
    {
        return Steering_Seek(boid, position) * weight;
    }
    
    // private Vector2 Steering_Wander(ref Boid boid, float weight)
    // {
    //     Vector2 circleCentre = (boid.Velocity == Vector2.Zero ? Vector2.Up : boid.Velocity.Normalized()) * 5.0f;
    //     float circleRadius = 5.0f;
    //     Vector2 displacement = new Vector2(Mathf.Cos(boid.WanderAngle), Mathf.Sin(boid.WanderAngle)).Normalized() * circleRadius;
    //     boid.WanderAngle += (Utils.Rng.Randf() - 0.5f) * TimeSystem.Delta * 100.0f;
    //     
    //     Vector2 desired = circleCentre + displacement;
    //     return desired * weight;
    // }
    
    private Vector2 Steering_EdgeRepulsion(Boid boid, Rect2 bounds, float weight)
    {
        if (bounds.HasPoint(boid.Position))
            return Vector2.Zero;

        Vector2 closestPointOnEdge = new(Mathf.Max(Mathf.Min(boid.Position.x, bounds.End.x), bounds.Position.x),
            Mathf.Max(Mathf.Min(boid.Position.y, bounds.End.y), bounds.Position.y));

        return Steering_Seek(boid, closestPointOnEdge) * weight;
    }
    
    private SurfaceTool _st = new();

    public void DrawSimulationToMesh(out Mesh mesh)
    {
        ArrayMesh outMesh = new ArrayMesh();
        
        // boid body
        _st.Begin(Mesh.PrimitiveType.Lines);
        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];
            Vector2 forward = boid.Heading;
            Vector2 right = new(forward.y, -forward.x);
            _st.AddColor(Colors.Black);
            _st.AddVertex((boid.Position + forward * -2.5f + right * 3.0f).To3D());
            _st.AddColor(Colors.Black);
            _st.AddVertex((boid.Position + forward * 4.5f).To3D());
            _st.AddColor(Colors.Black);
            _st.AddVertex((boid.Position + forward * -2.5f - right * 3.0f).To3D());
            _st.AddIndex(i * 3 + 0);
            _st.AddIndex(i * 3 + 1);
            _st.AddIndex(i * 3 + 1);
            _st.AddIndex(i * 3 + 2);
            _st.AddIndex(i * 3 + 2);
            _st.AddIndex(i * 3 + 0);
        }

        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, _st.CommitToArrays());
        
        // boid center
        // _st.Begin(Mesh.PrimitiveType.Triangles);
        // foreach (Boid boid in _boids)
        // {
        //     int segments = 8;
        //     float radius = 0.5f;
        //     for (int i = 0; i < segments; i++)
        //     {
        //         _st.AddColor(Colors.Black);
        //         float rad = Mathf.Pi * 2.0f * ((float) i / segments);
        //         Vector3 vert = (boid.Position + new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radius).To3D();
        //         _st.AddVertex(vert);
        //
        //         _st.AddIndex(9);
        //         _st.AddIndex((i + 1) % segments);
        //         _st.AddIndex(i);
        //     }
        //     _st.AddColor(Colors.Black);
        //     _st.AddVertex(boid.Position.To3D());
        // }
        // outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, _st.CommitToArrays());
        
        // boid velocity/force
        _st.Begin(Mesh.PrimitiveType.Lines);
        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];
            _st.AddColor(Colors.Blue);
            _st.AddVertex(boid.Position.To3D());
            _st.AddColor(Colors.Blue);
            _st.AddVertex((boid.Position + boid.Velocity * 0.25f).To3D());

            _st.AddColor(Colors.Red);
            _st.AddVertex(boid.Position.To3D());
            _st.AddColor(Colors.Red);
            _st.AddVertex((boid.Position + boid.Steering * 0.25f / TimeSystem.Delta).To3D());
            
            _st.AddIndex(i * 4 + 0);
            _st.AddIndex(i * 4 + 1);
            _st.AddIndex(i * 4 + 2);
            _st.AddIndex(i * 4 + 3);
        }

        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, _st.CommitToArrays());
        
        // edges
        _st.Begin(Mesh.PrimitiveType.LineStrip);
        {
            _st.AddColor(Colors.Black);
            _st.AddVertex(new Vector2(EdgeBounds.Position).To3D());
            _st.AddColor(Colors.Black);
            _st.AddVertex(new Vector2(EdgeBounds.End.x, EdgeBounds.Position.y).To3D());
            _st.AddColor(Colors.Black);
            _st.AddVertex(new Vector2(EdgeBounds.End).To3D());
            _st.AddColor(Colors.Black);
            _st.AddVertex(new Vector2(EdgeBounds.Position.x, EdgeBounds.End.y).To3D());
            
            _st.AddIndex(0);
            _st.AddIndex(1);
            _st.AddIndex(2);
            _st.AddIndex(3);
            _st.AddIndex(0);
            
        }
        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.LineStrip, _st.CommitToArrays());

        mesh = outMesh;
    }
}
