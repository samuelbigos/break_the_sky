#if GODOT_PC || GODOT_WEB || GODOT_MOBILE
#define EXPORT
#endif

using Godot;
using System;
using Vector2 = System.Numerics.Vector2;

public partial class SteeringManager
{
    public enum Behaviours // in order of importance
    {
        Separation,
        AvoidObstacles,
        AvoidAllies,
        AvoidEnemies,
        MaintainSpeed,
        Cohesion,
        Alignment,
        Arrive,
        Pursuit,
        Flee,
        EdgeRepulsion,
        Wander,
        FlowFieldFollow,
        COUNT,
    }
    
    private static Vector2 Steering_Seek(in Boid boid, in Span<BoidSharedProperties> shared, Vector2 position, float limit = 1.0f)
    {
        Vector2 desired = position - boid.Position;
        desired.SetMag(shared[boid.SharedPropertiesIdx].MaxSpeed * limit);

        Vector2 force = desired - boid.Velocity;
        return force;
    }
    
    private static Vector2 Steering_Arrive(in Boid boid, in Span<BoidSharedProperties> shared)
    {
        Vector2 position = boid.Target;
        float radius = 25.0f;//Mathf.Max(1.0f, boid.Speed * boid.LookAhead);
        float dist = (boid.Position - position).Length();
        return Steering_Seek(boid, shared, position, Mathf.Clamp(dist / radius, 0.0f, 1.0f));
    }

    private static Vector2 Steering_Cohesion(in Boid boid, in Span<Boid> boids, in Span<BoidSharedProperties> shared)
    {
        Vector2 centre = Vector2.Zero;
        int count = 0;
        foreach (ref readonly Boid other in boids)
        {
            if (boid.Id == other.Id) continue;
            if (boid.Alignment != other.Alignment) continue;
            if ((boid.Position - other.Position).LengthSquared() > Sq(shared[boid.SharedPropertiesIdx].ViewRange)) continue;
            if (!InView(boid, other, shared[boid.SharedPropertiesIdx].ViewAngle)) continue;

            centre += other.Position;
            count++;
        }

        if (count == 0)
            return Vector2.Zero;

        centre /= count;
        return Steering_Seek(boid, shared, centre);
    }

    private static Vector2 Steering_Align(in Boid boid, in Span<Boid> boids, in Span<BoidSharedProperties> shared)
    {
        Vector2 desired = Vector2.Zero;
        int count = 0;
        foreach (ref readonly Boid other in boids)
        {
            if (boid.Id == other.Id) continue;
            if (boid.Alignment != other.Alignment) continue;
            if ((boid.Position - other.Position).LengthSquared() > Sq(shared[boid.SharedPropertiesIdx].ViewRange)) continue;
            if (!InView(boid, other, shared[boid.SharedPropertiesIdx].ViewAngle)) continue;

            desired += other.Velocity;
            count++;
        }

        if (count == 0)
            return Vector2.Zero;

        desired /= count;
        Vector2 force = desired - boid.Velocity;
        return force;
    }

    private unsafe Vector2[] _tempPosBuffer = new Vector2[500 * sizeof(Vector2) * 2];

    private static Vector2 Steering_Separate(in Boid boid, in BoidSharedProperties shared, in Span<Boid> boids, in Span<Obstacle> obstacles, float delta)
    {
        Vector2 forceSum = Vector2.Zero;
        int count = 0;

        unsafe
        {
            
        }

        // boids
        int n = boids.Length;
        for (int i = 0; i < n; i++)
        {
            ref Boid other = ref boids[i];
            
            if (boid.Id == other.Id) continue;
            Vector2 desired = boid.Position - other.Position;
            float distSq = desired.X * desired.X + desired.Y * desired.Y;
            float radiusSq = Sq(boid.Radius + other.Radius);
            if (distSq > radiusSq)
                continue;

            desired.SetMag(shared.MaxSpeed);
            float t = 1.0f - Mathf.Pow(distSq / radiusSq, 2.0f);
            Vector2 force = desired.Limit(shared.MaxForce * delta * t);
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
            desired.SetMag(shared.MaxSpeed);
            float t = 1.0f - Mathf.Pow(distSq / radiusSq, 2.0f);
            Vector2 force = desired.Limit(shared.MaxForce * delta * t);
            forceSum += force;
            count++;
        }

        return count == 0 ? Vector2.Zero : forceSum;
    }

    private static Vector2 Steering_Pursuit(in Boid boid, in Span<BoidSharedProperties> shared)
    {
        return Steering_Seek(boid, shared, boid.Target);
    }
    
    private static Vector2 Steering_EdgeRepulsion(in Boid boid, in Span<BoidSharedProperties> shared, Rect2 bounds)
    {
        if (bounds.HasPoint(boid.Position.ToGodot()))
            return Vector2.Zero;

        Vector2 closestPointOnEdge = new(Mathf.Max(Mathf.Min(boid.Position.X, bounds.End.x), bounds.Position.x),
            Mathf.Max(Mathf.Min(boid.Position.Y, bounds.End.y), bounds.Position.y));

        return Steering_Seek(boid, shared, closestPointOnEdge);
    }
    
