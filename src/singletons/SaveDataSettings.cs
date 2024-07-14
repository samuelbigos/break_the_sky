using Godot;
using System;
using System.Diagnostics;
using Godot.Collections;
using ImGuiNET;

public partial class SaveDataSettings : Saveable
{
    public static SaveDataSettings Instance;

    private Dictionary<string, Variant> _defaults = new()
    {
        { "fullscreen", false },
        { "windowed_resolution", new Vector2(1440, 810) },
    };

    private static bool Fullscreen
    {
        get => Instance._data["fullscreen"].AsBool();
        set => Instance._data["fullscreen"] = value;
    }
    
    private static Vector2I WindowedResolution
    {
        get => (Vector2I) Utils.Vector2FromString(Instance._data["windowed_resolution"].AsString());
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
            if (!_data.ContainsKey(key))
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
        GetViewport().Connect("size_changed", new Callable(this, nameof(_OnWindowSizeChanged)));
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
            WindowedResolution = DisplayServer.WindowGetSize();
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            
        }
        else
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            DisplayServer.WindowSetSize(WindowedResolution);
        }
        Fullscreen = fullscreen;
        SaveManager.DoSave();
    }

    private void _OnWindowSizeChanged()
    {
        if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed)
            WindowedResolution = DisplayServer.WindowGetSize();
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