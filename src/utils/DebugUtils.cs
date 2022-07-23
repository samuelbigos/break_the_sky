using System;
using Godot;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

public static class DebugUtils
{
    public static bool Assert(bool condition, string message)
    {
        if (!condition)
        {
            Error(message);
        }

        return condition;
    }
    
    public static void Error(string message)
    {
        OS.Alert(message, "Error!");
        GD.PushError(message);
    }
}