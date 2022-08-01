using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using Godot;
using ImGuiNET;
using static SteeringManager.Behaviours;
using Vector2 = System.Numerics.Vector2;

public partial class SteeringManager : Singleton<SteeringManager>
{
    public enum ObstacleShape
    {
        Circle,
    }

    public interface IPoolable
    {
        public abstract bool Empty();
        public abstract int GenerateId();
    }

    public struct Boid : IPoolable
    {
        public int Id;
        public byte Alignment;
        public Vector2 Position; // TODO: Separate position.
        public Vector2 Velocity;
        public Vector2 DesiredVelocityOverride;
        public Vector2 Steering;
        public Vector2 Heading;
        public float Radius;
        public float Speed;
        public Vector2 Target;
        public float ArriveDeadzone;
        public float ArriveWeight;
        public int TargetIndex;
        public Vector2 TargetOffset;
        public float DesiredDistFromTargetMin;
        public float DesiredDistFromTargetMax;
        public Vector2 DesiredOffsetFromTarget;
        public int Behaviours;
        public float DesiredSpeed;
        public float MaxSpeed;
        public float MinSpeed;
        public float MaxForce;
        public float LookAhead;
        public float ViewRange;
        public float ViewAngle;
        public float WanderAngle;
        public float WanderCircleDist;
        public float WanderCircleRadius;
        public float WanderVariance;
        public bool Ignore;
#if !FINAL
        public Intersection Intersection;
#endif

        public bool HasBehaviour(Behaviours behaviour)
        {
            return (Behaviours & (1 << (int) behaviour)) > 0;
        }

        public Godot.Vector2 PositionG => Position.ToGodot();
        public Godot.Vector2 VelocityG => Velocity.ToGodot();
        
        public bool Empty() => Id == 0;
        public static int IdGen;
        public int GenerateId()
        {
            Id = IdGen++;
            return Id;
        }
    }

    public struct Obstacle : IPoolable
    {
        public int Id;
        public Vector2 Position;
        public ObstacleShape Shape;
        public float Size;
        
        public bool Empty() => Id == 0;
        public static int IdGen;
        public int GenerateId()
        {
            Id = IdGen++;
            return Id;
        }
    }

    public struct FlowField : IPoolable
    {
        public int Id;
        public FlowFieldResource Resource;
        public int TrackID;
        public Vector2 Position;
        public Vector2 Size;

        public bool HasPoint(Vector2 point)
        {
            Vector2 tl = Position - Size * 0.5f;
            if (point.X < tl.X)
                return false;
            if (point.Y < tl.Y)
                return false;
            if (point.X >= tl.X + Size.X)
                return false;
            if (point.Y >= tl.Y + Size.Y)
                return false;
            return true;
        }

        public bool Empty() => Id == 0;
        public static int IdGen;
        public int GenerateId()
        {
            Id = IdGen++;
            return Id;
        }
    }

    public struct Intersection
    {
        public bool Intersect;
        public float IntersectTime;
        public Vector2 SurfacePoint;
        public Vector2 SurfaceNormal;
    }

    private static readonly int MAX_BOIDS = 1000;
    private static readonly int MAX_OBSTACLES = 100;
    private static readonly int MAX_FLOWFIELDS = 100;
    
    private StructPool<Boid> _boidPool = new(MAX_BOIDS);
    private StructPool<Obstacle> _obstaclePool = new(MAX_OBSTACLES);
    private StructPool<FlowField> _flowFieldPool = new(MAX_FLOWFIELDS);

    private Dictionary<int, int> _boidIdToIndex = new();
    private Dictionary<int, int> _obstacleIdToIndex = new();
    private Dictionary<int, int> _flowFieldIdToIndex = new();

    private Vector2[] _boidPositions = new Vector2[MAX_BOIDS];
    private byte[] _boidAlignments = new byte[MAX_BOIDS];
    
    private static int _boidIdGen = 1; // IDs start at 1 because Boid struct initialises default to 0.
    private static int _obstacleIdGen = 1;
    private static int _flowFieldIdGen = 1;
    private static float[] _behaviourWeights;
    private static float _delta;

    public MeshInstance _debugMesh;

