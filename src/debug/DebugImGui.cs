using Godot;
using System;
using ImGuiNET;

public class DebugImGui : Node
{
    [Export] private PackedScene _initialScene;
    
    public static Action DrawImGui = null;
    
    private float _fps = 0.0f;

    public override void _Ready()
    {
        base._Ready();
        
        GetNode<ImGuiNode>("ImGuiNode").Connect("IGLayout", this, nameof(_OnImGuiLayout));
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        float fps = 1.0f / delta;
        _fps = 0.033f * fps + 0.966f * _fps;
    }

    private void _OnImGuiLayout()
    {
        if (ImGui.Begin("Debug Menu", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text($"FPS: {_fps:F0}");
            ImGui.BeginTabBar("Debug Menu#left_tabs_bar");

            if (ImGui.BeginTabItem("Debug"))
            {
                if (ImGui.Button("Reset Save"))
                {
                    SaveManager.Instance.Reset();
                    GetTree().ChangeSceneTo(_initialScene);
                }

                ImGui.EndTabItem();
            }

            DrawImGui?.Invoke();
            
            ImGui.EndTabBar();
            ImGui.End();
        }
    }
}
