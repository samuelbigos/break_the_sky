#if TOOLS
using Godot;

[Tool]
public class CloudTextureGenerator : EditorPlugin
{
    private Control _dock;

    public override void _EnterTree()   
    {
        base._EnterTree();

        _dock = ResourceLoader.Load<PackedScene>("res://addons/cloud_texture_generator/GeneratorToolbox.tscn").Instance<Control>();
        AddControlToDock(DockSlot.RightUr, _dock);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        RemoveControlFromDocks(_dock);
        
        _dock.Free();
    }
}
#endif