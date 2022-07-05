using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using ImGuiNET;

public class DebugImGui : Saveable
{
    public static DebugImGui Instance;
    
    [Export] private PackedScene _initialScene;
    
    public static Action DrawImGuiMenuBar = null;
    
    public static bool ManualInputHandling = false;
    
    private float _fps;
    private float _timescale;
    private List<(string, string, Action)> _registeredWindows = new();
    
    private Godot.Collections.Dictionary<string, object> _defaults = new()
    {
        { "performance", false },
        { "debug", false },
        { "gamesettings", false }
    };

    private bool ShowPerformance
    {
        get => Convert.ToBoolean(_data["performance"]);
        set { _data["performance"] = value; SaveManager.DoSave();}
    }

    private bool ShowDebug
    {
        get => Convert.ToBoolean(_data["debug"]);
        set { _data["debug"] = value; SaveManager.DoSave(); }
    }
    
    private bool ShowGameSettings
    {
        get => Convert.ToBoolean(_data["gamesettings"]);
        set { _data["gamesettings"] = value; SaveManager.DoSave(); }
    }

    public override void _Ready()
    {
        base._Ready();
        
        GetNode<ImGuiNode>("ImGuiNode").Connect("IGLayout", this, nameof(_OnImGuiLayout));

        _timescale = Engine.TimeScale;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        
        Instance = this;
    }

    public override void InitialiseSaveData()
    {
        Validate();
    }

    protected override void Validate()
    {
        foreach (string key in _defaults.Keys)
        {
            if (!_data.Contains(key))
            {
                _data[key] = _defaults[key];
            }
        }

        foreach ((string, string, Action) window in _registeredWindows)
        {
            if (!_data.Contains(window.Item1))
            {
                _data[window.Item1] = false;
            }
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        delta = TimeSystem.UnscaledDelta;
        if (delta != 0.0f)
        {
            float fps = 1.0f / Performance.GetMonitor(Performance.Monitor.TimeProcess);
            _fps = 0.033f * fps + 0.966f * _fps;
        }
    }
    
    public override void _Input(InputEvent evt)
    {
        if (ImGuiGD.ProcessInput(evt) && !ManualInputHandling)
        {
            GetTree().SetInputAsHandled();
        }

        if (evt.IsActionPressed("debug_show_performance"))
            ShowPerformance = !ShowPerformance;
        if (evt.IsActionPressed("debug_show_debug"))
            ShowDebug = !ShowDebug;
        if (evt.IsActionPressed("debug_show_gamesettings"))
            ShowGameSettings = !ShowGameSettings;
    }

    public void RegisterWindow(string id, string name, Action callback)
    {
        _registeredWindows.Add((id, name, callback));
        
        if (!_data.Contains(id))
            _data[id] = false;
    }
    
    public void UnRegisterWindow(string id, Action callback)
    {
        int index = -1;
        for (int i = 0; i < _registeredWindows.Count; i++)
        {
            (string, string, Action) window = _registeredWindows[i];
            if (window.Item1 == id)
            {
                index = i;
                break;
            }
        }
        if (index != -1)
            _registeredWindows.RemoveAt(index);
    }

    private void _OnImGuiLayout()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Menu"))
            {
                if (ImGui.MenuItem("Reset Save"))
                {
                    SaveManager.Instance.Reset();
                    GetTree().ChangeSceneTo(_initialScene);
                }
                ImGui.EndMenu();
            }
            
            if (ImGui.BeginMenu("Windows"))
            {
                if (ImGui.MenuItem("Performance", "CTRL+P", ShowPerformance))
                {
                    ShowPerformance = !ShowPerformance;
                }
                if (ImGui.MenuItem("Debug", "CTRL+D", ShowDebug))
                {
                    ShowDebug = !ShowDebug;
                }
                if (ImGui.MenuItem("GameSettings", "CTRL+G", ShowGameSettings))
                {
                    ShowGameSettings = !ShowGameSettings;
                }

                foreach ((string, string, Action) window in _registeredWindows)
                {
                    bool selected = Convert.ToBoolean(_data[window.Item1]);
                    if (ImGui.MenuItem($"{window.Item2}", "", selected))
                    {
                        _data[window.Item1] = !selected;
                        SaveManager.DoSave();
                    }
                }
                ImGui.EndMenu();
            }
            
            DrawImGuiMenuBar?.Invoke();
            
            ImGui.EndMainMenuBar();
        }

        const float windowAlpha = 0.75f;
        ImGuiWindowFlags flags = ImGuiWindowFlags.AlwaysAutoResize;

        if (ShowPerformance)
        {
            ImGui.SetNextWindowBgAlpha(windowAlpha);
            if (ImGui.Begin("Performance", flags))
            {
                ImGui.Text($"FPS: {_fps:F0}");
                
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
                ImGui.Text($"MemoryDynamic: {Performance.GetMonitor(Performance.Monitor.MemoryDynamic) / 1024.0f:F0}KiB");
                ImGui.Text($"MemoryStatic: {Performance.GetMonitor(Performance.Monitor.MemoryStatic) / 1024.0f:F0}KiB");
                ImGui.Text($"MemoryMessageBufferMax: {Performance.GetMonitor(Performance.Monitor.MemoryMessageBufferMax) / 1024.0f:F0}KiB");

                ImGui.Text(" ### Physics");
                ImGui.Text($"Physics3dActiveObjects: {Performance.GetMonitor(Performance.Monitor.Physics3dActiveObjects):F0}");
                ImGui.Text($"Physics2dActiveObjects: {Performance.GetMonitor(Performance.Monitor.Physics2dActiveObjects):F0}");
                ImGui.Text($"Physics3dIslandCount: {Performance.GetMonitor(Performance.Monitor.Physics3dIslandCount):F0}KiB");
                ImGui.Text($"Physics2dIslandCount: {Performance.GetMonitor(Performance.Monitor.Physics2dIslandCount):F0}KiB");

                ImGui.End();
            }
        }

        if (ShowDebug)
        {
            ImGui.SetNextWindowBgAlpha(windowAlpha);
            if (ImGui.Begin("Debug", flags))
            {
                if (ImGui.SliderFloat("Timescale", ref _timescale, 0.0f, 1.0f))
                {
                    Engine.TimeScale = _timescale;
                }
                ImGui.End();
            }
        }

        if (ShowGameSettings)
        {
            ImGui.SetNextWindowBgAlpha(windowAlpha);
            if (ImGui.Begin("GameSettings", flags))
            {
                Resources.Instance.GameSettings._OnImGuiLayout();
            }
        }

        foreach ((string, string, Action) window in _registeredWindows)
        {
            if (Convert.ToBoolean(_data[window.Item1]))
            {
                ImGui.SetNextWindowBgAlpha(windowAlpha);
                if (ImGui.Begin(window.Item2, flags))
                {
                    window.Item3?.Invoke();
                }
            }
        }
    }
}
