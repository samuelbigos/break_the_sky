using Godot;
using System;
using System.Diagnostics;

public partial class Singleton<T> : Node where T : class
{
    private static Singleton<T> _instance;
    public static T Instance => _instance as T;

    public override void _EnterTree()
    {
        base._EnterTree();

        Debug.Assert(Instance == null, $"Attempting to create multiple {typeof(T)} instances!");
        _instance = this;
    }
}
