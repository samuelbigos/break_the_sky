using Godot;
using System;
using System.Threading;
using ImGuiNET;

public class DebugImGui : Singleton<DebugImGui>
{
    [Export] private PackedScene _initialScene;
    
    public static Action DrawImGui = null;
    public static bool ManualInputHandling = false;
    
    private float _fps = 0.0f;

    public override void _Ready()
    {
        base._Ready();
        
        GetNode<ImGuiNode>("ImGuiNode").Connect("IGLayout", this, nameof(_OnImGuiLayout));
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        delta = TimeSystem.UnscaledDelta;
        if (delta != 0.0f)
        {
            float fps = 1.0f / delta;
            _fps = 0.033f * fps + 0.966f * _fps;
        }
    }
    
    public override void _Input(InputEvent evt)
    {
        if (ImGuiGD.ProcessInput(evt) && !ManualInputHandling)
        {
            GetTree().SetInputAsHandled();
        }
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

            if (ImGui.BeginTabItem("Performance"))
            {
                ImGui.Text(" ### Processing");
                ImGui.Text($"TimeProcess: {Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0f:F0}ms");
                ImGui.Text($"ObjectCount: {Performance.GetMonitor(Performance.Monitor.ObjectCount):F0}");
                ImGui.Text($"ObjectNodeCount: {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount):F0}");
                ImGui.Text($"ObjectResourceCount: {Performance.GetMonitor(Performance.Monitor.ObjectResourceCount):F0}");
                ImGui.Text($"ObjectOrphanNodeCount: {Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount):F0}");
                
                ImGui.Text(" ### Rendering");
                ImGui.Text($"RenderVerticesInFrame: {Performance.GetMonitor(Performance.Monitor.RenderVerticesInFrame):F0}");
                ImGui.Text($"RenderDrawCallsInFrame: {Performance.GetMonitor(Performance.Monitor.RenderDrawCallsInFrame):F0}");
                ImGui.Text($"Render2dDrawCallsInFrame: {Performance.GetMonitor(Performance.Monitor.Render2dDrawCallsInFrame):F0}");
                
                ImGui.Text(" ### Memory");
                ImGui.Text($"MemoryDynamic: {Performance.GetMonitor(Performance.Monitor.MemoryDynamic)/1024.0f:F0}KiB");
                ImGui.Text($"MemoryStatic: {Performance.GetMonitor(Performance.Monitor.MemoryStatic)/1024.0f:F0}KiB");
                ImGui.Text($"MemoryMessageBufferMax: {Performance.GetMonitor(Performance.Monitor.MemoryMessageBufferMax)/1024.0f:F0}KiB");
                
                ImGui.Text(" ### Physics");
                ImGui.Text($"Physics3dActiveObjects: {Performance.GetMonitor(Performance.Monitor.Physics3dActiveObjects):F0}");
                ImGui.Text($"Physics2dActiveObjects: {Performance.GetMonitor(Performance.Monitor.Physics2dActiveObjects):F0}");
                ImGui.Text($"Physics3dIslandCount: {Performance.GetMonitor(Performance.Monitor.Physics3dIslandCount):F0}KiB");
                ImGui.Text($"Physics2dIslandCount: {Performance.GetMonitor(Performance.Monitor.Physics2dIslandCount):F0}KiB");
                
                ImGui.EndTabItem();
            }

            DrawImGui?.Invoke();
            
            ImGui.EndTabBar();
            ImGui.End();
        }
    }
}
