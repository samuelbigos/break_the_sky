using Godot;
using System;
using System.Collections.Generic;

public class Database : Node
{
    public static Database Cities;

    private List<DataEntry> _entries = new List<DataEntry>();
    
    public DataEntry FindEntry(string name)
    {
        foreach (DataEntry entry in _entries)
        {
            if (string.Equals(entry.Name, name, StringComparison.CurrentCultureIgnoreCase))
                return entry;
        }

        return null;
    }

    public override void _Ready()
    {
        base._Ready();

        if (Name == "DatabaseCities")
            Cities = this;

        foreach (DataEntry node in GetChildren())
        {
            _entries.Add(node);
        }
    }
}
