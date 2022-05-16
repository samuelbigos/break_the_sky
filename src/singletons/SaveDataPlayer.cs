using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        
        List<DataEnemyBoid> enemyBoids = Database.EnemyBoids.GetAllEntries<DataEnemyBoid>();
        Debug.Assert(enemyBoids.Count > 0, "Error creating save file, no enemy boid data!");
        foreach (DataEnemyBoid enemy in enemyBoids)
        {
            seenEnemies[enemy.Name] = false;
        }
        _data["seenEnemies"] = seenEnemies;
        SetSeenEnemy(enemyBoids[0].Name); // so we always have something to spawn
        
        List<DataAllyBoid> allyBoids = Database.AllyBoids.GetAllEntries<DataAllyBoid>();
        Debug.Assert(allyBoids.Count > 0, "Error creating save file, no ally boid data!");
        ActiveDrones.Add(allyBoids[0].Name);
    }
}
