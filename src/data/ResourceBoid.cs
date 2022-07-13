using Godot;

public class ResourceBoid : Resource
{
    [Export] public string UniqueID;
    [Export] public string DisplayName;
    [Export] public PackedScene Scene;
    [Export] public Mesh Mesh;
}