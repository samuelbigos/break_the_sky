using Godot;
using System;
using System.Drawing.Drawing2D;

public partial class FlockingManager : Singleton<FlockingManager>
{
    private SurfaceTool _st = new();

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
            Circle(_st, boid.Position, 32, boid.SeparationRadius, Colors.Black, ref v);
            
            // boid velocity/force
            Line(_st, boid.Position, boid.Position + boid.Velocity * 15.0f / boid.MaxSpeed, Colors.Teal, ref v);
            Line(_st, boid.Position, boid.Position + boid.Steering * 15.0f / boid.MaxForce / TimeSystem.Delta, Colors.Purple, ref v);
            
            // boid avoidance
            //if (boid.Intersection.Intersect)
            {
                Line(_st, boid.Position, boid.Position + forward * boid.Intersection.Range, Colors.Black, ref v);
                
                
                Circle(_st, boid.Intersection.SurfacePoint, 8, 2.0f, Colors.Black, ref v);
                Line(_st, boid.Intersection.SurfacePoint, boid.Intersection.SurfacePoint + boid.Intersection.SurfaceNormal * 10.0f, Colors.Green, ref v);
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

    private static void Line(SurfaceTool st, Vector2 p1, Vector2 p2, Color col, ref int v)
    {
        st.AddColor(col);
        st.AddVertex(p1.To3D());
        st.AddColor(col);
        st.AddVertex(p2.To3D());
        st.AddIndex(v++);
        st.AddIndex(v++);
    }
}
