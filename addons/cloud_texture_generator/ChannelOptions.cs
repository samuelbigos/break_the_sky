using Godot;
using System;
using GodotOnReady.Attributes;

[Tool]
public partial class ChannelOptions : VBoxContainer
{
    [OnReadyGet] public CheckBox Gradient;
    [OnReadyGet] public CheckBox Worley;
    [OnReadyGet] public CheckBox GradientWorley;
    [OnReadyGet] public LineEdit Freq;
    [OnReadyGet] public LineEdit Lacunarity;
    [OnReadyGet] public LineEdit Amplitude;
    [OnReadyGet] public Label OctavesLabel;
    [OnReadyGet] public HSlider Octaves;

    public override void _Process(float delta)
    {
        base._Process(delta);

        OctavesLabel.Text = $"Octaves: {Octaves.Value}";
    }
}
