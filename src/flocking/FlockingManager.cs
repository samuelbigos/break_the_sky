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
        Separation,
        Cohesion,
        Alignment,
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
        public int Alignment;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Steering;
        public Vector2 Heading;
        public float SeparationRadius;
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
    private Vector2 _smoothedAcceleration;

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
                        force += Steering_Cohesion(i, delta, 50.0f, boid.Weights[(int) Behaviours.Cohesion]);
                        break;
                    case Behaviours.Alignment:
                        force += Steering_Align(i, delta, 50.0f, boid.Weights[(int) Behaviours.Alignment]);
                        break;
                    case Behaviours.Separation:
                        force += Steering_Separate(i, delta, boid.SeparationRadius) * boid.Weights[(int) Behaviours.Separation];
                        break;
                    case Behaviours.Arrive:
                        force += Steering_Arrive(boid, delta, boid.Target, 50.0f) * boid.Weights[(int) Behaviours.Arrive];
                        break;
                    case Behaviours.Pursuit:
                        force += Steering_Pursuit(boid, delta, boid.Target, boid.Weights[(int) Behaviours.Pursuit]);
                        break;
                    case Behaviours.Flee:
                        break;
                    case Behaviours.EdgeRepulsion:
                        force += Steering_EdgeRepulsion(boid, delta, EdgeBounds, boid.Weights[(int) Behaviours.EdgeRepulsion]);
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

            float smoothRate = 0.1f;
            _smoothedAcceleration = _smoothedAcceleration * (1.0f - smoothRate) + totalForce * smoothRate;
            _smoothedAcceleration = totalForce;
            
            boid.Velocity += _smoothedAcceleration;
            boid.Velocity.Limit(boid.MaxSpeed);
            
            boid.Steering = totalForce;

            if (boid.Velocity.Length() > boid.MaxSpeed * 0.025f)
            {
                const float smoothing = 0.9f;
                boid.Heading = boid.Velocity.Normalized() * (1.0f - smoothing) + boid.Heading * smoothing;
                boid.Heading = boid.Heading.Normalized();
            }

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
            return VecLimitDeviationAngleUtility(true, force, cosine, boid.Velocity.Normalized());
        }
    }
    
    private Vector2 VecLimitDeviationAngleUtility (bool insideOrOutside,
    Vector2 source,
    float cosineOfConeAngle,
    Vector2 basis)
    {
        // immediately return zero length input vectors
        float sourceLength = source.Length();
        if (sourceLength == 0) return source;

        // measure the angular diviation of "source" from "basis"
        Vector2 direction = source / sourceLength;
        float cosineOfSourceAngle = direction.Dot(basis);

        // Simply return "source" if it already meets the angle criteria.
        // (note: we hope this top "if" gets compiled out since the flag
        // is a constant when the function is inlined into its caller)
        if (insideOrOutside)
        {
            // source vector is already inside the cone, just return it
            if (cosineOfSourceAngle >= cosineOfConeAngle) return source;
        }
        else
        {
            // source vector is already outside the cone, just return it
            if (cosineOfSourceAngle <= cosineOfConeAngle) return source;
        }

        // find the portion of "source" that is perpendicular to "basis"
        Vector2 perp = source.PerpendicularComponent(basis);

        // normalize that perpendicular
        Vector2 unitPerp = perp.Normalized();

        // construct a new vector whose length equals the source vector,
        // and lies on the intersection of a plane (formed the source and
        // basis vectors) and a cone (whose axis is "basis" and whose
        // angle corresponds to cosineOfConeAngle)
        float perpDist = Mathf.Sqrt(1 - cosineOfConeAngle * cosineOfConeAngle);
        Vector2 c0 = basis * cosineOfConeAngle;
        Vector2 c1 = unitPerp * perpDist;
        return (c0 + c1) * sourceLength;
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

    public int AddBoid(Vector2 pos, Vector2 vel, float maxSpeed, float maxForce, 
        int behaviours, float[] weights, Vector2 target, float fov, int alignment)
    {
        Boid boid = new()
        {
            Alive = true,
            ID = _idGen,
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

    private Vector2 Steering_Cohesion(int i, float delta, float radius, float weight)
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
        return Steering_Seek(boid, delta, centre) * weight;
    }

    private Vector2 Steering_Align(int i, float delta, float radius, float weight)
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
        return force.Limit(boid.MaxForce * delta) * weight;
    }

    private Vector2 Steering_Separate(int i, float delta, float radius)
    {
        Boid boid = _boids[i];
        
        Vector2 forceSum = Vector2.Zero;
        int count = 0;
        for (int j = 0; j < _boids.Count; j++)
        {
            if (i == j) continue;
            Boid other = _boids[j];
            float dist = (boid.Position - other.Position).Length();
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

    private Vector2 Steering_Pursuit(Boid boid, float delta, Vector2 position, float weight)
    {
        return Steering_Seek(boid, delta, position) * weight;
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
    
    private Vector2 Steering_EdgeRepulsion(Boid boid, float delta, Rect2 bounds, float weight)
    {
        if (bounds.HasPoint(boid.Position))
            return Vector2.Zero;

        Vector2 closestPointOnEdge = new(Mathf.Max(Mathf.Min(boid.Position.x, bounds.End.x), bounds.Position.x),
            Mathf.Max(Mathf.Min(boid.Position.y, bounds.End.y), bounds.Position.y));

        return Steering_Seek(boid, delta, closestPointOnEdge) * weight;
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
            Color col = boid.Alignment == 0 ? Colors.Blue : Colors.Red;
            _st.AddColor(col);
            _st.AddVertex((boid.Position + forward * -2.5f + right * 3.0f).To3D());
            _st.AddColor(col);
            _st.AddVertex((boid.Position + forward * 4.5f).To3D());
            _st.AddColor(col);
            _st.AddVertex((boid.Position + forward * -2.5f - right * 3.0f).To3D());
            _st.AddIndex(i * 3 + 0);
            _st.AddIndex(i * 3 + 1);
            _st.AddIndex(i * 3 + 1);
            _st.AddIndex(i * 3 + 2);
            _st.AddIndex(i * 3 + 2);
            _st.AddIndex(i * 3 + 0);
        }

        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, _st.CommitToArrays());
        
        // separation radius
        _st.Begin(Mesh.PrimitiveType.Lines);
        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];
            int segments = 32;
            float radius = boid.SeparationRadius * 0.5f;
            for (int s = 0; s < segments; s++)
            {
                _st.AddColor(Colors.Black);
                float rad = Mathf.Pi * 2.0f * ((float) s / segments);
                Vector3 vert = (boid.Position + new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radius).To3D();
                _st.AddVertex(vert);
                _st.AddIndex(i * segments + s);
                _st.AddIndex(i * segments + (s + 1) % segments);
            }
        }
        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, _st.CommitToArrays());
        
        // boid velocity/force
        _st.Begin(Mesh.PrimitiveType.Lines);
        for (int i = 0; i < _boids.Count; i++)
        {
            Boid boid = _boids[i];
            _st.AddColor(Colors.Teal);
            _st.AddVertex(boid.Position.To3D());
            _st.AddColor(Colors.Teal);
            _st.AddVertex((boid.Position + boid.Velocity * 15.0f / boid.MaxSpeed).To3D());

            _st.AddColor(Colors.Purple);
            _st.AddVertex(boid.Position.To3D());
            _st.AddColor(Colors.Purple);
            _st.AddVertex((boid.Position + boid.Steering * 15.0f / boid.MaxForce / TimeSystem.Delta).To3D());
            
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
