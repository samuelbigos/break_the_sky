using Godot;
using System;

public partial class FlockingManager : Singleton<FlockingManager>
{
    private static Intersection CircleIntersection(Boid boid, Obstacle obstacle, float range)
    {
        Intersection intersection = new();
        
        float obstacleRadius = obstacle.Size + boid.SeparationRadius;
        
        intersection.Intersect = false;
        intersection.Range = range;
        
        https://stackoverflow.com/a/1079478
        
        // from centre of obstacle to boid
        Vector2 obstacleToBoid = obstacle.Position - boid.Position;
        Vector2 forward = boid.Heading * intersection.Range;
        
        // early exit if we're inside the obstacle
        float distFromObstacle = (boid.Position - obstacle.Position).Length();
        if (distFromObstacle < obstacleRadius)
        {
            intersection.Intersect = true;
            intersection.Distance = distFromObstacle;
            intersection.SurfacePoint = obstacle.Position - obstacleToBoid.Normalized() * obstacleRadius;
            intersection.SurfaceNormal = -obstacleToBoid.Normalized();
        }
        
        // project
        float k = obstacleToBoid.Dot(forward) / forward.Dot(forward);
        Vector2 d = forward * k;
        d += boid.Position;
        
        // behind, don't care
        if ((d - boid.Position).Dot(boid.Heading) < 0.0f)
            return intersection;
        
        // out of range, don't care
        // if ((d - boid.Position).Length() > intersection.Range)
        //     return intersection;

        // no collision, don't care
        float dist = (d - obstacle.Position).Length() - obstacleRadius;
        if (dist > 0.0f)
            return intersection;

        intersection.Intersect = true;
        intersection.Distance = dist;
        intersection.SurfacePoint = d;
        intersection.SurfaceNormal = (d - obstacle.Position).Normalized();
        
        return intersection;
    }

    private static Vector2 LocalisePosition(Boid boid, Vector2 globalPos)
    {
        Vector2 globalOffset = globalPos - boid.Position;
        return LocaliseDirection(boid, globalOffset);
    }

    private static Vector2 LocaliseDirection(Boid boid, Vector2 globalDir)
    {
        Vector2 forward = boid.Heading;
        Vector2 right = new(forward.y, -forward.x);
        return new Vector2(globalDir.Dot(forward), globalDir.Dot(right));
    }

    private static float Sq(float x)
    {
        return x * x;
    }
}
