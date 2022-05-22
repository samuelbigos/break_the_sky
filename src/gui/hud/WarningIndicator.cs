using Godot;
using System.Diagnostics;

public class WarningIndicator : Spatial
{
    [Export] private float _flashDistance;
    [Export] private float _flashTime;
    [Export] private NodePath _meshPath;
    
    private MeshInstance _mesh;
    private SpatialMaterial _mat;
    private int _flashState;
    private float _flashingTimer;
    
    public BoidBase Target;
    
    public override void _Ready()
    {
        base._Ready();

        _mesh = GetNode<MeshInstance>(_meshPath);
        Debug.Assert(_mesh != null);
        _mat = _mesh.GetSurfaceMaterial(0) as SpatialMaterial;
        Debug.Assert(_mat != null);
        _mat.AlbedoColor = ColourManager.Instance.Red;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        float distance = (GlobalTransform.origin - Target.GlobalTransform.origin).LengthSquared();
        if (distance < Mathf.Pow(_flashDistance, 2.0f))
        {
            _flashingTimer -= delta;
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
