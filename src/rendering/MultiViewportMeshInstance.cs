using Godot;
using System;
using System.Collections.Generic;

public class MultiViewportMeshInstance : MeshInstance
{
    [Export(PropertyHint.Flags, "0,1,2,3,4,5,6,7")] private int _meshLayers; 
    
    private List<MeshInstance> _meshes;
    
    public override void _Ready()
    {
        
    }
}
