using Godot;

public static class Utils
{
    public static RandomNumberGenerator Rng = new RandomNumberGenerator();

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

    public static Vector2 VecLimitDeviationAngleUtility (bool insideOrOutside, Vector2 source, float cosineOfConeAngle, Vector2 basis)
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
}