using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class MultiViewportMeshInstance : MeshInstance
{
    [Export] private int _numExtraMeshes = 0;
    [Export(PropertyHint.Flags, "0,1,2,3,4,5,6,7")] private List<uint> _meshLayers; 
    [Export] private List<Material> _meshMaterials; 
    
    private List<MeshInstance> _meshes = new List<MeshInstance>();
    private bool _createdMeshes = false;

    public List<MeshInstance> AltMeshes => _meshes;
    
    public override void _Ready()
    {
        base._Ready();
        
        if (_numExtraMeshes == 0)
            return;
        
        Debug.Assert(_meshLayers.Count == _numExtraMeshes && _meshMaterials.Count == _numExtraMeshes);
        for (int i = 0; i < _numExtraMeshes; i++)
        {
            MeshInstance mesh = new MeshInstance();
            mesh.Mesh = Mesh;
            mesh.Layers = _meshLayers[i];
            mesh.MaterialOverride = _meshMaterials[i];
            _meshes.Add(mesh);
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (!_createdMeshes)
        {
            for (int i = _meshes.Count - 1; i >= 0; i--)
            {
                MeshInstance mesh = _meshes[i];
                GetParent().AddChild(mesh);
            }
            _createdMeshes = true;
        }
    }
}
