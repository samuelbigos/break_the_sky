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
    [Export] public List<string> PrimarySpawns;
    [Export] public List<string> SecondarySpawns;
}
