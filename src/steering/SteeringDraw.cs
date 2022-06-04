using Godot;
using System;
using System.Drawing.Drawing2D;
using ImGuiNET;

public partial class SteeringManager
{
    private readonly SurfaceTool _st = new();

    private static bool _drawSeparation = true;
    private static bool _drawSteering = true;
    private static bool _drawVelocity = true;
    private static bool _drawVision = true;
    private static bool _drawAvoidance = true;
    private static bool _drawWander = true;
    
    public void DrawSimulationToMesh(out Mesh mesh)
    {
        ArrayMesh outMesh = new();
        int v = 0;
        
        _st.Begin(Mesh.PrimitiveType.Lines);
        
        // boids
        foreach (Boid boid in _boids)
        {
            // body
            Vector2 forward = boid.Heading;
            Vector2 right = new(forward.y, -forward.x);
            Color col = boid.Alignment == 0 ? Colors.Blue : Colors.Red;
            Vector2 p0 = boid.Position + forward * -2.5f + right * 3.0f;
            Vector2 p1 = boid.Position + forward * 4.5f;
            Vector2 p2 = boid.Position + forward * -2.5f - right * 3.0f;
            Line(_st, p0, p1, col, ref v);
            Line(_st, p1, p2, col, ref v);
            Line(_st, p2, p0, col, ref v);
            
            // separation radius
            if (_drawSeparation)
            {
                Circle(_st, boid.Position, 32, boid.Radius, Colors.DarkSlateGray, ref v);
            }
            
            // boid velocity/force
            if (_drawVelocity)
            {
                Line(_st, boid.Position, boid.Position + boid.Velocity * 25.0f / boid.MaxSpeed, col, ref v);
            }
            if (_drawSteering)
            {
                Line(_st, boid.Position, boid.Position + boid.Steering * 25.0f / boid.MaxForce / TimeSystem.Delta, Colors.Purple, ref v);
            }
            
            // boid avoidance
            if (boid.Intersection.Intersect && _drawAvoidance)
            {
                //Line(_st, boid.Position, boid.Position + forward * boid.LookAhead * boid.Speed, Colors.Black, ref v);
                Circle(_st, boid.Intersection.SurfacePoint, 8, 1.0f, Colors.Black, ref v);
                Line(_st,  boid.Intersection.SurfacePoint,  boid.Intersection.SurfacePoint +  boid.Intersection.SurfaceNormal * 10.0f, Colors.Black, ref v);
            }
            
            // view range
            if (_drawVision)
            {
                CircleArc(_st, boid.Position, 64, boid.ViewRange, boid.ViewAngle, boid.Heading, Colors.DarkGray, ref v);
            }
            
            // wander
            if (_drawWander)
            {
                Vector2 circlePos = boid.Position + boid.Heading * boid.WanderCircleDist;
                
                float angle = -boid.Heading.AngleTo(Vector2.Right) + boid.WanderAngle;
                Vector2 displacement = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).Normalized() * boid.WanderCircleRadius;
                
                Circle(_st, circlePos, 32, boid.WanderCircleRadius, Colors.Black, ref v);
                Line(_st, circlePos, circlePos + displacement, Colors.Black, ref v);
            }
        }
        
        // edges
        {
            Vector2 p0 = new(EdgeBounds.Position);
            Vector2 p1 = new(EdgeBounds.End.x, EdgeBounds.Position.y);
            Vector2 p2 = new(EdgeBounds.End);
            Vector2 p3 = new(EdgeBounds.Position.x, EdgeBounds.End.y);
            
            Line(_st, p0, p1, Colors.Black, ref v);
            Line(_st, p1, p2, Colors.Black, ref v);
            Line(_st, p2, p3, Colors.Black, ref v);
            Line(_st, p3, p0, Colors.Black, ref v);
        }
        
        // obstacles
        foreach (Obstacle obstacle in _obstacles)
        {
            switch (obstacle.Shape)
            {
                case ObstacleShape.Circle:
                {
                    Circle(_st, obstacle.Position, 32, obstacle.Size, Colors.Brown, ref v);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, _st.CommitToArrays());

        mesh = outMesh;
    }

    private static void Circle(SurfaceTool st, Vector2 pos, int segments, float radius, Color col, ref int v)
    {
        for (int s = 0; s < segments; s++)
        {
            st.AddColor(col);
            float rad = Mathf.Pi * 2.0f * ((float) s / segments);
            Vector3 vert = (pos + new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radius).To3D();
            st.AddVertex(vert);
            st.AddIndex(v + s);
            st.AddIndex(v + (s + 1) % segments);
        }
        v += segments;
    }
    
    private static void CircleArc(SurfaceTool st, Vector2 pos, int segments, float radius, float arcDeg, Vector2 heading, Color col, ref int v)
    {
        if (arcDeg >= 360.0f)
        {
            Circle(st, pos, segments, radius, col, ref v);
            return;
        }
        
        float segmentArc = Mathf.Deg2Rad(arcDeg / (segments - 1));
        float headingAngle = heading.AngleTo(Vector2.Down) - Mathf.Deg2Rad(arcDeg * 0.5f);
        st.AddColor(col);
        st.AddVertex(pos.To3D());
        for (int s = 0; s < segments; s++)
        {
            st.AddColor(col);
            float rad = headingAngle + segmentArc * s;
            Vector3 vert = (pos + new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radius).To3D();
            st.AddVertex(vert);
            st.AddIndex(v + (segments + s - 1) % segments);
            st.AddIndex(v + (segments + s) % segments);
        }
        v += segments + 1;
    }

    private static void Line(SurfaceTool st, Vector2 p1, Vector2 p2, Color col, ref int v)
    {
        st.AddColor(col);
        st.AddVertex(p1.To3D());
        st.AddColor(col);
        st.AddVertex(p2.To3D());
        st.AddIndex(v++);
        st.AddIndex(v++);
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

    private static void _OnImGuiLayout()
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
