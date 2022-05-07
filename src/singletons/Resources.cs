using Godot;
using System;

public class Resources : Node
{
    public static Resources Instance;
    
    [Export] public PackedScene Tooltip;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;
    }
}
