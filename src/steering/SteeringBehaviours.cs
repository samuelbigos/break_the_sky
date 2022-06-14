using Godot;
using System;
using System.Collections.Generic;

public partial class SteeringManager
{
    private static Vector2 Steering_Seek(in Boid boid, Vector2 position)
    {
        Vector2 desired = position - boid.Position;
        desired.SetMag(boid.MaxSpeed);

        Vector2 force = desired - boid.Velocity;
        return force;
    }
    
    private static Vector2 Steering_Arrive(in Boid boid, Vector2 position)
    {
        float radius = Mathf.Max(1.0f, boid.Speed * boid.LookAhead);
        float dist = (boid.Position - position).Length();
        if (dist > radius)
        {
            return Steering_Seek(boid, position);
        }
        Vector2 desired = position - boid.Position;
        //desired.Limit(boid.MaxSpeed * (dist / radius));
        Vector2 force = desired - boid.Velocity;
        return force;
    }

    private static Vector2 Steering_Cohesion(in Boid boid, in Span<Boid> boids, float radius)
    {
        Vector2 centre = Vector2.Zero;
        int count = 0;
        foreach (ref readonly Boid other in boids)
        {
            if (boid.Id == other.Id) continue;
            if ((boid.Position - other.Position).LengthSquared() > radius * radius) continue;
            if (!InView(boid, other, boid.ViewAngle)) continue;
            if (boid.Alignment != other.Alignment) continue;

            centre += other.Position;
            count++;
        }

        if (count == 0)
            return Vector2.Zero;

        centre /= count;
        return Steering_Seek(boid, centre);
    }

