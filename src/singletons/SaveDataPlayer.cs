using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot.Collections;
using ImGuiNET;
using Array = Godot.Collections.Array;

public partial class SaveDataPlayer : Saveable
{
    private static SaveDataPlayer _instance;

    public static Action<int> OnLevelUp;

    private Godot.Collections.Dictionary<string, Variant> _defaults = new()
    {
        { "level", 0 },
        { "initialPlayerBoid", "" },
        { "totalExperience", 50 },
        { "skillPoints", 0 },
        { "materialCount", 0 },
        { "maxAllyCount", 500 },
        { "unlockedAllies", new Array() },
        { "seenEnemies", new Dictionary() },
    };

    private System.Collections.Generic.Dictionary<string, List<ResourceSkillNode>> _activeSkills = new();

    public static int MaxAllyCount
    {
        get => _instance._data["maxAllyCount"].AsInt32();
        set => _instance._data["maxAllyCount"] = value;
    }
    
    public static int SkillPoints
    {
        get => _instance._data["skillPoints"].AsInt32();
        set => _instance._data["skillPoints"] = value;
    }
    
    public static int Experience
    {
        get => _instance._data["totalExperience"].AsInt32();
        set
        {
            _instance._data["totalExperience"] = value;
            _instance.DetermineLevelup();
        }
    }

    public static int Level => _instance._data["level"].AsInt32();
    public static string InitialPlayerBoid => _instance._data["initialPlayerBoid"].AsString();

    public static int MaterialCount
    {
        get => _instance._data["materialCount"].AsInt32();
        set => _instance._data["materialCount"] = value;
    }

    public static bool HasSeenEnemy(ResourceBoidEnemy enemyData)
    {
        Dictionary seenEnemies = _instance._data["seenEnemies"].AsGodotDictionary();
        Debug.Assert(seenEnemies != null, nameof(seenEnemies) + " != null");
        if (!seenEnemies.ContainsKey(enemyData.UniqueID))
            return false;
        return (bool) seenEnemies[enemyData.UniqueID];
    }

    public static void SetSeenEnemy(ResourceBoidEnemy enemyData)
    {
        Dictionary seenEnemies = _instance._data["seenEnemies"].AsGodotDictionary();
        Debug.Assert(seenEnemies != null, nameof(seenEnemies) + " != null");
        seenEnemies[enemyData.UniqueID] = true;
    }
    
    public static bool IsFabricantUnlocked(ResourceBoidAlly allyData)
    {
        Array unlockedFabricants = _instance._data["unlockedAllies"].AsGodotArray();
        Debug.Assert(unlockedFabricants != null, nameof(unlockedFabricants) + " != null");
        return unlockedFabricants.Contains(allyData.UniqueID);
    }
    
    public static void UnlockFabricant(ResourceBoidAlly allyData)
    {
        Array unlockedFabricants = _instance._data["unlockedAllies"].AsGodotArray();
        if (!IsFabricantUnlocked(allyData))
        {
            Debug.Assert(unlockedFabricants != null, nameof(unlockedFabricants) + " != null");
            unlockedFabricants.Add(allyData.UniqueID);
        }
    }
    
    public static bool ConsumeSkillPoint()
    {
        if (SkillPoints > 0)
        {
            _instance._data["skillPoints"] = SkillPoints - 1;
            return true;
        }

        return false;
    }

    public static List<ResourceSkillNode> GetActiveSkills(ResourceBoidAlly allyType)
    {
        return _instance._activeSkills[allyType.UniqueID];
        
        // Dictionary dict = Instance._data["skills"] as Dictionary;
        // List<SkillNodeResource> skills = new();
        // foreach (Variant skill in dict[allyType] as Array)
        // {
        //     skills.Add(skill as SkillNodeResource);
        // }
        //
        // return skills;
    }
    
    public static void UpdateActiveSkills(ResourceBoidAlly allyType, List<ResourceSkillNode> nodes)
    {
        _instance._activeSkills[allyType.UniqueID] = nodes;

        // Dictionary dict = Instance._data["skills"] as Dictionary;
        // Array array = new();
        // foreach (SkillNodeResource skill in nodes)
        // {
        //     array.Add(skill);
        // }
        // dict[allyType] = array;
    }

    private void DetermineLevelup()
    {
        bool tryLevelup = true;
        while (tryLevelup)
        {
            if (TotalExpRequiredForLevel(Level + 1) <= Experience)
            {
                _instance._data["level"] = _instance._data["level"].AsInt32() + 1;
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
        ResourceGameSettings settings = Resources.Instance.ResourceGameSettings;
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
        Debug.Assert(_instance == null, "Attempting to create multiple SaveDataPlayer instances!");
        _instance = this;
    }

    protected override void Validate()
    {
        foreach (string key in _defaults.Keys)
        {
            if (!_data.ContainsKey(key))
            {
                _data[key] = _defaults[key];

                // if (key == "skills")
                // {
                //     foreach (DataAllyBoid boid in Database.AllyBoids.GetAllEntries<DataAllyBoid>())
                //     {
                //         Dictionary dict = _data[key] as Dictionary;
                //         if (dict.ContainsKey(boid.Name))
                //             continue;
                //         
                //         dict.Add(boid.Name, new Array());
                //     }
                // }
            }
        }

        Debug.Assert(FabricateManager.Instance.Fabricants.Count > 0);
        _data["initialPlayerBoid"] = FabricateManager.Instance.Fabricants[0].UniqueID;

        // TODO: _activeSkills isn't saved.
        foreach (ResourceBoidAlly allyData in FabricateManager.Instance.Fabricants)
        {
            if (!_activeSkills.ContainsKey(allyData.UniqueID))
                _activeSkills.Add(allyData.UniqueID, new List<ResourceSkillNode>());
        }
        if (!_activeSkills.ContainsKey("player"))
            _activeSkills.Add("player", new List<ResourceSkillNode>());
    }

    public override void InitialiseSaveData()
    {
        base.InitialiseSaveData();
    }
    
    public override void _EnterTree()
    {
        base._EnterTree();
        DebugImGui.Instance.RegisterWindow("savedata_player", "Player Data", _OnImGuiLayout);
        OnLevelUp += _OnLevelUp;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        DebugImGui.Instance.UnRegisterWindow("savedata_player", _OnImGuiLayout);
    }

    private void _OnLevelUp(int level)
    {
        switch (level)
        {
            case 1:
                break;
        }
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