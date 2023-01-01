#if TOOLS
using Godot;
using System;
using GodotOnReady.Attributes;

[Tool]
public class ChannelOptions : VBoxContainer
{
    [Export] private NodePath _fbmOptionsPath;
    [Export] private NodePath _gradientPath;
    [Export] private NodePath _cellularPath;
    [Export] private NodePath _cellularFBMPath;
    [Export] private NodePath _freqPath;
    [Export] private NodePath _lacunarityPath;
    [Export] private NodePath _amplitudePath;
    [Export] private NodePath _octavesLabelPath;
    [Export] private NodePath _octavesPath;
    [Export] private NodePath _invertPath;
    
    private Control _fbmOptions;
    private CheckBox _gradient;
    private CheckBox _cellular;
    private CheckBox _cellularFBM;
    private LineEdit _freq;
    private LineEdit _lacunarity;
    private LineEdit _amplitude;
    private Label _octavesLabel;
    private HSlider _octaves;
    private CheckButton _invert;
    
    public GeneratorToolbox.NoiseType GetNoiseType()
    {
        if (_gradient.Pressed)
            return GeneratorToolbox.NoiseType.Gradient;
        if (_cellular.Pressed)
            return GeneratorToolbox.NoiseType.Cellular;
        if (_cellularFBM.Pressed)
            return GeneratorToolbox.NoiseType.CellularFBM;
        
        return GeneratorToolbox.NoiseType.None;
    }

    public void GetParams(out float freq, out int octaves, out float lac, out float amp, out bool invert)
    {
        freq = _freq.Text.ToFloat();
        octaves = (int) _octaves.Value;
        lac = _lacunarity.Text.ToFloat();
        amp = _amplitude.Text.ToFloat();
        invert = _invert.Pressed;
    }
    
    public override void _Ready()
    {
        base._Ready();

        _fbmOptions = GetNode<Control>(_fbmOptionsPath);
        _gradient = GetNode<CheckBox>(_gradientPath);
        _cellular = GetNode<CheckBox>(_cellularPath);
        _cellularFBM = GetNode<CheckBox>(_cellularFBMPath);
        _freq = GetNode<LineEdit>(_freqPath);
        _lacunarity = GetNode<LineEdit>(_lacunarityPath);
        _amplitude = GetNode<LineEdit>(_amplitudePath);
        _octavesLabel = GetNode<Label>(_octavesLabelPath);
        _octaves = GetNode<HSlider>(_octavesPath);
        _invert = GetNode<CheckButton>(_invertPath);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (_gradient != null)
        {
            bool isFbm = _gradient.Pressed;
            if (isFbm)
            {
                _octavesLabel.Text = $"Octaves: {_octaves.Value}";
            }

            _fbmOptions.Visible = isFbm;
        }
    }
}
#endif