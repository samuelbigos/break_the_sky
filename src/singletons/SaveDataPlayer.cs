using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot.Collections;
using Array = Godot.Collections.Array;

public class SaveDataPlayer : Saveable
{
    public static SaveDataPlayer Instance;

    private Godot.Collections.Dictionary<string, object> _defaults = new Godot.Collections.Dictionary<string, object>()
    {
        {"level", 1},
        {"materialCount", 100},
        {"maxAllyCount", 50},
        {"initialAllyCount", 1},
        {"activeDrones", new Array()},
        {"seenEnemies", new Dictionary()},
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

    public static int Level => Convert.ToInt32(Instance._data["level"]);
    public static Array ActiveDrones => Instance._data["activeDrones"] as Array;

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
        ActiveDrones.Add(allyBoids[0].Name);
    }
}
