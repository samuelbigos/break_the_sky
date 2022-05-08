using Godot;
using System;
using Godot.Collections;
using Array = Godot.Collections.Array;

public class SaveDataPlayer : Saveable
{
    public static SaveDataPlayer Instance;

    public int MaxAllyCount
    {
        get => Convert.ToInt32(_data["maxAllyCount"]);
        set => _data["maxAllyCount"] = value;
    }
    
    public int InitialAllyCount
    {
        get => Convert.ToInt32(_data["initialAllyCount"]);
        set => _data["initialAllyCount"] = value;
    }

    public Array ActiveDrones => _data["activeDrones"] as Array;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;
    }

    public override void InitialiseSaveData()
    {
        Array activeDrones = new Array();
        _data["activeDrones"] = activeDrones;
        _data["maxAllyCount"] = 50;
        _data["initialAllyCount"] = 10;
    }
}
