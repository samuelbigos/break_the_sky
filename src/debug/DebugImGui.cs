using Godot;
using System;

public class DebugImGui : Node
{
    public static ImGuiNode DebugImGuiNode;

    public override void _Ready()
    {
        base._Ready();

        DebugImGuiNode = GetNode<ImGuiNode>("ImGuiNode");
    }
}
