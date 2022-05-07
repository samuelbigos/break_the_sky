using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

public class DataEntry : Node
{
    protected Dictionary _data = new Dictionary();

    public T FindProperty<T>(string name)
    {
        return (T) Convert.ChangeType(_data[name], typeof(T));
    }
}
