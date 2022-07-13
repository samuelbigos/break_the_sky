#if GODOT_PC || GODOT_WEB || GODOT_MOBILE
#define EXPORT
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using static SteeringManager.Behaviours;
using Vector2 = System.Numerics.Vector2;

public partial class SteeringManager : Singleton<SteeringManager>
{
    public enum ObstacleShape
    {
        Circle,
    }

    public struct Boid
    {
        public short Id;
        public byte Alignment;
        public Vector2 Position; // TODO: Separate position.
        public Vector2 Velocity;
        public Vector2 Steering;
        public Vector2 Heading;
        public float Radius;
        public float Speed;
        public Vector2 Target;
        public short TargetIndex;
        public Vector2 TargetOffset;
        public float DesiredDistFromTarget;
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
#if !EXPORT
        public Intersection Intersection;
#endif
        
        public bool HasBehaviour(Behaviours behaviour)
        {
            return (Behaviours & (1 << (int) behaviour)) > 0;
        }

        public Godot.Vector2 PositionG => Position.ToGodot();
        public Godot.Vector2 VelocityG => Velocity.ToGodot();
    }

    public struct Obstacle
    {
        public int ID;
        public Vector2 Position;
        public ObstacleShape Shape;
        public float Size;
    }

    public struct FlowField
    {
        public int ID;
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
    }

    public struct Intersection
    {
        public bool Intersect;
        public float IntersectTime;
        public Vector2 SurfaceNormal;
    }

    public static Rect2 EdgeBounds;

    private StructPool<Boid> _boidPool = new(1000);
    private int _numObstacles;
    private Obstacle[] _obstaclePool = new Obstacle[100];
    private int _numFlowFields;
    private FlowField[] _flowFieldPool = new FlowField[100];

    private Dictionary<int, int> _boidIdToIndex = new();
    private Dictionary<int, int> _obstacleIdToIndex = new();
    private Dictionary<int, int> _obstacleIndexToId = new();
    private Dictionary<int, int> _flowFieldIdToIndex = new();
    private Dictionary<int, int> _flowFieldIndexToId = new();

    private Vector2[] _boidPositions = new Vector2[500];
    private byte[] _boidAlignments = new byte[500];
    
    private int _boidIdGen = 1;
    private int _obstacleIdGen = 1;
    private int _flowFieldIdGen = 1;
    private static float[] _behaviourWeights;

    public MeshInstance _debugMesh;

    public override void _Ready()
    {
        base._Ready();

        _debugMesh = new MeshInstance();
        SpatialMaterial mat = new();
        mat.FlagsUnshaded = true;
        mat.VertexColorUseAsAlbedo = true;
        mat.VertexColorIsSrgb = true;
        _debugMesh.MaterialOverride = mat;
        AddChild(_debugMesh);

        _behaviourWeights = new float[(int) COUNT];
        _behaviourWeights[(int) Separation] = 2.0f;
        _behaviourWeights[(int) AvoidObstacles] = 2.0f;
        _behaviourWeights[(int) AvoidAllies] = 2.0f;
        _behaviourWeights[(int) AvoidEnemies] = 2.0f;
        _behaviourWeights[(int) MaintainSpeed] = 0.1f;
        _behaviourWeights[(int) Cohesion] = 0.1f;
        _behaviourWeights[(int) Alignment] = 0.1f;
        _behaviourWeights[(int) Arrive] = 2.0f;
        _behaviourWeights[(int) Pursuit] = 1.0f;
        _behaviourWeights[(int) Flee] = 1.0f;
        _behaviourWeights[(int) Wander] = 0.1f;
        _behaviourWeights[(int) FlowFieldFollow] = 1.0f;
        _behaviourWeights[(int) MaintainDistance] = 1.0f;
        _behaviourWeights[(int) MaintainOffset] = 1.0f;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

#if TOOLS
        if (Game.Instance == null)
            return;
#endif

        // move area
        EdgeBounds.Position = Game.Player.GlobalPosition - EdgeBounds.Size * 0.5f;

        Span<Boid> boids = _boidPool.AsSpan();
        ReadOnlySpan<Obstacle> obstacles = _obstaclePool.AsSpan(0, _numObstacles);
        Span<FlowField> flowFields = _flowFieldPool.AsSpan(0, _numFlowFields);
        
        foreach (ref FlowField flowField in flowFields)
        {
            if (flowField.TrackID != 0)
            {
                flowField.Position = boids[_boidIdToIndex[flowField.TrackID]].Position;
            }
        }

        Span<Vector2> boidPositions = _boidPositions.AsSpan(0, _boidPool.Count);
        Span<byte> boidAlignments = _boidAlignments.AsSpan(0, _boidPool.Count);
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
            
            //GD.Print($"{totalForce.Length() / (boid.MaxForce * delta * 2.0f)}");

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
                boid.Heading = Vector2.Normalize(boid.Velocity) * (1.0f - smoothing) + boid.Heading * smoothing;
                boid.Heading = Vector2.Normalize(boid.Heading);
            }

            boid.Position = WrapPosition(boid.Position + boid.Velocity * delta, EdgeBounds);
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
                force += Steering_Arrive(boid, boid.Target);
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
            case COUNT:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return force.Limit(boid.MaxForce * delta) * _behaviourWeights[(int)behaviour];
    }

    public short RegisterBoid(Boid boid)
    {
        Debug.Assert(!_boidIdToIndex.ContainsKey(boid.Id), $"Boid with this ID ({boid.Id}) already registered.");
        if (_boidIdToIndex.ContainsKey(boid.Id))
            return -1;

        boid.Id = Convert.ToInt16(_boidIdGen++);
        int index = _boidPool.Add(boid);
        _boidIdToIndex[boid.Id] = index;
        return boid.Id;
    }

    public int RegisterObstacle(Obstacle obstacle)
    {
        Debug.Assert(!_obstacleIdToIndex.ContainsKey(obstacle.ID), $"Obstacle with this ID ({obstacle.ID}) already registered.");
        if (_obstacleIdToIndex.ContainsKey(obstacle.ID))
            return -1;
        
        obstacle.ID =  _obstacleIdGen++;
        _obstaclePool[_numObstacles++] = obstacle;
        _obstacleIdToIndex[obstacle.ID] = _numObstacles - 1;
        _obstacleIndexToId[_numObstacles - 1] = obstacle.ID;
        return obstacle.ID;
    }

    public int RegisterFlowField(FlowField flowField)
    {
        Debug.Assert(!_flowFieldIdToIndex.ContainsKey(flowField.ID), $"Obstacle with this ID ({flowField.ID}) already registered.");
        if (_flowFieldIdToIndex.ContainsKey(flowField.ID))
            return -1;

        flowField.ID = _flowFieldIdGen++;
        _flowFieldPool[_numFlowFields++] = flowField;
        _flowFieldIdToIndex[flowField.ID] = _numFlowFields - 1;
        _flowFieldIndexToId[_numFlowFields - 1] = flowField.ID;
        return flowField.ID;
    }

    public bool HasBoid(int id)
    {
        return _boidIdToIndex.ContainsKey(id);
    }

    public ref Boid GetBoid(int id)
    {
        Debug.Assert(_boidIdToIndex.ContainsKey(id), "Boid doesn't exist.");
        return ref _boidPool.AsSpan()[_boidIdToIndex[id]];
    }

    public void RemoveBoid(int id)
    {
        Debug.Assert(HasBoid(id));
        int i = _boidIdToIndex[id];
        _boidPool.Remove(i);
        _boidIdToIndex.Remove(id);
    }
}
