using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using Godot.Collections;

public partial class MultiViewportMeshInstance : MeshInstance3D
{
    [Export] private int _numExtraMeshes;
    [Export(PropertyHint.Flags, "0,1,2,3,4,5,6,7")] private Array<uint> _meshLayers; 
    [Export] private Array<Material> _meshMaterials; 
    
    private List<MeshInstance3D> _meshes = new();
    private List<ShaderMaterial> _shaders = new();
    private bool _createdMeshes;

    public List<MeshInstance3D> AltMeshes => _meshes;
    public List<ShaderMaterial> AltShaders => _shaders;
    
    public override void _Ready()
    {
        base._Ready();
        
        if (_numExtraMeshes == 0)
            return;
        
        Debug.Assert(_meshLayers.Count == _numExtraMeshes && _meshMaterials.Count == _numExtraMeshes);
        for (int i = 0; i < _numExtraMeshes; i++)
        {
            MeshInstance3D mesh = new();
            mesh.Mesh = Mesh;
            mesh.Layers = _meshLayers[i];
            mesh.MaterialOverride = _meshMaterials[i].Duplicate() as ShaderMaterial;
            mesh.Transform = Transform;
            _meshes.Add(mesh);
            mesh.Visible = Visible;
            mesh.CastShadow = ShadowCastingSetting.Off;
            _shaders.Add(mesh.MaterialOverride as ShaderMaterial);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!_createdMeshes)
        {
            for (int i = _meshes.Count - 1; i >= 0; i--)
            {
                MeshInstance3D mesh = _meshes[i];
                GetParent().AddChild(mesh);
            }
            _createdMeshes = true;
        }
    }

    public void SetMeshTransform(Transform3D transform)
    {
        foreach (MeshInstance3D mesh in _meshes)
        {
            mesh.Transform = transform;
        }
    }

    public void ClearAltMeshes()
    {
        foreach (MeshInstance3D mesh in AltMeshes)
        {
            mesh.QueueFree();
        }
        AltMeshes.Clear();
    }
}
