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
    [Export] public float YFromCamera = 250.0f;

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
        
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector3 origin = GameCamera.Instance.ProjectRayOrigin(mouse);
        Vector3 normal = GameCamera.Instance.ProjectRayNormal(mouse);
        Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * YFromCamera;
        Vector3 pos = new(hit.x, GameCamera.Instance.GlobalTransform.origin.y - YFromCamera, hit.z);

        GlobalTransform = new Transform(GlobalTransform.basis, pos);
        _countMat.SetShaderParam("u_count", PipCount);

        if (Activated)
        {
            _outerMesh.RotateY(delta * RotSpeed);
            _outerMesh.Scale = Vector3.One * Size;
        }
    }
}
