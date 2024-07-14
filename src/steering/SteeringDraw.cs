using Godot;
using System;
using System.Diagnostics;
using Array = Godot.Collections.Array;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

public partial class SteeringManager
{
    private bool _draw = false;
    private bool _drawSeparation = true;
    private bool _drawSteering = true;
    private bool _drawVelocity = true;
    private bool _drawVision = false;
    private bool _drawAvoidance = false;
    private bool _drawWander = false;
    private bool _drawFlowFields = false;
    
    private Godot.Vector3[] _vertList = new Godot.Vector3[60000];
    private Color[] _colList = new Color[60000];
    private int[] _indexList = new int[150000];

    public void DrawSimulationToMesh(out Mesh mesh)
    {
        ArrayMesh outMesh = new();
        if (!_draw)
        {
            mesh = outMesh;
            return;
        }

        int v = 0;
        int i = 0;

        // boids
        Span<Boid> boids = _boidPool.AsSpan();
        foreach (ref readonly Boid boid in boids)
        {
            if (boid.Empty()) continue;
            
            // body
            Vector3 boidPos = boid.Position.To3D();
            Vector3 forward = boid.Heading.To3D();
            Vector3 right = new(forward.Z, 0.0f, -forward.X);
            Color col = boid.Alignment == 0 ? Colors.Blue : Colors.Red;
            col = Color.Color8(230, 230, 230);
            float size = boid.Radius;
            Vector3 p0 = boidPos + forward * -size * 0.33f + right * size * 0.5f;
            Vector3 p1 = boidPos + forward * size * 0.66f;
            Vector3 p2 = boidPos + forward * -size * 0.33f - right * size * 0.5f;
            Utils.Line(p0, p1, col, ref v, ref i, _vertList, _colList, _indexList);
            Utils.Line(p1, p2, col, ref v, ref i, _vertList, _colList, _indexList);
            Utils.Line(p2, p0, col, ref v, ref i, _vertList, _colList, _indexList);

            // separation radius
            if (_drawSeparation)
            {
                Utils.Circle(boidPos, 32, boid.Radius, Colors.DarkGray, ref v, ref i, _vertList, _colList, _indexList);
            }

            // boid velocity/force
            if (_drawVelocity)
            {
                Utils.Line(boidPos, boidPos + boid.Velocity.To3D() * 25.0f / boid.MaxSpeed, Colors.Red, ref v, ref i,
                    _vertList, _colList, _indexList);
            }

            if (_drawSteering)
            {
                Utils.Line(boidPos, boidPos + boid.Steering.To3D() * 10.0f / boid.MaxForce / (float) TimeSystem.Delta,
                    Colors.Purple, ref v, ref i, _vertList, _colList, _indexList);
            }

            // boid avoidance
#if !FINAL
            if (boid.Intersection.Intersect && _drawAvoidance)
            {
                //Line(_st, boid.Position, boid.Position + forward * boid.LookAhead * boid.Speed, Colors.Black, ref v);
                Vector3 surface = boid.Intersection.SurfacePoint.To3D();
                Utils.Circle(surface, 8, 1.0f, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
                Utils.Line(surface, surface + boid.Intersection.SurfaceNormal.To3D() * 10.0f, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
            }
#endif

            // view range
            if (_drawVision)
            {
                Color visCol = Color.Color8(30, 30, 30);
                Utils.CircleArc(boidPos, 32, boid.ViewRange, boid.ViewAngle, boid.Heading, visCol, ref v, ref i,
                    _vertList, _colList, _indexList);
            }

            // wander
            if (_drawWander && boid.HasBehaviour(Behaviours.Wander))
            {
                Vector3 circlePos = boidPos + boid.Heading.To3D() * boid.WanderCircleDist;

                float angle = -boid.Heading.AngleTo(System.Numerics.Vector2.UnitX) + boid.WanderAngle;
                Vector3 displacement = Vector3.Normalize(new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle))) *
                                       boid.WanderCircleRadius;

                Utils.Circle(circlePos, 32, boid.WanderCircleRadius, Colors.SlateGray, ref v, ref i, _vertList,
                    _colList, _indexList);
                Utils.Line(circlePos, circlePos + displacement, Colors.DarkGray, ref v, ref i, _vertList, _colList,
                    _indexList);
            }

            if (_drawFlowFields)
            {
                //Utils.Line(boidPos, boidPos + forward * boid.FlowFieldDist, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
                //Utils.Circle(boidPos + forward * boid.FlowFieldDist, 8, 2.0f, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
            }
        }

        // flow fields
        if (_drawFlowFields)
        {
            float flowFieldVisCellSize = 10.0f;
            Vector2 topLeft = GameCamera.Instance.ScreenPosition(Godot.Vector2.Zero).ToNumerics();
            Vector2 botRight = GameCamera.Instance.ScreenPosition(DisplayServer.ScreenGetSize()).ToNumerics();

            for (float x = topLeft.X; x < botRight.X; x += flowFieldVisCellSize)
            {
                for (float y = topLeft.Y; y < botRight.Y; y += flowFieldVisCellSize)
                {
                    Vector3 pos = new(x, -1.0f, y); // / flowFieldVisCellSize;
                    // pos = pos.Floor();
                    // pos *= flowFieldVisCellSize;

                    Vector3 dir = Vector3.Zero;
                    int count = 0;
                    Span<FlowField> ffs = _flowFieldPool.AsSpan();
                    foreach (FlowField ff in ffs)
                    {
                        if (ff.Empty()) continue;
                        if (TryGetFieldAtPosition(ff, new System.Numerics.Vector2(pos.X, pos.Z),
                                out Vector2 vector))
                        {
                            dir.X += vector.X;
                            dir.Z += vector.Y;
                            count++;
                        }
                    }

                    if (count > 0)
                        dir /= count;

                    Color col = new(
                        Mathf.Lerp(0.0f, 1.0f, dir.X * 0.5f + 0.5f),
                        Mathf.Lerp(0.0f, 1.0f, dir.Z * 0.5f + 0.5f),
                        Mathf.Lerp(0.0f, 1.0f, 1.0f - (dir.Z * 0.5f + 0.5f)));
                    col.A = 0.5f;

                    Utils.Line(pos, pos + dir * flowFieldVisCellSize, col, ref v, ref i, _vertList, _colList,
                        _indexList);
                }
            }
        }

        // obstacles
        Span<Obstacle> obstacles = _obstaclePool.AsSpan();
        foreach (Obstacle obs in obstacles)
        {
            if (obs.Empty()) continue;
            switch (obs.Shape3D)
            {
                case ObstacleShape.Circle:
                {
                    Utils.Circle(obs.Position.To3D(), 32, obs.Size, Colors.Brown, ref v, ref i, _vertList,
                        _colList, _indexList);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        Debug.Assert(v < _vertList.Length, "v < _vertList.Length");
        Debug.Assert(v < _colList.Length, "v < _colList.Length");
        Debug.Assert(i < _indexList.Length, "i < _indexList.Length");

        Span<Godot.Vector3> verts = _vertList.AsSpan(0, v);
        Span<Color> colours = _colList.AsSpan(0, v);
        Span<int> indices = _indexList.AsSpan(0, i);

        Array arrays = new();
        arrays.Resize((int) ArrayMesh.ArrayType.Max);
        arrays[(int) ArrayMesh.ArrayType.Vertex] = verts.ToArray();
        arrays[(int) ArrayMesh.ArrayType.Color] = colours.ToArray();
        arrays[(int) ArrayMesh.ArrayType.Index] = indices.ToArray();

        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);

        mesh = outMesh;
    }
}
