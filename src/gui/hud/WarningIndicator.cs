using Godot;
using System.Diagnostics;

public class WarningIndicator : Spatial
{
    [Export] private NodePath _meshPath;
    private MeshInstance _mesh;
    
    public override void _Ready()
    {
        base._Ready();

        _mesh = GetNode<MeshInstance>(_meshPath);
        Debug.Assert(_mesh != null);
        SpatialMaterial mat = _mesh.GetSurfaceMaterial(0) as SpatialMaterial;
        Debug.Assert(mat != null);
        mat.AlbedoColor = ColourManager.Instance.Red;
    }
}
