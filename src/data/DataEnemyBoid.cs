using Godot;
using System;
using System.Collections.Generic;

public class DataEnemyBoid : DataEntry
{
    [Export] public string DisplayName;
    [Export] public PackedScene Scene;
    [Export] public float SpawningCost;
    [Export] public int MaterialDropCount;

    public override void _Ready()
    {
        base._Ready();
    }
}
