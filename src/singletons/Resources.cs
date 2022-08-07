using Godot;
using System;
using System.Diagnostics;

public class Resources : Singleton<Resources>
{
    [Export] public PackedScene Tooltip;
    [Export] public ResourceGameSettings ResourceGameSettings;
    
    // VFX
    [Export] public PackedScene HitVFX;
    [Export] public PackedScene DamagedVFX;
    [Export] public PackedScene DustCloudVFX;
    [Export] public PackedScene ExplodeVFX;
}
