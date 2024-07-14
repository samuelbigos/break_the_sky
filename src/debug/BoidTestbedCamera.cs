using Godot;

public partial class BoidTestbedCamera : Camera3D
{
    public static BoidTestbedCamera Instance;
    
    private Transform3D _initialTrans;

    public Vector2 MousePosition()
    {
        Vector2 pos = GetViewport().GetMousePosition();
        Vector3 origin = ProjectRayOrigin(pos);
        Vector3 normal = ProjectRayNormal(pos);
        Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * GlobalTransform.Origin.Y;
        return new Vector2(hit.X, hit.Z);
    }
    
    public Vector2 ScreenPosition(Vector2 screen)
    {
        Vector3 origin = ProjectRayOrigin(screen);
        Vector3 normal = ProjectRayNormal(screen);
        Vector3 hit = origin + normal * (1.0f / Vector3.Down.Dot(normal)) * GlobalTransform.Origin.Y;
        return new Vector2(hit.X, hit.Z);
    }
    
    public override void _Ready()
    {
        base._Ready();
        
        _initialTrans = GlobalTransform;
        Instance = this;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        Vector2 cameraMouseOffset = MousePosition() - Vector2.Zero;
        Vector2 cameraOffset = cameraMouseOffset * 0.5f;
        Transform3D cameraTransform = new(GlobalTransform.Basis, cameraOffset.To3D());
        cameraTransform.Origin.Y = _initialTrans.Origin.Y;
        //GlobalTransform = cameraTransform;

        Vector3 pos = _initialTrans.Origin;
        
        Vector3 offset = Vector3.Zero;
        if (Input.IsActionJustReleased("zoom_in"))
        {
            pos.Y = Mathf.Max(pos.Y - 10.0f, 100.0f);
            offset += Vector3.Forward * (float)delta * 10.0f;
        }
        if (Input.IsActionJustReleased("zoom_out"))
        {
            pos.Y = Mathf.Min(pos.Y + 10.0f, 999.0f);
            offset += Vector3.Back * (float)delta * 10.0f;
        }
        _initialTrans = new Transform3D(_initialTrans.Basis,  pos);

        if (Input.IsActionPressed("w"))
        {
            offset += Vector3.Up * (float)delta;
        }
        if (Input.IsActionPressed("a"))
        {
            offset += Vector3.Left * (float)delta;
        }
        if (Input.IsActionPressed("s"))
        {
            offset += Vector3.Down * (float)delta;
        }
        if (Input.IsActionPressed("d"))
        {
            offset += Vector3.Right * (float)delta;
        }

        GlobalTransform = GlobalTransform.Translated(offset * 100.0f);
    }
}