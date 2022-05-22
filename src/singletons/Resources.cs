using Godot;
using System;
using System.Diagnostics;

public class Resources : Node
{
    public static Resources Instance;
    
    [Export] public PackedScene Tooltip;

    public Resources()
    {
        Debug.Assert(Instance == null, "Attempting to create multiple Resources instances!");
        Instance = this;
    }
}
