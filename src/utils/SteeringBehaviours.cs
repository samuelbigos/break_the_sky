using Godot;
using System;

public partial class FlockingManager
{
    private static Vector2 Steering_Seek(Boid boid, float delta, Vector2 position)
    {
        Vector2 desired = position - boid.Position;
        desired *= boid.MaxSpeed / desired.Length();

        Vector2 force = desired - boid.Velocity;
        return force.SetMag(boid.MaxForce * delta);
    }
    
    private static Vector2 Steering_Arrive(Boid boid, float delta, Vector2 position, float radius)
    {
        float dist = (boid.Position - position).Length();
        if (dist > radius)
        {
            return Steering_Seek(boid, delta, position);
        }
        Vector2 desired = position - boid.Position;
        desired.SetMag(boid.MaxSpeed * (dist / radius));
        Vector2 force = desired - boid.Velocity;
        return force.Limit(boid.MaxForce * delta);
    }

    private Vector2 Steering_Cohesion(int i, float delta, float radius)
    {
        Boid boid = _boids[i];
        Vector2 centre = Vector2.Zero;
        int count = 0;
        for (int j = 0; j < _boids.Count; j++)
        {
            Boid other = _boids[j];
            if (i == j) continue;
            if ((boid.Position - other.Position).LengthSquared() > radius * radius) continue;
            if (!InView(boid, other, boid.ViewAngle)) continue;
            if (boid.Alignment != other.Alignment) continue;

            centre += other.Position;
            count++;
        }

        if (count == 0)
            return Vector2.Zero;

        centre /= count;
        return Steering_Seek(boid, delta, centre);
    }

    private Vector2 Steering_Align(int i, float delta, float radius)
    {
        Boid boid = _boids[i];
        Vector2 desired = Vector2.Zero;
        int count = 0;
        for (int j = 0; j < _boids.Count; j++)
        {
            Boid other = _boids[j];
            if (i == j) continue;
            if ((boid.Position - other.Position).LengthSquared() > radius * radius) continue;
            if (!InView(boid, other, boid.ViewAngle)) continue;
            if (boid.Alignment != other.Alignment) continue;

            desired += other.Velocity;
            count++;
        }

        if (count == 0)
            return Vector2.Zero;

        desired /= count;
        Vector2 force = desired - boid.Velocity;
        return force.Limit(boid.MaxForce * delta);
    }

    private Vector2 Steering_Separate(int i, float delta)
    {
        Boid boid = _boids[i];
        
        Vector2 forceSum = Vector2.Zero;
        int count = 0;
        
        // boids
        for (int j = 0; j < _boids.Count; j++)
        {
            if (i == j) continue;
            Boid other = _boids[j];
            float dist = (boid.Position - other.Position).Length();
            float radius = boid.Radius + other.Radius;
            if (dist > radius)
                continue;

            Vector2 desired = boid.Position - other.Position;
            desired.SetMag(boid.MaxSpeed);
            float t = 1.0f - Mathf.Pow(dist / radius, 3.0f);
            Vector2 force = desired.Limit(boid.MaxForce * delta * t);
            forceSum += force;
            count++;
        } 
        
        // obstacles
        for (int j = 0; j < _obstacles.Count; j++)
        {
            if (i == j) continue;
            Obstacle other = _obstacles[j];
            float dist = (boid.Position - other.Position).Length();
            float radius = boid.Radius + other.Size;
            if (dist > radius)
                continue;

            Vector2 desired = boid.Position - other.Position;
            desired.SetMag(boid.MaxSpeed);
            float t = 1.0f - Mathf.Pow(dist / radius, 3.0f);
            Vector2 force = desired.Limit(boid.MaxForce * delta * t);
            forceSum += force;
            count++;
        }   
        
        if (count == 0)
            return Vector2.Zero;

        return forceSum.Limit(boid.MaxForce * delta);
    }

    private Vector2 Steering_Pursuit(Boid boid, float delta, Vector2 position)
    {
        return Steering_Seek(boid, delta, position);
    }
    
    private Vector2 Steering_EdgeRepulsion(Boid boid, float delta, Rect2 bounds, float weight)
    {
        if (bounds.HasPoint(boid.Position))
            return Vector2.Zero;

        Vector2 closestPointOnEdge = new(Mathf.Max(Mathf.Min(boid.Position.x, bounds.End.x), bounds.Position.x),
            Mathf.Max(Mathf.Min(boid.Position.y, bounds.End.y), bounds.Position.y));

        return Steering_Seek(boid, delta, closestPointOnEdge) * weight;
    }
    
    private Vector2 Steering_Avoidance(ref Boid boid, int i, float delta)
    {
        Intersection intersection = default;
        float nearestDistance = 999999.0f;
        
        // obstacles
        float range = boid.Speed * boid.LookAhead;
        foreach (Obstacle obstacle in _obstacles)
        {
            switch (obstacle.Shape)
            {
                case ObstacleShape.Circle:
                {
                    if ((obstacle.Position - boid.Position).Length() > range + obstacle.Size + boid.Radius)
                        continue;
                    
                    bool collision = CollisionDetection(boid.Position, obstacle.Position, boid.Velocity, Vector2.Zero, boid.Radius, obstacle.Size,
                        out Vector2 collisionPos, out Vector2 collisionNormal, out float collisionTime);
                    
                    if (!collision)
                        continue;
                    
                    float dist = (collisionPos - boid.Position).LengthSquared();
                    if (dist < nearestDistance)
                    {
                        nearestDistance = dist;
                        intersection.SurfacePoint = collisionPos;
                        intersection.SurfaceNormal = collisionNormal;
                        intersection.IntersectTime = collisionTime;
                        intersection.Intersect = true;
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // boids
        if (!intersection.Intersect) // obstacles have priority
        {
            for (int j = 0; j < _boids.Count; j++)
            {
                if (i == j)
                    continue;
            
                Boid other = _boids[j];
                if ((other.Position - boid.Position).Length() > range + other.Radius + boid.Radius) continue;
                if (!InView(boid, other, boid.ViewAngle)) continue;
                if (boid.Alignment == 0 && other.Alignment == 0) continue; // allied boids don't avoid each other
            
                bool collision = CollisionDetection(boid.Position, other.Position, boid.Velocity, other.Velocity, boid.Radius, boid.Radius, 
                    out Vector2 collisionPos, out Vector2 collisionNormal, out float collisionTime);
            
                if (!collision)
                    continue;
            
                float dist = (collisionPos - boid.Position).LengthSquared();
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    intersection.SurfacePoint = collisionPos;
                    intersection.SurfaceNormal = collisionNormal;
                    intersection.IntersectTime = collisionTime;
                    intersection.Intersect = true;
                }
            }
        }

        // if nearby intersection found, steer away from it, otherwise no steering
        boid.Intersection = intersection;
        if (intersection.Intersect && intersection.IntersectTime < boid.LookAhead)
        {
            Vector2 force = intersection.SurfaceNormal.PerpendicularComponent(boid.Heading);
            force.SetMag(boid.MaxForce * delta);
            return force;
        }
        return Vector2.Zero;
    }
}
