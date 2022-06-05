using Godot;
using System;
using System.Collections.Generic;
using ImGuiNET;
using Array = Godot.Collections.Array;

public partial class SteeringManager
{
    private bool _drawSeparation = true;
    private bool _drawSteering = true;
    private bool _drawVelocity = true;
    private bool _drawVision = true;
    private bool _drawAvoidance = true;
    private bool _drawWander = true;
    
    private List<Vector3> _vertList = new();
    private List<Color> _colList = new();
    private List<int> _indexList = new();

    public void DrawSimulationToMesh(out Mesh mesh)
    {
        ArrayMesh outMesh = new();
        int v = 0;

        // boids
        Span<Boid> boids = _boidPool.AsSpan(0, _numBoids);
        foreach (ref readonly Boid boid in boids)
        {
            // body
            Vector2 forward = boid.Heading;
            Vector2 right = new(forward.y, -forward.x);
            Color col = boid.Alignment == 0 ? Colors.Blue : Colors.Red;
            Vector2 p0 = boid.Position + forward * -2.5f + right * 3.0f;
            Vector2 p1 = boid.Position + forward * 4.5f;
            Vector2 p2 = boid.Position + forward * -2.5f - right * 3.0f;
            Line(p0, p1, col, ref v);
            Line(p1, p2, col, ref v);
            Line(p2, p0, col, ref v);

            // separation radius
            if (_drawSeparation)
            {
                Circle(boid.Position, 32, boid.Radius, Colors.DarkSlateGray, ref v);
            }

            // boid velocity/force
            if (_drawVelocity)
            {
                Line(boid.Position, boid.Position + boid.Velocity * 25.0f / boid.MaxSpeed, col, ref v);
            }

            if (_drawSteering)
            {
                Line(boid.Position, boid.Position + boid.Steering * 25.0f / boid.MaxForce / TimeSystem.Delta,
                    Colors.Purple, ref v);
            }

            // boid avoidance
            if (boid.Intersection.Intersect && _drawAvoidance)
            {
                //Line(_st, boid.Position, boid.Position + forward * boid.LookAhead * boid.Speed, Colors.Black, ref v);
                Circle(boid.Intersection.SurfacePoint, 8, 1.0f, Colors.Black, ref v);
                Line(boid.Intersection.SurfacePoint,
                    boid.Intersection.SurfacePoint + boid.Intersection.SurfaceNormal * 10.0f, Colors.Black, ref v);
            }

            // view range
            if (_drawVision)
            {
                CircleArc(boid.Position, 64, boid.ViewRange, boid.ViewAngle, boid.Heading, Colors.DarkGray, ref v);
            }

            // wander
            if (_drawWander)
            {
                Vector2 circlePos = boid.Position + boid.Heading * boid.WanderCircleDist;

                float angle = -boid.Heading.AngleTo(Vector2.Right) + boid.WanderAngle;
                Vector2 displacement = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).Normalized() *
                                       boid.WanderCircleRadius;

                Circle(circlePos, 32, boid.WanderCircleRadius, Colors.Black, ref v);
                Line(circlePos, circlePos + displacement, Colors.Black, ref v);
            }
        }

        // edges
        {
            Vector2 p0 = new(EdgeBounds.Position);
            Vector2 p1 = new(EdgeBounds.End.x, EdgeBounds.Position.y);
            Vector2 p2 = new(EdgeBounds.End);
            Vector2 p3 = new(EdgeBounds.Position.x, EdgeBounds.End.y);

            Line(p0, p1, Colors.Black, ref v);
            Line(p1, p2, Colors.Black, ref v);
            Line(p2, p3, Colors.Black, ref v);
            Line(p3, p0, Colors.Black, ref v);

            // obstacles
            foreach (Obstacle obstacle in _obstacles)
            {
                switch (obstacle.Shape)
                {
                    case ObstacleShape.Circle:
                    {
                        Circle(obstacle.Position, 32, obstacle.Size, Colors.Brown, ref v);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } 
        }
        
        Array arrays = new();
        arrays.Resize((int) ArrayMesh.ArrayType.Max);
        arrays[(int) ArrayMesh.ArrayType.Vertex] = _vertList;
        arrays[(int) ArrayMesh.ArrayType.Color] = _colList;
        arrays[(int) ArrayMesh.ArrayType.Index] = _indexList;

        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
        
        _vertList.Clear();
        _colList.Clear();
        _indexList.Clear();

        mesh = outMesh;
    }

    private void Circle(Vector2 pos, int segments, float radius, Color col, ref int v)
    {
        for (int s = 0; s < segments; s++)
        {
            _colList.Add(col);
            float rad = Mathf.Pi * 2.0f * ((float) s / segments);
            Vector3 vert = (pos + new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radius).To3D();
            _vertList.Add(vert);
            _indexList.Add(v + s);
            _indexList.Add(v + (s + 1) % segments);
        }
        v += segments;
    }
    
    private void CircleArc(Vector2 pos, int segments, float radius, float arcDeg, Vector2 heading, Color col, ref int v)
    {
        if (arcDeg >= 360.0f)
        {
            Circle(pos, segments, radius, col, ref v);
            return;
        }
        
        float segmentArc = Mathf.Deg2Rad(arcDeg / (segments - 1));
        float headingAngle = heading.AngleTo(Vector2.Down) - Mathf.Deg2Rad(arcDeg * 0.5f);
        _colList.Add(col);
        _vertList.Add(pos.To3D());
        for (int s = 0; s < segments; s++)
        {
            _colList.Add(col);
            float rad = headingAngle + segmentArc * s;
            Vector3 vert = (pos + new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radius).To3D();
            _vertList.Add(vert);
            _indexList.Add(v + (segments + s - 1) % segments);
            _indexList.Add(v + (segments + s) % segments);
        }
        v += segments + 1;
    }

    private void Line(Vector2 p1, Vector2 p2, Color col, ref int v)
    {
        _colList.Add(col);
        _vertList.Add(p1.To3D());
        _colList.Add(col);
        _vertList.Add(p2.To3D());
        _indexList.Add(v++);
        _indexList.Add(v++);
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
            ImGui.EndTabItem();
        }
    }
}
