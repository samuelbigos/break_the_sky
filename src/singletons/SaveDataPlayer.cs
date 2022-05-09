using Godot;
using System;
using Godot.Collections;
using Array = Godot.Collections.Array;

public class SaveDataPlayer : Saveable
{
    public static SaveDataPlayer Instance;

    public int MaxAllyCount
    {
        get => Convert.ToInt32(_data["maxAllyCount"]);
        set => _data["maxAllyCount"] = value;
    }
    
    public int InitialAllyCount
    {
        get => Convert.ToInt32(_data["initialAllyCount"]);
        set => _data["initialAllyCount"] = value;
    }

    public int Level => Convert.ToInt32(_data["level"]);

    public Array ActiveDrones => _data["activeDrones"] as Array;

    public bool HasSeenEnemy(string id)
    {
        Dictionary seenEnemies = _data["seenEnemies"] as Dictionary;
        return (bool) seenEnemies[id];
    }

    public void SetSeenEnemy(string id)
    {
        Dictionary seenEnemies = _data["seenEnemies"] as Dictionary;
        seenEnemies[id] = true;
    }

    public void LevelUp()
    {
        _data["level"] = Convert.ToInt32(_data["level"]) + 1;
    }

    public override void _Ready()
    {
        base._Ready();

        Instance = this;
    }

    public override void InitialiseSaveData()
    {
        _data["level"] = 1;
        Array activeDrones = new Array();
        _data["activeDrones"] = activeDrones;
        _data["maxAllyCount"] = 50;
        _data["initialAllyCount"] = 10;
        Dictionary seenEnemies = new Dictionary();
        foreach (DataEnemyBoid enemy in Database.EnemyBoids.GetAllEntries<DataEnemyBoid>())
        {
            seenEnemies[enemy.Name] = false;
        }
        _data["seenEnemies"] = seenEnemies;
        SetSeenEnemy("driller"); // so we always have something to spawn
    }
}
