using Godot;
using System;
using System.Diagnostics;
using Godot.Collections;
using ImGuiNET;

public class SaveDataSettings : Saveable
{
    public static SaveDataSettings Instance;

    private Dictionary<string, object> _defaults = new()
    {
        { "fullscreen", false },
        { "windowed_resolution", new Vector2(1440, 810) },
    };

    private static bool Fullscreen
    {
        get => Convert.ToBoolean(Instance._data["fullscreen"]);
        set => Instance._data["fullscreen"] = value;
    }
    
    private static Vector2 WindowedResolution
    {
        get => Utils.Vector2FromString(Convert.ToString(Instance._data["windowed_resolution"]));
        set => Instance._data["windowed_resolution"] = value;
    }

    public SaveDataSettings()
    {
        Debug.Assert(Instance == null, "Attempting to create multiple SaveDataSettings instances!");
        Instance = this;
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
    }

    public override void InitialiseSaveData()
    {
        Validate();
    }

    public override void DoLoad(Dictionary data)
    {
        base.DoLoad(data);
        
        SetFullscreen(Fullscreen);
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        DebugImGui.Instance.RegisterWindow("savedata_settings", "Settings", _OnImGuiLayout);
        GetViewport().Connect("size_changed", this, nameof(_OnWindowSizeChanged));
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DebugImGui.Instance.UnRegisterWindow("savedata_settings", _OnImGuiLayout);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionPressed("toggle_fullscreen"))
        {
            SetFullscreen(!Fullscreen);
        }
    }

    private void SetFullscreen(bool fullscreen)
    {
        if (fullscreen)
        {
            WindowedResolution = OS.WindowSize;
            OS.WindowFullscreen = true;
            OS.WindowBorderless = true;
            
        }
        else
        {
            OS.WindowFullscreen = false;
            OS.WindowBorderless = false;
            OS.WindowSize = WindowedResolution;
        }
        Fullscreen = fullscreen;
        SaveManager.DoSave();
    }

    private void _OnWindowSizeChanged()
    {
        if (!OS.WindowFullscreen)
            WindowedResolution = OS.WindowSize;
    }

    private void _OnImGuiLayout()
    {
        foreach (string key in _defaults.Keys)
        {
            ImGui.Text($"{key}: {_data[key]}");
        }

        if (ImGui.Button("Toggle Fullscreen"))
        {
            SetFullscreen(!Fullscreen);
        }
        ImGui.EndTabItem();
    }
}