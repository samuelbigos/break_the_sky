using Godot;
using System;
using System.Diagnostics;
using Godot.Collections;

public class SaveManager : Node
{
	public static SaveManager Instance;
	
	private const string SAVE_FOLDER = "user://savegame";
	
	private bool _loadedThisSession = false;

	public override void _Ready()
	{
		base._Ready();

		Instance = this;

		if (!_loadedThisSession)
		{
			DoLoad();
			_loadedThisSession = true;
		}
	}

	public void DoSave()
	{
		int version = Convert.ToInt32(ProjectSettings.GetSetting("application/config/save_version"));
		
		Directory directory = new Directory();
		if (!directory.DirExists(SAVE_FOLDER))
		{
			directory.MakeDirRecursive(SAVE_FOLDER);
		}
		
		File saveGame = new File();
		saveGame.Open(SavePath(version), File.ModeFlags.Write);
		
		// first add the version
		saveGame.StoreLine(JSON.Print(version));

		// then store each persistent node
		Dictionary saveData = new Dictionary();
		foreach (Node node in GetTree().GetNodesInGroup("persistent"))
		{
			Saveable saveableNode = node as Saveable;
			if (saveableNode == null)
				continue;

			saveData[saveableNode.Name] = saveableNode.DoSave();
		}
		saveGame.StoreLine(JSON.Print(saveData));
		saveGame.Close();
	}

	public void DoLoad()
	{
		int version = Convert.ToInt32(ProjectSettings.GetSetting("application/config/save_version"));

		File saveGame = new File();
		string path = SavePath(version);
		if (!saveGame.FileExists(path))
			return;
		
		saveGame.Open(path, File.ModeFlags.Read);
		
		// first read in the save game version
		int saveVersion = Convert.ToInt32(JSON.Parse(saveGame.GetLine()).Result);
		
		if (saveVersion != version)
			return;
		
		Dictionary saveData = (Dictionary) JSON.Parse(saveGame.GetLine()).Result;
		
		foreach (Node node in GetTree().GetNodesInGroup("persistent"))
		{
			Saveable saveableNode = node as Saveable;
			if (saveableNode == null)
				continue;
			
			saveableNode.DoLoad(saveData[saveableNode.Name] as Dictionary);
		}
		
		saveGame.Close();
	}

	private string SavePath(int version)
	{
		return SAVE_FOLDER.PlusFile($"save_{version:D3}.tres");
	}
}