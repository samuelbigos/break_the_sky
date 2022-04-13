using System.Collections.Generic;
using Godot;

public class PerkManager : Node
{
	[Export] public int PerkThreshold = 350;
	[Export] public float PerkThresholdMulti = 1.33f;
	
	private int _perkLevel = 1;
	private List<Perk> _perks = new List<Perk>();

	public override void _Ready()
	{  
		foreach(Node child in GetChildren())
		{
			Perk perk = child as Perk;;
			if (perk != null && perk.enabled)
			{
				_perks.Add(perk);
			}
		}
	}
	
	public bool ThresholdReached(int score)
	{  
		if(score >= GetNextThreshold())
		{
			_perkLevel += 1;
			return true;
		}
		return false;
		
	}

	public int GetNextThreshold()
	{  
		int threshold = 0;
		for (int i = 0; i < _perkLevel; i++)
		{
			threshold += (int)(PerkThreshold * Mathf.Pow(PerkThresholdMulti, i));
		}
		return threshold;
	}
	
	public void PickPerk(Perk perk)
	{  
		perk.maximum -= 1;
	}
	
	public List<Perk> GetRandomPerks(int count)
	{
		List<Perk> perks = new List<Perk>();
		foreach(var perk in _perks)
		{
			if(perk.maximum > 0)
			{
				perks.Add(perk);
				
			}
		}
		List<Perk> ret = new List<Perk>();
		while(ret.Count < count)
		{
			long rand = GD.Randi() % perks.Count;
			ret.Add(perks[(int)rand]);
			perks.RemoveAt((int)rand);
		}
		return ret;
	}
}