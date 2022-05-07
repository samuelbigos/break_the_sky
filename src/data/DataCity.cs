using Godot;
using System;
using System.Collections.Generic;

public class DataCity : DataEntry
{
    [Export] public string DisplayName;
    [Export] private List<NodePath> Connections;

    public override void _Ready()
    {
        base._Ready();
        
        _data.Add("DisplayName", DisplayName);
        _data.Add("Connections", Connections);
    }
}
