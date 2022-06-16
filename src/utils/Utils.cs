using Godot;

public static class Utils
{
    public static readonly RandomNumberGenerator Rng = new();

    public static Vector2 To2D(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.z);
    }

    public static Vector3 To3D(this Vector2 vec)
    {
        return new Vector3(vec.x, 0.0f, vec.y);
    }

    public static void GlobalPosition(this Spatial spatial, Vector3 position)
    {
        spatial.GlobalTransform = new Transform(spatial.GlobalTransform.basis, position);
    }

    public static float RandfUnit()
    {
        return Rng.RandfRange(-1.0f, 1.0f);
    }

    public static Vector2 RandV2()
    {
        return new Vector2(RandfUnit(), RandfUnit()).Normalized();
    }

    public static float Ease_CubicInOut(float t)
    {
        return t < 0.5f
            ? 4.0f * t * t * t
            : 0.5f * Mathf.Pow(2.0f * t - 2.0f, 3.0f) + 1.0f;
    }

    public static Vector2 RandPointOnEdge(this Rect2 rect)
    {
        Vector2 r = new Vector2(Rng.Randf(), Rng.Randf());
        if (Rng.Randf() > 0.5f)
        {
            r.x = Mathf.Floor(Rng.Randf() + 0.5f);
        }
        else
        {
            r.y = Mathf.Floor(Rng.Randf() + 0.5f);
        }

        return rect.Position + r * rect.Size;
    }

    public static Vector2 Limit(ref this Vector2 vec, float vMax)
    {
        float length = vec.Length();
        if (length == 0.0f)
        {
            return vec;
        }

        float i = vMax / length;
        i = Mathf.Min(i, 1.0f);
        vec *= i;
        return vec;
    }

    public static Vector2 SetMag(ref this Vector2 vec, float vMag)
    {
        vec = vec.Normalized() * vMag;
        return vec;
    }

    public static Vector2 ParallelComponent(this Vector2 vec, Vector2 unitBasis)
    {
        float projection = vec.Dot(unitBasis);
        return unitBasis * projection;
    }

    public static Vector2 PerpendicularComponent(this Vector2 vec, Vector2 unitBasis)
    {
        return vec - vec.ParallelComponent(unitBasis);
    }
    
    public static Vector2 LocaliseDirection(Vector2 globalDirection, Vector2 forward, Vector2 side)
    {
        return new Vector2(globalDirection.Dot(side),
            globalDirection.Dot(forward));
    }
    
    public static Vector2 LocalisePosition(Vector2 globalPosition, Vector2 localPosition, Vector2 forward, Vector2 side)
    {
        Vector2 globalOffset = globalPosition - localPosition;
        return LocaliseDirection(globalOffset, forward, side);
    }
    
    public static Vector2 LocaliseOffset(Vector2 globalOffset, Vector2 forward, Vector2 side)
    {
        return LocaliseDirection(globalOffset, forward, side);
    }

    public static Vector2 GlobaliseOffset(Vector2 localOffset, Vector2 forward, Vector2 side)
    {
        return localOffset.x * side + localOffset.y * forward;
    }

    public static void Line(Vector3 p1, Vector3 p2, Color col, ref int v, ref int i, Vector3[] verts, Color[] cols, int[] indices)
    {
        cols[v] = col;
        verts[v] = p1;
        indices[i++] = v++;
        cols[v] = col;
        verts[v] = p2;
        indices[i++] = v++;
    }
    
    public static void Circle(Vector3 pos, int segments, float radius, Color col, ref int v, ref int i, Vector3[] verts, Color[] cols, int[] indices)
    {
        for (int s = 0; s < segments; s++)
        {
            cols[v + s] = col;
            float rad = Mathf.Pi * 2.0f * ((float) s / segments);
            Vector3 vert = pos + new Vector3(Mathf.Sin(rad), 0.0f, Mathf.Cos(rad)) * radius;
            verts[v + s] = vert;
            indices[i++] = v + s;
            indices[i++] = v + (s + 1) % segments;
        }

        v += segments;
    }
    
    public static void CircleArc(Vector3 pos, int segments, float radius, float arcDeg, Vector2 heading, Color col, ref int v, ref int i, Vector3[] verts, Color[] cols, int[] indices)
    {
        if (arcDeg >= 360.0f)
        {
            Circle(pos, segments, radius, col,ref v, ref i, verts, cols, indices);
            return;
        }
        
        float segmentArc = Mathf.Deg2Rad(arcDeg / (segments - 1));
        float headingAngle = heading.AngleTo(Vector2.Down) - Mathf.Deg2Rad(arcDeg * 0.5f);
        cols[v] = col;
        verts[v] = pos;
        v++;
        for (int s = 0; s < segments; s++)
        {
            cols[v + s] = col;
            float rad = headingAngle + segmentArc * s;
            Vector3 vert = pos + new Vector3(Mathf.Sin(rad), 0.0f, Mathf.Cos(rad)) * radius;
            verts[v + s] = vert;
            indices[i++] = (v - 1 + (segments + s - 1) % segments);
            indices[i++] = (v - 1 + (segments + s) % segments);
        }
        v += segments;
    }
}