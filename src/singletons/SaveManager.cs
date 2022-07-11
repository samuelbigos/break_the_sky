using Godot;
using System;
using System.Diagnostics;
using Godot.Collections;

public class SaveManager : Singleton<SaveManager>
{
	private const string SAVE_FOLDER = "user://savegame";
	private bool _loadedThisSession = false;

	private static int Version => Convert.ToInt32(ProjectSettings.GetSetting("application/config/save_version"));
	private static string SavePath => SAVE_FOLDER.PlusFile($"save_{Version:D3}.tres");

	public override void _Ready()
	{
		base._Ready();

		//Reset();
		DoLoad();
	}

	public static void DoSave()
	{
		int version = Convert.ToInt32(ProjectSettings.GetSetting("application/config/save_version"));
		
		Directory directory = new Directory();
		if (!directory.DirExists(SAVE_FOLDER))
		{
			directory.MakeDirRecursive(SAVE_FOLDER);
		}
		
		File saveGame = new File();
		saveGame.Open(SavePath, File.ModeFlags.Write);
		
		// first add the version
		saveGame.StoreLine(JSON.Print(version));

		// then store each persistent node
		Dictionary saveData = new Dictionary();
		foreach (Node node in Instance.GetTree().GetNodesInGroup("persistent"))
		{
			Saveable saveableNode = node as Saveable;
			if (saveableNode == null)
				continue;

			saveData[saveableNode.Name] = saveableNode.DoSave();
		}
		saveGame.StoreLine(JSON.Print(saveData));
		saveGame.Close();
	}

	private static void DoLoad()
	{
		int version = Convert.ToInt32(ProjectSettings.GetSetting("application/config/save_version"));

		File saveGame = new File();
		string path = SavePath;
		if (!saveGame.FileExists(path))
			Instance.CreateSave();
		
		saveGame.Open(path, File.ModeFlags.Read);
		
		// first read in the save game version
		int saveVersion = Convert.ToInt32(JSON.Parse(saveGame.GetLine()).Result);
		
		if (saveVersion != version)
			return;
		
		Dictionary saveData = (Dictionary) JSON.Parse(saveGame.GetLine()).Result;
		
		foreach (Node node in Instance.GetTree().GetNodesInGroup("persistent"))
		{
			Saveable saveableNode = node as Saveable;
			if (saveableNode == null)
				continue;

			if (!saveData.Contains(saveableNode.Name))
			{
				saveableNode.InitialiseSaveData();
				saveableNode.DoSave();
				continue;
			}
			saveableNode.DoLoad(saveData[saveableNode.Name] as Dictionary);
		}
		
		saveGame.Close();
		
		// save any changes that might have been made when loading
		DoSave();
	}

	public void Reset()
	{
		Directory dir = new Directory();
		dir.Remove(SaveManager.SavePath);
		DoLoad();
	}

	private void CreateSave()
	{
		foreach (Node node in GetTree().GetNodesInGroup("persistent"))
		{
			Saveable saveableNode = node as Saveable;
			saveableNode.Reset();
			if (saveableNode == null)
				continue;
			
			saveableNode.InitialiseSaveData();
		}
		DoSave();
	}
}