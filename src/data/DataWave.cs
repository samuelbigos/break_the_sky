using Godot;
using System;
using System.Collections.Generic;

public class DataWave : DataEntry
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
    [Export] public List<ResourceBoidEnemy> PrimarySpawns;
    [Export] public List<ResourceBoidEnemy> SecondarySpawns;
}