    private static Vector2 Steering_AvoidObstacles(ref Boid boid, in Span<Obstacle> obstacles, in Span<BoidSharedProperties> shared)
    {
        Intersection intersection = default;
        float nearestDistance = 999999.0f;
        float range = boid.Speed * shared[boid.SharedPropertiesIdx].LookAhead;
        
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
#if !EXPORT
        boid.Intersection = intersection;
#endif
        if (intersection.Intersect && intersection.IntersectTime < shared[boid.SharedPropertiesIdx].LookAhead)
        {
            Vector2 force = intersection.SurfaceNormal.PerpendicularComponent(boid.Heading);
            force.SetMag(shared[boid.SharedPropertiesIdx].MaxForce);
            return force;
        }
        return Vector2.Zero;
    }

    private static Vector2 Steering_AvoidAllies(ref Boid boid, in Span<Boid> boids, in Span<BoidSharedProperties> shared)
    {
        return Steering_AvoidBoids(ref boid, boids, shared, true);
    }
    
    private static Vector2 Steering_AvoidEnemies(ref Boid boid, in Span<Boid> boids, in Span<BoidSharedProperties> shared)
    {
        return Steering_AvoidBoids(ref boid, boids, shared, false);
    }

    private static Vector2 Steering_AvoidBoids(ref Boid boid, in Span<Boid> boids, in Span<BoidSharedProperties> shared, bool avoidAllies)
    {
        Intersection intersection = default;
        float nearestDistance = 999999.0f;
        float range = boid.Speed * shared[boid.SharedPropertiesIdx].LookAhead;

        // boids
        if (!intersection.Intersect) // obstacles have priority
        {
            foreach (ref readonly Boid other in boids)
            {
                if (boid.Id == other.Id) continue;
                if (avoidAllies && boid.Alignment != other.Alignment) continue;
                if (!avoidAllies && boid.Alignment == other.Alignment) continue;
                if ((other.Position - boid.Position).LengthSquared() > Sq(range + other.Radius + boid.Radius)) continue;
                if (!InView(boid, other, shared[boid.SharedPropertiesIdx].ViewAngle)) continue;

                bool collision = CollisionDetection(boid.Position, other.Position, boid.Velocity, other.Velocity,
                    boid.Radius, other.Radius,
                    out Vector2 collisionPos, out Vector2 collisionNormal, out float collisionTime);

                if (!collision)
                    continue;

                float dist = (collisionPos - boid.Position).LengthSquared();
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    intersection.SurfaceNormal = collisionNormal;
                    intersection.IntersectTime = collisionTime;
                    intersection.Intersect = true;
                }
            }
        }

        // if nearby intersection found, steer away from it, otherwise no steering
#if !EXPORT
        boid.Intersection = intersection;
#endif
        if (intersection.Intersect && intersection.IntersectTime < shared[boid.SharedPropertiesIdx].LookAhead)
        {
            Vector2 force = intersection.SurfaceNormal.PerpendicularComponent(boid.Heading);
            force.SetMag(shared[boid.SharedPropertiesIdx].MaxForce);
            return force;
        }
        return Vector2.Zero;
    }

    private static Vector2 Steering_MaintainSpeed(in Boid boid, in Span<BoidSharedProperties> shared)
    {
        Vector2 desired = boid.Heading * shared[boid.SharedPropertiesIdx].DesiredSpeed;
        Vector2 force = desired - boid.Velocity;
        return force;
    }
    
    private static Vector2 Steering_Wander(ref Boid boid, in Span<BoidSharedProperties> shared, float delta)
    {
        Vector2 circleCentre = boid.Position + boid.Heading * shared[boid.SharedPropertiesIdx].WanderCircleDist;
        float angle = -boid.Heading.AngleTo(Vector2.UnitX) + shared[boid.SharedPropertiesIdx].WanderAngle;
        Vector2 displacement = Vector2.Normalize(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))) * shared[boid.SharedPropertiesIdx].WanderCircleRadius;
        shared[boid.SharedPropertiesIdx].WanderAngle += (Utils.Rng.Randf() - 0.5f) * delta * shared[boid.SharedPropertiesIdx].WanderVariance;

        Vector2 desired = (circleCentre + displacement) - boid.Position;
        Vector2 force = desired - boid.Velocity;
        return force;
    }

    private static Vector2 Steering_FlowFieldFollow(in Boid boid, in Span<FlowField> flowFields, in Span<BoidSharedProperties> shared)
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
        desired.SetMag(shared[boid.SharedPropertiesIdx].MaxSpeed);
        return desired - boid.Velocity;
    }
}
