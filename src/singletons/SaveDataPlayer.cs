using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot.Collections;
using ImGuiNET;
using Array = Godot.Collections.Array;

public class SaveDataPlayer : Saveable
{
    public static SaveDataPlayer Instance;

    public static Action<int> OnLevelUp;

    private Godot.Collections.Dictionary<string, object> _defaults = new()
    {
        { "level", 0 },
        { "totalExperience", 0 },
        { "skillPoints", 0 },
        { "materialCount", 10 },
        { "maxAllyCount", 500 },
        { "initialAllyCount", 1 },
        { "unlockedAllies", new Array() },
        { "seenEnemies", new Dictionary() },
    };

    public static int MaxAllyCount
    {
        get => Convert.ToInt32(Instance._data["maxAllyCount"]);
        set => Instance._data["maxAllyCount"] = value;
    }

    public static int InitialAllyCount
    {
        get => Convert.ToInt32(Instance._data["initialAllyCount"]);
        set => Instance._data["initialAllyCount"] = value;
    }
    
    public static int SkillPoints
    {
        get => Convert.ToInt32(Instance._data["skillPoints"]);
        set => Instance._data["skillPoints"] = value;
    }
    
    public static int Experience
    {
        get => Convert.ToInt32(Instance._data["totalExperience"]);
        set
        {
            Instance._data["totalExperience"] = value;
            Instance.DetermineLevelup();
        }
    }

    public static int Level => Convert.ToInt32(Instance._data["level"]);
    public static Array UnlockedAllies => Instance._data["unlockedAllies"] as Array;

    public static int MaterialCount
    {
        get => Convert.ToInt32(Instance._data["materialCount"]);
        set => Instance._data["materialCount"] = value;
    }

    public static bool HasSeenEnemy(string id)
    {
        Dictionary seenEnemies = Instance._data["seenEnemies"] as Dictionary;
        if (!seenEnemies.Contains(id))
            return false;
        return (bool) seenEnemies[id];
    }

    public static void SetSeenEnemy(string id)
    {
        Dictionary seenEnemies = Instance._data["seenEnemies"] as Dictionary;
        seenEnemies[id] = true;
    }
    
    public static bool ConsumeSkillPoint()
    {
        if (SkillPoints > 0)
        {
            Instance._data["skillPoints"] = SkillPoints - 1;
            return true;
        }

        return false;
    }

    private void DetermineLevelup()
    {
        bool tryLevelup = true;
        while (tryLevelup)
        {
            if (TotalExpRequiredForLevel(Level + 1) <= Experience)
            {
                Instance._data["level"] = Convert.ToInt32(Instance._data["level"]) + 1;
                SkillPoints += 1;
                OnLevelUp?.Invoke(Level);
            }
            else
            {
                tryLevelup = false;
            }
        }
    }

    public static float TotalExpRequiredForLevel(int level)
    {
        GameSettingsResource settings = Resources.Instance.GameSettings;
        float b = settings.ExperiencePerLevelBase;
        float e = settings.ExperiencePerLevelExponent;
        float sum = 0.0f;
        for (int j = 0; j < level; j++)
        {
            sum += b * Mathf.Pow(e, j);
        }
        return sum;
    }

    public SaveDataPlayer()
    {
        Debug.Assert(Instance == null, "Attempting to create multiple SaveDataPlayer instances!");
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

        List<DataEnemyBoid> enemyBoids = Database.EnemyBoids.GetAllEntries<DataEnemyBoid>();
        SetSeenEnemy(enemyBoids[0].Name); // so we always have something to spawn

        List<DataAllyBoid> allyBoids = Database.AllyBoids.GetAllEntries<DataAllyBoid>();
        Debug.Assert(allyBoids.Count > 0, "Error creating save file, no ally boid data!");
        UnlockedAllies.Add(allyBoids[0].Name);
    }
    
    public override void _EnterTree()
    {
        base._EnterTree();
        DebugImGui.Instance.RegisterWindow("savedata_player", "Player Data", _OnImGuiLayout);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DebugImGui.Instance.UnRegisterWindow("savedata_player", _OnImGuiLayout);
    }

    private void _OnImGuiLayout()
    {
        foreach (string key in _defaults.Keys)
        {
            ImGui.Text($"{key}: {_data[key]}");
        }

        if (ImGui.Button("Add Skill Point"))
        {
            SkillPoints++;
        }
        
        if (ImGui.Button("Add Materials x100"))
        {
            MaterialCount += 100;
        }
        
        if (ImGui.Button("Add Experience x10"))
        {
            Experience += 10;
        }
        
        if (ImGui.Button("Add Experience x100"))
        {
            Experience += 100;
        }
        
        if (ImGui.Button("Add Experience x1000"))
        {
            Experience += 1000;
        }
        
        ImGui.EndTabItem();
    }
}