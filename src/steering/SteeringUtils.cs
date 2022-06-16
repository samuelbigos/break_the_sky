using Godot;
using System;

public partial class SteeringManager
{
    private static Vector2 ApplyMinimumSpeed(Boid boid, Vector2 force, float minSpeed)
    {
        if (boid.Speed > minSpeed || force == Vector2.Zero || boid.Speed == 0.0f)
        {
            return force;
        }

        float range = boid.Speed / minSpeed;
        float cosine = Mathf.Lerp(1.0f, -1.0f, Mathf.Pow(range, 5));
        return VecLimitDeviationAngleUtility(true, force, cosine, boid.Velocity.Normalized());
    }

    private static Vector2 VecLimitDeviationAngleUtility(bool insideOrOutside, Vector2 source, float cosineOfConeAngle, Vector2 basis)
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

    private static Vector2 WrapPosition(Vector2 pos, Rect2 edge)
    {
        pos -= edge.Position;
        pos += edge.Size;
        pos.x %= edge.Size.x;
        pos.y %= edge.Size.y;
        pos += edge.Position;
        return pos;
    }
    
    // TODO: optimise to not use AngleTo
    private static bool InView(Boid boid, Boid other, float viewDegrees)
    {
        Vector2 toOther = other.Position - boid.Position;
        Vector2 viewDir = Vector2.Up;
        if (boid.Velocity != Vector2.Zero)
            viewDir = boid.Velocity.Normalized();
        float angleRad = viewDir.AngleTo(toOther);
        return Mathf.Abs(angleRad) < Mathf.Deg2Rad(viewDegrees * 0.5f);
    }
    
    private static float Sq(float x)
    {
        return x * x;
    }

    private static bool CollisionDetection(Vector2 pA, Vector2 pB, Vector2 vA, Vector2 vB, float rA, float rB, 
        out Vector2 collisionPos, out Vector2 collisionNormal, out float collisionTime)
    {
        collisionPos = Vector2.Zero;
        collisionNormal = Vector2.Zero;
        collisionTime = -9999.0f;
        
        // http://twobitcoder.blogspot.com/2010/04/circle-collision-detection.html

        Vector2 pAB = pA - pB;
        Vector2 vAB = vA - vB;

        float a = vAB.Dot(vAB);
        float b = pAB.Dot(vAB) * 2.0f;
        float c = pAB.Dot(pAB) - Sq(rA + rB);
        
        float discriminant = Sq(b) - (4 * a * c);
        
        // no collision
        if (discriminant < 0.0f)
        {
            return false;
        }

        discriminant = Mathf.Sqrt(discriminant);
        float t1 = (-b - discriminant)/(2*a);
        float t2 = (-b + discriminant)/(2*a);

        collisionTime = Mathf.Min(t1, t2);

        // collision in past
        if (t1 < 0.0f && t2 < 0.0f)
        {
            return false;
        }
        
        // collision in progress, return point between the two
        if (t1 < 0.0f && t2 > 0.0f)
        {
            collisionPos = pA + (pB - pA) * (rA / (rA + rB));
            collisionNormal = (pA - pB).Normalized();
            return true;
        }

        Vector2 cA = pA + vA * collisionTime;
        Vector2 cB = pB + vB * collisionTime;

        collisionPos = cA + (cB - cA).Normalized() * rA;
        collisionNormal = (collisionPos - (pB + vB * collisionTime)).Normalized();
        return true;
    }

    public static bool TryGetFieldAtPosition(FlowField flowField, Vector2 pos, out Vector2 field)
    {
        field = Vector2.Zero;
        if (!flowField.Bounds.HasPoint(pos))
            return false;
        
        Vector2 flowFieldSize = new(flowField.Resource.X, flowField.Resource.Y);
        Vector2 uv = pos - flowField.Position;
        uv.x /= flowField.Size.x / flowField.Resource.X;
        uv.y /= flowField.Size.y / flowField.Resource.Y;
        uv += flowFieldSize * 0.5f;

        uv.x = Mathf.Clamp(uv.x, 0, flowField.Resource.X - 1);
        uv.y = Mathf.Clamp(uv.y, 0, flowField.Resource.Y - 1);
        field = flowField.Resource.VectorAt((int) uv.x, (int) uv.y);
        return true;
    }
}