    private static Vector2 Steering_Align(in Boid boid, in Span<Boid> boids, float radius)
    {
        Vector2 desired = Vector2.Zero;
        int count = 0;
        foreach (ref readonly Boid other in boids)
        {
            if (boid.Id == other.Id) continue;
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
        return force;
    }

    private static Vector2 Steering_Separate(in Boid boid, in Span<Boid> boids, in Span<Obstacle> obstacles, float delta)
    {
        Vector2 forceSum = Vector2.Zero;
        int count = 0;
        
        // boids
        foreach (ref readonly Boid other in boids)
        {
            if (boid.Id == other.Id) continue;
            float distSq = (boid.Position - other.Position).LengthSquared();
            float radiusSq = Sq(boid.Radius + other.Radius);
            if (distSq > radiusSq)
                continue;

            Vector2 desired = boid.Position - other.Position;
            desired.SetMag(boid.MaxSpeed);
            float t = 1.0f - Mathf.Pow(distSq / radiusSq, 2.0f);
            Vector2 force = desired.Limit(boid.MaxForce * delta * t);
            forceSum += force;
            count++;
        }

        // obstacles
        for (int i = 0; i < obstacles.Length; i++)
        {
            Obstacle other = obstacles[i];
            float distSq = (boid.Position - other.Position).LengthSquared();
            float radiusSq = boid.Radius + other.Size;
            if (distSq > radiusSq)
                continue;

            Vector2 desired = boid.Position - other.Position;
            desired.SetMag(boid.MaxSpeed);
            float t = 1.0f - Mathf.Pow(distSq / radiusSq, 2.0f);
            Vector2 force = desired.Limit(boid.MaxForce * delta * t);
            forceSum += force;
            count++;
        }

        return count == 0 ? Vector2.Zero : forceSum;
    }

    private static Vector2 Steering_Pursuit(in Boid boid, Vector2 position)
    {
        return Steering_Seek(boid, position);
    }
    
    private static Vector2 Steering_EdgeRepulsion(in Boid boid, Rect2 bounds)
    {
        if (bounds.HasPoint(boid.Position))
            return Vector2.Zero;

        Vector2 closestPointOnEdge = new(Mathf.Max(Mathf.Min(boid.Position.x, bounds.End.x), bounds.Position.x),
            Mathf.Max(Mathf.Min(boid.Position.y, bounds.End.y), bounds.Position.y));

        return Steering_Seek(boid, closestPointOnEdge);
    }
    
    private static Vector2 Steering_AvoidObstacles(ref Boid boid, in Span<Obstacle> obstacles)
    {
        Intersection intersection = default;
        float nearestDistance = 999999.0f;
        float range = boid.Speed * boid.LookAhead;
        
        // obstacles
        for (int i = 0; i < obstacles.Length; i++)
        {
            Obstacle obstacle = obstacles[i];
            switch (obstacle.Shape)
            {
                case ObstacleShape.Circle:
                {
                    if ((obstacle.Position - boid.Position).Length() > range + obstacle.Size + boid.Radius)
                        continue;

                    bool collision = CollisionDetection(boid.Position, obstacle.Position, boid.Velocity, Vector2.Zero,
                        boid.Radius, obstacle.Size,
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

        // if nearby intersection found, steer away from it, otherwise no steering
        boid.Intersection = intersection;
        if (intersection.Intersect && intersection.IntersectTime < boid.LookAhead)
        {
            Vector2 force = intersection.SurfaceNormal.PerpendicularComponent(boid.Heading);
            force.SetMag(boid.MaxForce);
            return force;
        }
        return Vector2.Zero;
    }

    private static Vector2 Steering_AvoidAllies(ref Boid boid, in Span<Boid> boids)
    {
        return Steering_AvoidBoids(ref boid, boids, true);
    }
    
    private static Vector2 Steering_AvoidEnemies(ref Boid boid, in Span<Boid> boids)
    {
        return Steering_AvoidBoids(ref boid, boids, false);
    }

    private static Vector2 Steering_AvoidBoids(ref Boid boid, in Span<Boid> boids, bool avoidAllies)
    {
        Intersection intersection = default;
        float nearestDistance = 999999.0f;
        float range = boid.Speed * boid.LookAhead;

        // boids
        if (!intersection.Intersect) // obstacles have priority
        {
            foreach (ref readonly Boid other in boids)
            {
                if (boid.Id == other.Id) continue;
                if ((other.Position - boid.Position).LengthSquared() > Sq(range + other.Radius + boid.Radius)) continue;
                if (!InView(boid, other, boid.ViewAngle)) continue;
                if (avoidAllies && boid.Alignment != other.Alignment) continue;
                if (!avoidAllies && boid.Alignment == other.Alignment) continue;
                
                bool collision = CollisionDetection(boid.Position, other.Position, boid.Velocity, other.Velocity,
                    boid.Radius, other.Radius,
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
            force.SetMag(boid.MaxForce);
            return force;
        }
        return Vector2.Zero;
    }

    private static Vector2 Steering_MaintainSpeed(in Boid boid)
    {
        Vector2 desired = boid.Heading * boid.DesiredSpeed;
        Vector2 force = desired - boid.Velocity;
        return force;
    }
    
    private static Vector2 Steering_Wander(ref Boid boid, float delta)
    {
        Vector2 circleCentre = boid.Position + boid.Heading * boid.WanderCircleDist;
        float angle = -boid.Heading.AngleTo(Vector2.Right) + boid.WanderAngle;
        Vector2 displacement = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).Normalized() * boid.WanderCircleRadius;
        boid.WanderAngle += (Utils.Rng.Randf() - 0.5f) * delta * boid.WanderVariance;

        Vector2 desired = (circleCentre + displacement) - boid.Position;
        Vector2 force = desired - boid.Velocity;
        return force;
    }

    private static Vector2 Steering_FlowFieldFollow(in Boid boid, Span<FlowField> flowFields)
    {
        Vector2 desired = Vector2.Zero;
        int count = 0;
        foreach (FlowField flowField in flowFields)
        {
            Vector2 pos = boid.Position + boid.Heading * 10.0f;
            if (TryGetFieldAtPosition(flowField, boid.Position, out Vector2 v))
            {
                desired += v;
                count++;
            }
        }

        if (count == 0)
            return Vector2.Zero;

        desired /= count;
        desired.SetMag(boid.MaxSpeed);
        return desired - boid.Velocity;
    }
}
