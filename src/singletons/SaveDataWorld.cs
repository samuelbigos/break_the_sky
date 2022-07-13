using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Godot.Collections;
using ImGuiNET;

public class SaveDataWorld : Saveable
{
    private static SaveDataWorld _instance;

    private Godot.Collections.Dictionary<string, object> _defaults = new()
    {
    };
    
    public SaveDataWorld()
    {
        Debug.Assert(_instance == null, "Attempting to create multiple SaveDataWorld instances!");
        _instance = this;
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
        base.InitialiseSaveData();
    }
    
    public override void _EnterTree()
    {
        base._EnterTree();
        DebugImGui.Instance.RegisterWindow("savedata_world", "World Data", _OnImGuiLayout);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DebugImGui.Instance.UnRegisterWindow("savedata_world", _OnImGuiLayout);
    }

    private void _OnImGuiLayout()
    {
        foreach (string key in _defaults.Keys)
        {
            ImGui.Text($"{key}: {_data[key]}");
        }
    }
}
