using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Godot.Collections;

public class SaveDataWorld : Saveable
{
    public static SaveDataWorld Instance;

    public enum CityState
    {
        Cold,
        Warm,
        Hot,
        Destroyed
    }

    public CityState GetCityState(string city)
    {
        Dictionary cityStates = _data["cityStates"] as Dictionary;
        
        Debug.Assert(cityStates != null);
        Debug.Assert(cityStates.Contains(city));

        return (CityState) Convert.ToInt32(cityStates[city]);
    }
    
    public SaveDataWorld()
    {
        Debug.Assert(Instance == null, "Attempting to create multiple SaveDataWorld instances!");
        Instance = this;
    }

    public override void _Ready()
    {
        base._Ready();

        Utils.Rng.Seed = (ulong)DateTime.Now.Millisecond;
    }

    public override void InitialiseSaveData()
    {
        Dictionary cityStates = new Dictionary();
        _data["cityStates"] = cityStates;
        
        // populate city states
        List<DataCity> cities = Database.Cities.GetAllEntries<DataCity>();
        foreach (DataCity city in cities)
        {
            cityStates[city.Name] = CityState.Cold;
        }
        
        // pick two cities to be under attack
        int numCitiesUnderAttack = Metagame.Instance.InitialCitiesUnderAttack;
        Debug.Assert(cities.Count > numCitiesUnderAttack);
        List<string> citiesUnderAttack = new List<string>();
        while (citiesUnderAttack.Count < numCitiesUnderAttack)
        {
            int randCity = (int) (Utils.Rng.Randi() % cities.Count);
            if (!citiesUnderAttack.Contains(cities[randCity].Name))
            {
                citiesUnderAttack.Add(cities[randCity].Name);
            }
        }
        foreach (string city in citiesUnderAttack)
        {
            cityStates[city] = CityState.Warm;
        }
    }

    protected override void Validate()
    {
    }
}
