using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using Array = Godot.Collections.Array;

public partial class SteeringManager
{
    private bool _drawSeparation = true;
    private bool _drawSteering = true;
    private bool _drawVelocity = true;
    private bool _drawVision = false;
    private bool _drawAvoidance = true;
    private bool _drawWander = true;
    private bool _drawFlowFields = false;
    
    private Vector3[] _vertList = new Vector3[50000];
    private Color[] _colList = new Color[50000];
    private int[] _indexList = new int[100000];

    public void DrawSimulationToMesh(out Mesh mesh)
    {
        ArrayMesh outMesh = new();
        int v = 0;
        int i = 0;

        // boids
        Span<Boid> boids = _boidPool.AsSpan(0, _numBoids);
        foreach (ref readonly Boid boid in boids)
        {
            // body
            Vector3 boidPos = boid.Position.To3D();
            Vector3 forward = boid.Heading.To3D();
            Vector3 right = new(forward.z, 0.0f, -forward.x);
            Color col = boid.Alignment == 0 ? Colors.Blue : Colors.Red;
            Vector3 p0 = boidPos + forward * -2.5f + right * 3.0f;
            Vector3 p1 = boidPos + forward * 4.5f;
            Vector3 p2 = boidPos + forward * -2.5f - right * 3.0f;
            Utils.Line(p0, p1, col, ref v, ref i, _vertList, _colList, _indexList);
            Utils.Line(p1, p2, col, ref v, ref i, _vertList, _colList, _indexList);
            Utils.Line(p2, p0, col, ref v, ref i, _vertList, _colList, _indexList);
            
            // separation radius
            if (_drawSeparation)
            {
                Utils.Circle(boidPos, 32, boid.Radius, Colors.DarkSlateGray, ref v, ref i, _vertList, _colList, _indexList);
            }

            // boid velocity/force
            if (_drawVelocity)
            {
                Utils.Line(boidPos, boidPos + boid.Velocity.To3D() * 25.0f / boid.MaxSpeed, col, ref v, ref i, _vertList, _colList, _indexList);
            }

            if (_drawSteering)
            {
                Utils.Line(boidPos, boidPos + boid.Steering.To3D() * 25.0f / boid.MaxForce / TimeSystem.Delta,
                    Colors.Purple, ref v, ref i, _vertList, _colList, _indexList);
            }

            // boid avoidance
            if (boid.Intersection.Intersect && _drawAvoidance)
            {
                //Line(_st, boid.Position, boid.Position + forward * boid.LookAhead * boid.Speed, Colors.Black, ref v);
                Vector3 surface = boid.Intersection.SurfacePoint.To3D();
                Utils.Circle(surface, 8, 1.0f, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
                Utils.Line(surface, surface + boid.Intersection.SurfaceNormal.To3D() * 10.0f, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
            }

            // view range
            if (_drawVision)
            {
                Utils.CircleArc(boidPos, 32, boid.ViewRange, boid.ViewAngle, boid.Heading, Colors.DarkGray, ref v, ref i, _vertList, _colList, _indexList);
            }

            // wander
            if (_drawWander)
            {
                Vector3 circlePos = boidPos + boid.Heading.To3D() * boid.WanderCircleDist;

                float angle = -boid.Heading.AngleTo(Vector2.Right) + boid.WanderAngle;
                Vector3 displacement = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle)).Normalized() *  boid.WanderCircleRadius;

                Utils.Circle(circlePos, 32, boid.WanderCircleRadius, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
                Utils.Line(circlePos, circlePos + displacement, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
            }

            if (_drawFlowFields)
            {
                Utils.Line(boidPos, boidPos + forward * boid.FlowFieldDist, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
                Utils.Circle(boidPos + forward * boid.FlowFieldDist, 8, 2.0f, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
            }
        }
        
        // flow fields
        if (_drawFlowFields)
        {
            float flowFieldVisCellSize = 10.0f;
            Vector2 topLeft = BoidTestbedCamera.Instance.ScreenPosition(Vector2.Zero);
            Vector2 botRight = BoidTestbedCamera.Instance.ScreenPosition(GetViewport().Size);
            
            for (float x = topLeft.x; x < botRight.x; x += flowFieldVisCellSize)
            {
                for (float y = topLeft.y; y < botRight.y; y += flowFieldVisCellSize)
                {
                    Vector3 pos = new Vector3(x, 0.0f, y);// / flowFieldVisCellSize;
                    // pos = pos.Floor();
                    // pos *= flowFieldVisCellSize;
                    
                    Vector3 dir = Vector3.Zero;
                    int count = 0;
                    for (int ff = 0; ff < _numFlowFields; ff++)
                    {
                        FlowField flowField = _flowFieldPool[ff];
                        if (TryGetFieldAtPosition(flowField, pos.To2D(), out Vector2 vector))
                        {
                            dir.x += vector.x;
                            dir.z += vector.y;
                            count++;
                        }
                    }

                    if (count > 0)
                        dir /= count;

                    Color col = new(
                        Mathf.Lerp(0.0f, 1.0f, dir.x * 0.5f + 0.5f),
                        Mathf.Lerp(0.0f, 1.0f, dir.y * 0.5f + 0.5f),
                        Mathf.Lerp(0.0f, 1.0f, 1.0f - (dir.y * 0.5f + 0.5f)));
                    
                    Utils.Line(pos, pos + dir * flowFieldVisCellSize, col, ref v, ref i, _vertList, _colList, _indexList);
                }
            }
        }

        // edges
        {
            Vector3 p0 = new(EdgeBounds.Position.To3D());
            Vector3 p1 = new(EdgeBounds.End.x, 0.0f, EdgeBounds.Position.y);
            Vector3 p2 = new(EdgeBounds.End.To3D());
            Vector3 p3 = new(EdgeBounds.Position.x, 0.0f, EdgeBounds.End.y);

            Utils.Line(p0, p1, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
            Utils.Line(p1, p2, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
            Utils.Line(p2, p3, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);
            Utils.Line(p3, p0, Colors.Black, ref v, ref i, _vertList, _colList, _indexList);

            // obstacles
            foreach (Obstacle obstacle in _obstaclePool)
            {
                switch (obstacle.Shape)
                {
                    case ObstacleShape.Circle:
                    {
                        Utils.Circle(obstacle.Position.To3D(), 32, obstacle.Size, Colors.Brown, ref v, ref i, _vertList, _colList, _indexList);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } 
        }
        
        Debug.Assert(v < _vertList.Length, "v < _vertList.Length");
        Debug.Assert(v < _colList.Length, "v < _colList.Length");
        Debug.Assert(i < _indexList.Length, "i < _indexList.Length");

        Span<Vector3> verts = _vertList.AsSpan(0, v);
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
    
    public override void _EnterTree()
    {
        base._EnterTree();
        DebugImGui.DrawImGui += _OnImGuiLayout;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DebugImGui.DrawImGui -= _OnImGuiLayout;
    }

    private void _OnImGuiLayout()
    {
        if (ImGui.BeginTabItem("Steering"))
        {
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
}
