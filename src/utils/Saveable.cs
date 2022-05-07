using Godot;
using System;
using Godot.Collections;

public class Saveable : Node
{
    protected Dictionary _data = new Dictionary();
    
    public override void _Ready()
    {
        base._Ready();
        
        AddToGroup("persistent");
    }

    public Dictionary DoSave()
    {
        return _data.Duplicate();
    }

    public void DoLoad(Dictionary data)
    {
        _data = data.Duplicate();
    }
}
