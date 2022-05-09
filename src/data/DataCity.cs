using Godot;
using System;
using System.Collections.Generic;

public class DataCity : DataEntry
{
    [Export] public string DisplayName;
    [Export] public List<NodePath> Connections;

    public override void _Ready()
    {
        base._Ready();
    }
}