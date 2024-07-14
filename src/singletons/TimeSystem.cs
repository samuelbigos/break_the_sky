using Godot;
using System;
using System.Diagnostics;

public partial class TimeSystem : Node
{
    public static float UnscaledDelta;
    public static double Delta;
    
    private Stopwatch _stopwatch = new Stopwatch();
    private float _stopwatchTickMs;
    
    public override void _Ready()
    {
        base._Ready();
        
        _stopwatchTickMs = 1.0f / Stopwatch.Frequency;
        _stopwatch.Start();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Delta = delta;
        UnscaledDelta = _stopwatch.ElapsedTicks * _stopwatchTickMs;
        _stopwatch.Restart();
    }
}
