using Godot;

public class BoidTestbedCamera : Camera
{
    public static BoidTestbedCamera Instance;
    
    private Transform _initialTrans;

    public Vector2 MousePosition()
    {
        Vector2 pos = GetViewport().GetMousePosition();
        Vector3 origin = ProjectRayOrigin(pos);
        Vector3 normal = ProjectRayNormal(pos);
        Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * GlobalTransform.origin.y;
        return new Vector2(hit.x, hit.z);
    }
    
    public Vector2 ScreenPosition(Vector2 screen)
    {
        Vector3 origin = ProjectRayOrigin(screen);
        Vector3 normal = ProjectRayNormal(screen);
        Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * GlobalTransform.origin.y;
        return new Vector2(hit.x, hit.z);
    }
    
    public override void _Ready()
    {
        base._Ready();
        
        VisualServer.SetDefaultClearColor(ColourManager.Instance.Primary);
        
        _initialTrans = GlobalTransform;
        Instance = this;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        VisualServer.SetDefaultClearColor(ColourManager.Instance.Primary);
        
        Vector2 cameraMouseOffset = MousePosition() - Vector2.Zero;
        Vector2 cameraOffset = cameraMouseOffset * 0.33f;
        Transform cameraTransform = new(GlobalTransform.basis, cameraOffset.To3D());
        cameraTransform.origin.y = _initialTrans.origin.y;
        GlobalTransform = cameraTransform;

        Vector3 pos = _initialTrans.origin;
        if (Input.IsActionJustReleased("zoom_in"))
        {
            pos.y = Mathf.Max(pos.y - 10.0f, 100.0f);
        }
        if (Input.IsActionJustReleased("zoom_out"))
        {
            pos.y = Mathf.Min(pos.y + 10.0f, 999.0f);
        }
        _initialTrans = new Transform(_initialTrans.basis,  pos);
    }
}