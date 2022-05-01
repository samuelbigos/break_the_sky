using Godot;
using System;

public static class Utils
{
    public static Vector2 To2D(this Vector3 vec)
    {
        return new Vector2(vec.x, vec.z);
    }
    
    public static Vector3 To3D(this Vector2 vec)
    {
        return new Vector3(vec.x, 0.0f, vec.y);
    }
    
    public static Vector2 Truncate(this Vector2 vec, float vMax)
    {
        float length = vec.Length();
        if (length == 0.0f)
        {
            return vec;
        }

        float i = vMax / vec.Length();
        i = Mathf.Min(i, 1.0f);
        return vec * i;
    }
    
    public static Vector3 Truncate(this Vector3 vec, float vMax)
    {
        float length = vec.Length();
        if (length == 0.0f)
        {
            return vec;
        }

        float i = vMax / vec.Length();
        i = Mathf.Min(i, 1.0f);
        return vec * i;
    }

    public static Vector2 ClampVec(this Vector2 vec, float vMin, float vMax)
    {
        float length = vec.Length();
        if (length == 0.0f)
        {
            return vec;
        }

        float i = vec.Length();
        i = Mathf.Clamp(i, vMin, vMax);
        return vec.Normalized() * i;
    }
    
    public static Vector3 ClampVec(this Vector3 vec, float vMin, float vMax)
    {
        float length = vec.Length();
        if (length == 0.0f)
        {
            return vec;
        }

        float i = vec.Length();
        i = Mathf.Clamp(i, vMin, vMax);
        return vec.Normalized() * i;
    }
}