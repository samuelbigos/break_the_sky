using Godot;
using System;
using System.Diagnostics;
using Godot.Collections;

public partial class SaveManager : Singleton<SaveManager>
{
	private const string SAVE_FOLDER = "user://savegame";
	private bool _loadedThisSession = false;

	private static int Version => ProjectSettings.GetSetting("application/config/save_version").AsInt32();
	private static string SavePath => SAVE_FOLDER + "/" + ($"save_{Version:D3}.tres");

	public override void _Ready()
	{
		base._Ready();

		//Reset();
		DoLoad();
	}

	public static void DoSave()
	{
		int version = ProjectSettings.GetSetting("application/config/save_version").AsInt32();

		DirAccess directory = DirAccess.Open("user://");
		if (!directory.DirExists(SAVE_FOLDER))
		{
			directory.MakeDirRecursive(SAVE_FOLDER);
		}

		FileAccess saveGame = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);

		// first add the version
		saveGame.StoreLine(Json.Stringify(version));

		// then store each persistent node
		Dictionary saveData = new Dictionary();
		foreach (Node node in Instance.GetTree().GetNodesInGroup("persistent"))
		{
			Saveable saveableNode = node as Saveable;
			if (saveableNode == null)
				continue;

			saveData[saveableNode.Name] = saveableNode.DoSave();
		}
		saveGame.StoreLine(Json.Stringify(saveData));
		saveGame.Close();
	}

	private static void DoLoad()
	{
		int version = ProjectSettings.GetSetting("application/config/save_version").AsInt32();

		FileAccess saveGame = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
		if (!saveGame.IsOpen())
		{
			Instance.CreateSave();
			saveGame = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
		}

		Json json = new Json();
		
		// first read in the save game version
		json.Parse(saveGame.GetLine());
		int saveVersion = json.Data.AsInt32();
		
		if (saveVersion != version)
			return;

		json.Parse(saveGame.GetLine());
		Dictionary saveData = (Dictionary) json.Data;
		
		foreach (Node node in Instance.GetTree().GetNodesInGroup("persistent"))
		{
			Saveable saveableNode = node as Saveable;
			if (saveableNode == null)
				continue;

			if (!saveData.ContainsKey(saveableNode.Name))
			{
				saveableNode.InitialiseSaveData();
				saveableNode.DoSave();
				continue;
			}
			saveableNode.DoLoad(saveData[saveableNode.Name].AsGodotDictionary());
		}
		
		saveGame.Close();
		
		// save any changes that might have been made when loading
		DoSave();
	}

	public void Reset()
	{
		DirAccess dir = DirAccess.Open("user://");
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