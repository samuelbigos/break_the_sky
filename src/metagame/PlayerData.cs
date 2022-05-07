using Godot;
using System;
using Godot.Collections;

public class PlayerData : Saveable
{
    public static PlayerData Instance;

    public int Level
    {
        get => Convert.ToInt32(_data["level"]);
        set => _data["level"] = value;
    }

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        DoCreateNew();
    }
    
    public void Reset()
    {
        DoCreateNew();
    }

    private void DoCreateNew()
    {
        _data = new Dictionary();

        _data["level"] = 0;
    }
}
