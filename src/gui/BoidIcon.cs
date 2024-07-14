using Godot;
using System;

public partial class BoidIcon : Control
{
    [Export] private NodePath _viewportPath;
    [Export] private NodePath _meshInstancePath;
    [Export] private NodePath _buttonPath;
    [Export] private NodePath _progressPath;
    [Export] private float _rotSpeed = 5.0f;
    [Export] private bool _showProgress = true;

    public Action<BoidIcon, ResourceBoid> OnPressed;

    private MeshInstance3D _mesh;
    private Button _button;
    private ProgressBar _progress;
    private ResourceBoid _boidData;

    public override void _Ready()
    {
        base._Ready();

        _mesh = GetNode<MeshInstance3D>(_meshInstancePath);
        _button = GetNode<Button>(_buttonPath);
        _progress = GetNode<ProgressBar>(_progressPath);

        _button.Connect("pressed", new Callable(this, nameof(_OnPressed)));
        
        _progress.Visible = _showProgress;
    }
    
    public void Init(ResourceBoid data, bool showProgress)
    {
        _mesh.Mesh = data.Mesh;
        _boidData = data;
        _progress.Visible = showProgress && _showProgress;
    }

    public void UpdateProgress(float progress)
    {
        _progress.Value = progress;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        delta = TimeSystem.UnscaledDelta;
        
        _mesh.RotateY((float)delta * _rotSpeed);
    }

    private void _OnPressed()
    {
        OnPressed?.Invoke(this, _boidData);
    }
}
