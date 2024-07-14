using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

public partial class DataWave : DataEntry
{
    public enum Type
    {
        Standard,
        Escort,
        Swarm,
    }
    
    [Export] public bool Introduction;
    [Export] public Type WaveType;
    [Export] public float TriggerTimeMinutes;
    [Export] public float TriggerBudget;
    [Export] public Array<ResourceBoidEnemy> PrimarySpawns;
    [Export] public Array<ResourceBoidEnemy> SecondarySpawns;
}
