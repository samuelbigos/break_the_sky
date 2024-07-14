using Godot;

public partial class Cursor : Node3D
{
    private static Cursor _instance;
    public static Cursor Instance => _instance;
    
    [Export] private MeshInstance3D _countMesh;
    [Export] private MeshInstance3D _circleMesh;
    [Export] private MeshInstance3D _outerMesh;

    [Export] public float BaseRadius = 50.0f;
    [Export] public int TotalPips = 48;
    [Export] public float RotSpeed = 5.0f;
    [Export] public float YFromCamera = 250.0f;

    public float Radius => BaseRadius * Size;
    public float RadiusSq => Radius * Radius;
    
    public int PipCount = 0;
    public float Size = 1.0f;
    public bool Activated;

    private ShaderMaterial _countMat;
    
    public override void _Ready()
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

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector3 origin = GameCamera.Instance.ProjectRayOrigin(mouse);
        Vector3 normal = GameCamera.Instance.ProjectRayNormal(mouse);
        Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * YFromCamera;
        Vector3 pos = new(hit.X, GameCamera.Instance.GlobalTransform.Origin.Y - YFromCamera, hit.Z);

        GlobalTransform = new Transform3D(GlobalTransform.Basis, pos);
        _countMat.SetShaderParameter("u_count", PipCount);

        if (Activated)
        {
            _outerMesh.RotateY((float)delta * RotSpeed);
            _outerMesh.Scale = Vector3.One * Size;
        }
    }
}
