using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public abstract class Database : Node
{
    public static Database Cities;
    public static Database AllyBoids;
    public static Database EnemyBoids;
    
    private readonly List<DataEntry> _entries = new List<DataEntry>();
    
    public T FindEntry<T>(string name)
        where T: class
    {
        foreach (DataEntry entry in _entries)
        {
            if (string.Equals(entry.Name, name, StringComparison.CurrentCultureIgnoreCase))
                return (T) Convert.ChangeType(entry, typeof(T));
        }

        return null;
    }

    public List<T> GetAllEntries<T>() where T: class => _entries.Cast<T>().ToList();

    public override void _Ready()
    {
        base._Ready();

        switch (Name)
        {
            case "DatabaseCities":
                Cities = this;
                break;
            case "DatabaseAllyBoids":
                AllyBoids = this;
                break;
            case "DatabaseEnemyBoids":
                EnemyBoids = this;
                break;
        }

        foreach (DataEntry node in GetChildren())
        {
            _entries.Add(node);
        }
    }
    
    // quick-helpers
    public static DataAllyBoid AllyBoid(string id)
    {
        return AllyBoids.FindEntry<DataAllyBoid>(id);
    }
    
    public static DataEnemyBoid EnemyBoid(string id)
    {
        return EnemyBoids.FindEntry<DataEnemyBoid>(id);
    }
}
