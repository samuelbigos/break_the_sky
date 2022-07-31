using Godot;
using System;
using GodotOnReady.Attributes;

public partial class Cursor : Spatial
{
    private static Cursor _instance;
    public static Cursor Instance => _instance;
    
    [OnReadyGet] private MeshInstance _countMesh;
    [OnReadyGet] private MeshInstance _circleMesh;
    [OnReadyGet] private MeshInstance _outerMesh;

    [Export] public int TotalPips = 48;
    [Export] public float RotSpeed = 5.0f;

    public int PipCount = 0;
    public float Size = 1.0f;
    public bool Activated;

    private ShaderMaterial _countMat;

    [OnReady] private void Ready()
    {
        _instance = this;
        
        _countMat = _countMesh.MaterialOverride as ShaderMaterial;
        
        //Input.SetMouseMode(Input.MouseMode.Hidden);
    }

    public void Reset()
    {
        PipCount = 0;
        _outerMesh.Rotation = Vector3.Up * 0.0f;
        _outerMesh.Scale = Vector3.One;
        Size = 1.0f;
        Activated = false;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        GlobalTransform = new Transform(GlobalTransform.basis, GameCamera.Instance.MousePosition.To3D());
        _countMat.SetShaderParam("u_count", PipCount);

        if (Activated)
        {
            _outerMesh.RotateY(delta * RotSpeed);
            _outerMesh.Scale = Vector3.One * Size;
        }
    }
}
