using Godot;
using System;

public static class Utils
{
    public static Vector2 Flatten(Vector3 vec)
    {
        return new Vector2(vec.x, vec.z);
    }
}