    public override void _Ready()
    {
        base._Ready();

        Boid.IdGen = 1;
        Obstacle.IdGen = 1;
        FlowField.IdGen = 1;
        
        _debugMesh = new MeshInstance();
        SpatialMaterial mat = new();
        mat.FlagsUnshaded = true;
        mat.VertexColorUseAsAlbedo = true;
        mat.VertexColorIsSrgb = true;
        mat.FlagsNoDepthTest = true;
        _debugMesh.MaterialOverride = mat;
        AddChild(_debugMesh);

        _behaviourWeights = new float[(int) COUNT];
        _behaviourWeights[(int) DesiredVelocityOverride] = 1.0f; 
        _behaviourWeights[(int) Separation] = 2.0f;
        _behaviourWeights[(int) AvoidObstacles] = 2.0f;
        _behaviourWeights[(int) AvoidAllies] = 2.0f;
        _behaviourWeights[(int) AvoidEnemies] = 2.0f;
        _behaviourWeights[(int) MaintainSpeed] = 0.1f;
        _behaviourWeights[(int) Cohesion] = 0.1f;
        _behaviourWeights[(int) Alignment] = 0.1f;
        _behaviourWeights[(int) Arrive] = 1.0f;
        _behaviourWeights[(int) Pursuit] = 1.0f;
        _behaviourWeights[(int) Flee] = 1.0f;
        _behaviourWeights[(int) Wander] = 0.1f;
        _behaviourWeights[(int) FlowFieldFollow] = 1.0f;
        _behaviourWeights[(int) MaintainDistance] = 1.0f;
        _behaviourWeights[(int) MaintainOffset] = 1.0f;
        _behaviourWeights[(int) Stop] = 1.0f;
        // this is low so it provides a subtle nudge and doesn't override other behaviours.
        _behaviourWeights[(int) MaintainBroadside] = 0.1f;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        _delta = delta;

#if TOOLS
        if (Game.Instance == null)
            return;
#endif

        Span<Boid> boids = _boidPool.AsSpan();
        ReadOnlySpan<Obstacle> obstacles = _obstaclePool.AsSpan();
        Span<FlowField> flowFields = _flowFieldPool.AsSpan();
        
        foreach (ref FlowField flowField in flowFields)
        {
            if (flowField.TrackID != 0)
            {
                flowField.Position = boids[_boidIdToIndex[flowField.TrackID]].Position;
            }
        }

        Span<Vector2> boidPositions = _boidPositions.AsSpan(0, _boidPool.Span);
        Span<byte> boidAlignments = _boidAlignments.AsSpan(0, _boidPool.Span);
        for (int i = 0; i < boidPositions.Length; i++)
        {
            boidPositions[i] = boids[i].Position;
            boidAlignments[i] = boids[i].Alignment;
        }

        for (int i = 0; i < boids.Length; i++)
        {
            ref Boid boid = ref boids[i];
            if (boid.Id == 0 || boid.Behaviours == 0)
                continue;

            Vector2 totalForce = Vector2.Zero;

            // update target
            if (boid.TargetIndex != -1)
            {
                Boid targetBoid = boids[_boidIdToIndex[boid.TargetIndex]];
                Vector2 side = new(targetBoid.Heading.Y, -targetBoid.Heading.X);
                boid.Target = targetBoid.Position - Utils.GlobaliseOffset(boid.TargetOffset, targetBoid.Heading, side);
            }

            for (Behaviours j = 0; j < COUNT; j++)
            {
                if (!boid.HasBehaviour(j))
                    continue;

                Vector2 force = CalculateSteeringForce(j, ref boid, i, boids, boidPositions, boidAlignments, 
                    obstacles, flowFields, delta);

                if (float.IsNaN(force.X) || float.IsInfinity(force.X))
                {
                    Debug.Assert(!float.IsNaN(force.X) && !float.IsInfinity(force.X), "NaN in steering calculation!");
                }

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

            totalForce = ApplyMinimumSpeed(boid, totalForce, boid.MinSpeed);
            boid.Steering = totalForce;

            boid.Velocity += boid.Steering;
            boid.Velocity.Limit(boid.MaxSpeed);

            // TODO: replace max speed with drag, so max speed is a derived value from drag and thrust.
            // float dragCoeff = 0.0001f;
            // Vector2 drag = -boid.Velocity.NormalizeSafe() * boid.Velocity.LengthSquared() * dragCoeff;
            //boid.Velocity *= boid.Velocity.LengthSquared();

            boid.Speed = boid.Velocity.Length();

            // Smooth heading to eliminate rapid heading changes on small velocity adjustments
            if (boid.Speed > boid.MaxSpeed * 0.025f)
            {
                const float smoothing = 0.9f;
                boid.Heading = Vector2.Normalize(boid.Velocity) * (1.0f - smoothing) + boid.Heading * smoothing;
                boid.Heading = Vector2.Normalize(boid.Heading);
            }

            boid.Position += boid.Velocity * delta;
            
            if (float.IsNaN(boid.Position.X))
            {
                Debug.Assert(!float.IsNaN(boid.Position.X), "!float.IsNaN(boid.Position.X)");
            }
        }

        DrawSimulationToMesh(out Mesh mesh);
        _debugMesh.Mesh = mesh;
    }

    private static Vector2 CalculateSteeringForce(Behaviours behaviour, ref Boid boid, int index, ReadOnlySpan<Boid> boids, 
        in ReadOnlySpan<Vector2> boidPositions, in ReadOnlySpan<byte> boidAlignments, ReadOnlySpan<Obstacle> obstacles, ReadOnlySpan<FlowField> flowFields, float delta)
    {
        Vector2 force = Vector2.Zero;
        float influence;
        
        switch (behaviour)
        {
            case Cohesion:
                force += Steering_Cohesion(boid, boids);
                break;
            case Alignment:
                force += Steering_Align(boid, boids);
                break;
            case Separation:
                force += Steering_Separate(boid, boids, boidPositions, obstacles, delta);
                break;
            case Arrive:
                force += Steering_Arrive(boid, boid.Target, out influence);
                force = force.Limit(boid.MaxForce * delta) * influence;
                break;
            case Pursuit:
                force += Steering_Pursuit(boid);
                break;
            case Flee:
                force += Steering_Flee(boid);
                break;
            case AvoidAllies:
                force += Steering_AvoidAllies(ref boid, index, boids, boidPositions, boidAlignments);
                break;
            case AvoidEnemies:
                force += Steering_AvoidEnemies(ref boid, index, boids, boidPositions, boidAlignments);
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
            case MaintainDistance:
                force += Steering_MaintainDistance(boid);
                break;
            case MaintainOffset:
                force += Steering_MaintainOffset(boid);
                break;
            case Stop:
                force += Steering_Stop(boid);
                break;
            case MaintainBroadside:
                force += Steering_MaintainBroadside(boid);
                break;
            case DesiredVelocityOverride:
                force += Steering_DesiredVelocityOverride(boid);
                break;
            case COUNT:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return force.Limit(boid.MaxForce * delta) * _behaviourWeights[(int)behaviour];
    }

    private void GetPoolForType<T>(out StructPool<T> pool, out Dictionary<int, int> toIndex, out int max) where T : IPoolable
    {
        pool = null;
        toIndex = null;
        max = 0;
        if (typeof(T) == typeof(Boid))
        {
            pool = _boidPool as StructPool<T>;
            toIndex = _boidIdToIndex;
            max = MAX_BOIDS;
        }
        else if (typeof(T) == typeof(Obstacle))
        {
            pool = _obstaclePool as StructPool<T>;
            toIndex = _obstacleIdToIndex;
            max = MAX_OBSTACLES;
        }
        else if (typeof(T) == typeof(FlowField))
        {
            pool = _flowFieldPool as StructPool<T>;
            toIndex = _flowFieldIdToIndex;
            max = MAX_FLOWFIELDS;
        }
        else
            DebugUtils.Assert(false, "Invalid type in SteeringManager.GetPoolForType()!");
    }
    
    public int Register<T>(T obj) where T : IPoolable
    {
        GetPoolForType<T>(out StructPool<T> pool, out Dictionary<int, int> toIndex, out int max);
        
#if !FINAL
        if (pool.Count >= max)
        {
            Debug.Assert(false, $"Maximum {typeof(T)} ({max}) reached, check for leaks or increase pool size.");
            return -1;
        }
#endif

        int id = obj.GenerateId();
        int index = pool.Add(obj);
        toIndex[id] = index;
        return id;
    }

    public bool HasObject<T>(int id) where T : IPoolable
    {
        GetPoolForType<T>(out StructPool<T> _, out Dictionary<int, int> toIndex, out int _);
        return toIndex.ContainsKey(id);
    }

    public ref T GetObject<T>(int id) where T : IPoolable
    {
        GetPoolForType<T>(out StructPool<T> pool, out Dictionary<int, int> toIndex, out int _);
        DebugUtils.Assert(toIndex.ContainsKey(id), "Object doesn't exist.");
        return ref pool.AsSpan()[toIndex[id]];
    }

    public void Unregister<T>(int id) where T : IPoolable
    {
        GetPoolForType<T>(out StructPool<T> pool, out Dictionary<int, int> toIndex, out int _);
        Debug.Assert(HasObject<T>(id));
        int i = toIndex[id];
        pool.Remove(i);
        toIndex.Remove(id);
    }
    
    public override void _EnterTree()
    {
        base._EnterTree();
        DebugImGui.Instance.RegisterWindow("steering", "Steering Behaviours", _OnImGuiLayout);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DebugImGui.Instance.UnRegisterWindow("steering", _OnImGuiLayout);
    }

    private void _OnImGuiLayout()
    {
        ImGui.Text($"Boids: {_boidPool.Count} / {MAX_BOIDS}");
        ImGui.Text($"Span: {_boidPool.Span}");
        ImGui.Spacing();
        ImGui.Checkbox("Draw", ref _draw);
        ImGui.Checkbox("Draw Separation", ref _drawSeparation);
        ImGui.Checkbox("Draw Steering", ref _drawSteering);
        ImGui.Checkbox("Draw Velocity", ref _drawVelocity);
        ImGui.Checkbox("Draw Vision", ref _drawVision);
        ImGui.Checkbox("Draw Avoidance", ref _drawAvoidance);
        ImGui.Checkbox("Draw Wander", ref _drawWander);
        ImGui.Checkbox("Draw FlowFields", ref _drawFlowFields);
        ImGui.EndTabItem();
    }
}
