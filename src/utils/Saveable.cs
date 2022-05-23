using Godot;
using System;
using Godot.Collections;

public abstract class Saveable : Node
{
    protected Dictionary _data = new Dictionary();
    
    public override void _Ready()
    {
        base._Ready();
        
        AddToGroup("persistent");
    }
    
    public abstract void InitialiseSaveData();

    public void Reset()
    {
        _data = new Dictionary();
    }

    protected abstract void Validate();

    public virtual Dictionary DoSave()
    {
        return _data.Duplicate();
    }

    public virtual void DoLoad(Dictionary data)
    {
        _data = data.Duplicate();
        Validate();
    }
}
