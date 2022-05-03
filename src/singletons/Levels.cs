using System.Collections.Generic;
using Godot;

public class Levels : Node
{
	public static Levels Instance;
	
	public struct Wave
	{
		public List<SpawnSet> SpawnSets;
	}
	public struct SpawnSet
	{
		public float Time;
		public List<int> Spawns;
	}

	public List<Wave> Waves = new List<Wave>()
	{
		new Wave()
		{
			SpawnSets = new List<SpawnSet>()
			{
				// new SpawnSet() {Time = 0.0f, Spawns = new List<int> {0,0,0,0,0}},
				// new SpawnSet() {Time = 10.0f, Spawns = new List<int> {1,1}},
				// new SpawnSet() {Time = 20.0f, Spawns = new List<int> {2}},
				// new SpawnSet() {Time = 30.0f, Spawns = new List<int> {3}},
				// new SpawnSet() {Time = 40.0f, Spawns = new List<int> {4}},
				// new SpawnSet() {Time = 50.0f, Spawns = new List<int> {5}},
			}
		}
	};
	
	// public List<Wave> LevelData = new List<Wave>()
	// {
	// 	new Dictionary()
	// 	{
	// 		{"title", "Act 1"},
	// 		{"waves", new List<List<Dictionary>>(){
	// 			new List<Dictionary>()
	// 			{ // wave 1
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 2.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 4.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 6.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 8.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 11.0}, {"spawns", new Array(){ 0,0 } }},
	// 				new Dictionary(){ {"time", 14.0}, {"spawns", new Array(){ 0,0 } }},
	// 				new Dictionary(){ {"time", 17.0}, {"spawns", new Array(){ 0,0 } }},
	// 				new Dictionary(){ {"time", 20.0}, {"spawns", new Array(){ 0,0 } }},
	// 				new Dictionary(){ {"time", 25.0}, {"spawns", new Array(){ 0,0,0 } }},
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 2
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 1.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 2.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 3.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 6.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 7.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 8.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 9.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 14.0}, {"spawns", new Array(){ 1,0 } }},
	// 				new Dictionary(){ {"time", 19.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 21.0}, {"spawns", new Array(){ 1,0 } }},
	// 				new Dictionary(){ {"time", 25.0}, {"spawns", new Array(){ 0 } }},
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 3
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 0,0 } }},
	// 				new Dictionary(){ {"time", 3.0}, {"spawns", new Array(){ 0,0 } }},
	// 				new Dictionary(){ {"time", 7.0}, {"spawns", new Array(){ 0,0,0,0 } }},
	// 				new Dictionary(){ {"time", 10.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 11.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 12.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 15.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 18.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 21.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 23.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 25.0}, {"spawns", new Array(){ 0,0 } }},
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 4
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 0,0,1 } }},
	// 				new Dictionary(){ {"time", 5.0}, {"spawns", new Array(){ 0,0,1 } }},
	// 				new Dictionary(){ {"time", 10.0}, {"spawns", new Array(){ 0,0,1 } }},
	// 				new Dictionary(){ {"time", 15.0}, {"spawns", new Array(){ 0,0,1 } }},
	// 				new Dictionary(){ {"time", 20.0}, {"spawns", new Array(){ 0,0,1 } }},
	// 				new Dictionary(){ {"time", 20.0}, {"spawns", new Array(){ 0,1,1 } }},
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 5
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 1.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 2.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 3.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 4.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 5.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 6.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 7.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 8.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 9.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 10.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 11.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 12.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 13.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 14.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 15.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 16.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 17.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 18.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 19.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 20.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 21.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 22.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 23.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 24.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 25.0}, {"spawns", new Array(){ 1 } }},
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 6
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 0,1,1 } }},
	// 				new Dictionary(){ {"time", 2.0}, {"spawns", new Array(){ 0,0 } }},
	// 				new Dictionary(){ {"time", 8.0}, {"spawns", new Array(){ 0,1,1 } }},
	// 				new Dictionary(){ {"time", 10.0}, {"spawns", new Array(){ 0,0 } }},
	// 				new Dictionary(){ {"time", 15.0}, {"spawns", new Array(){ 0,1,1 } }},
	// 				new Dictionary(){ {"time", 17.0}, {"spawns", new Array(){ 0,0 } }},
	// 				new Dictionary(){ {"time", 23.0}, {"spawns", new Array(){ 0,1,1 } }},
	// 				new Dictionary(){ {"time", 25.0}, {"spawns", new Array(){ 0,0 } }},
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 7
	// 				new Dictionary(){ {"time", 5.0}, {"spawns", new Array(){ 2 } }},
	// 				new Dictionary(){ {"time", 10.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 15.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 20.0}, {"spawns", new Array(){ 2 } }},
	// 				new Dictionary(){ {"time", 25.0}, {"spawns", new Array(){ 0 } }},
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 8
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 0,0,0,0 } }},
	// 				new Dictionary(){ {"time", 5.0}, {"spawns", new Array(){ 1,1,0,0 } }},
	// 				new Dictionary(){ {"time", 10.0}, {"spawns", new Array(){ 1,1,1,1 } }},
	// 				new Dictionary(){ {"time", 22.0}, {"spawns", new Array(){ 2,0,0 } }},
	// 				new Dictionary(){ {"time", 25.0}, {"spawns", new Array(){ 0,0,0 } }},
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 9
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 1.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 2.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 3.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 4.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 5.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 6.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 7.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 8.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 9.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 10.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 11.0}, {"spawns", new Array(){ 2 } }},
	// 				new Dictionary(){ {"time", 12.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 13.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 14.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 15.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 16.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 17.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 18.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 19.0}, {"spawns", new Array(){ 2 } }},
	// 				new Dictionary(){ {"time", 20.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 21.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 22.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 23.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 24.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 25.0}, {"spawns", new Array(){ 2 } }},
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 10
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 3 } }},
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 11
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 0,0,0 } }},
	// 				new Dictionary(){ {"time", 5.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 8.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 10.0}, {"spawns", new Array(){ 0,0 } }},
	// 				new Dictionary(){ {"time", 12.0}, {"spawns", new Array(){ 0,0 } }},
	// 				new Dictionary(){ {"time", 15.0}, {"spawns", new Array(){ 3 } }},
	// 				new Dictionary(){ {"time", 20.0}, {"spawns", new Array(){ 1,1 } }},
	// 				
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 12
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 2 } }},
	// 				new Dictionary(){ {"time", 5.0}, {"spawns", new Array(){ 2 } }},
	// 				new Dictionary(){ {"time", 10.0}, {"spawns", new Array(){ 2 } }},
	// 				new Dictionary(){ {"time", 15.0}, {"spawns", new Array(){ 2 } }},
	// 				new Dictionary(){ {"time", 20.0}, {"spawns", new Array(){ 2 } }},
	// 				new Dictionary(){ {"time", 25.0}, {"spawns", new Array(){ 2 } }},
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 13
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 1.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 2.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 3.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 4.0}, {"spawns", new Array(){ 2 } }},
	// 				new Dictionary(){ {"time", 5.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 6.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 7.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 8.0}, {"spawns", new Array(){ 2 } }},
	// 				new Dictionary(){ {"time", 9.0}, {"spawns", new Array(){ 3 } }},
	// 				new Dictionary(){ {"time", 16.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 17.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 18.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 19.0}, {"spawns", new Array(){ 2 } }},
	// 				new Dictionary(){ {"time", 20.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 21.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 22.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 23.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 24.0}, {"spawns", new Array(){ 1 } }},
	// 				new Dictionary(){ {"time", 25.0}, {"spawns", new Array(){ 2 } }},
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 14
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 3 } }},
	// 				new Dictionary(){ {"time", 10.0}, {"spawns", new Array(){ 3 } }}
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 15
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 0 } }},
	// 				new Dictionary(){ {"time", 5.0}, {"spawns", new Array(){ 0,0 } }},
	// 				new Dictionary(){ {"time", 10.0}, {"spawns", new Array(){ 0,0,0 } }},
	// 				new Dictionary(){ {"time", 15.0}, {"spawns", new Array(){ 0,0,0,0 } }},
	// 				new Dictionary(){ {"time", 20.0}, {"spawns", new Array(){ 0,0,0,0,0 } }},
	// 				new Dictionary(){ {"time", 25.0}, {"spawns", new Array(){ 1,1,1,1,1,1 } }}
	// 			},
	// 			new List<Dictionary>()
	// 			{ // wave 16
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 2,2,2 } }},
	// 				new Dictionary(){ {"time", 10.0}, {"spawns", new Array(){ 3 } }},
	// 				new Dictionary(){ {"time", 25.0}, {"spawns", new Array(){ 2,2,2 } }}
	// 			},
	// 		}
	// 	}}
	// };
	
	// public Array DebugLevels = new Array()
	// {
	// 	new Dictionary()
	// 	{
	// 		{"title", "Act 1"},
	// 		{"waves", new Array()
	// 			{
	// 			new Array()
	// 			{ // wave 1
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 3 } }},
	// 			},
	// 			new Array()
	// 			{ // wave 2
	// 				new Dictionary(){ {"time", 1.0}, {"spawns", new Array(){ 0,0,0 } }},
	// 				new Dictionary(){ {"time", 6.0}, {"spawns", new Array(){ 0,0,0 } }},
	// 			},
	// 			new Array()
	// 			{ // wave 2
	// 				new Dictionary(){ {"time", 0.0}, {"spawns", new Array(){ 0,0,0 } }},
	// 				new Dictionary(){ {"time", 3.0}, {"spawns", new Array(){ 0,0,0 } }},
	// 			}
	// 		}
	// 	}}
	//};

	public override void _Ready()
	{
		Instance = this;
	}
}