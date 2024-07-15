using Godot;
using System.Diagnostics;

public partial class WarningIndicator : Node3D
{
    [Export] private float _flashDistance;
    [Export] private float _flashTime;
    [Export] private MeshInstance3D _mesh;
    [Export] private StandardMaterial3D _mat;
    
    private int _flashState;
    private float _flashingTimer;
    
    public BoidBase Target;
    
    public override void _Ready()
    {
        base._Ready();

        _mesh.MaterialOverride = _mat;
        _mat.AlbedoColor = ColourManager.Instance.Red;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        float distance = (GlobalTransform.Origin - Target.GlobalTransform.Origin).LengthSquared();
        if (distance < Mathf.Pow(_flashDistance, 2.0f))
        {
            _flashingTimer -= (float)delta;
            if (_flashingTimer <= 0.0f)
            {
                _flashingTimer = 0.1f;
                _mat.AlbedoColor = _flashState == 1 ? ColourManager.Instance.White : ColourManager.Instance.Red;
                _flashState = (_flashState + 1) % 2;
            }
        }
        else
        {
            _mat.AlbedoColor = ColourManager.Instance.Red;
        }
    }
}
