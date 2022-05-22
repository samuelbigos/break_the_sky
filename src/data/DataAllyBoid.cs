using Godot;
using System;
using System.Collections.Generic;

public class DataAllyBoid : DataEntry
{
    [Export] public string DisplayName;
    [Export] public PackedScene Scene;
    [Export] public Mesh Mesh;
    [Export] public int FabricateCost;
    [Export] public float FabricateTime;

    public override void _Ready()
    {
        base._Ready();
    }
}
