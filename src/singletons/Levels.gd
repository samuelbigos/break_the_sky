extends Node

var Levels = [
	{
		"title": "Act 1",
		"waves": [
			[ # wave 1
				{ "time": 0.0, "spawns": [ 0 ] },
				{ "time": 2.0, "spawns": [ 0 ] },
				{ "time": 4.0, "spawns": [ 0 ] },
				{ "time": 6.0, "spawns": [ 0 ] },
				{ "time": 8.0, "spawns": [ 0 ] },
				{ "time": 12.0, "spawns": [ 0,0,0 ] },
			],
			[ # wave 2
				{ "time": 0.0, "spawns": [ 0,0,0 ] },
				{ "time": 5.0, "spawns": [ 0,0,0 ] },
				{ "time": 10.0, "spawns": [ 0,0,0 ] },
				{ "time": 15.0, "spawns": [ 0,0,0,0 ] },
				{ "time": 18.0, "spawns": [ 0,0,0,0 ] },
				{ "time": 30.0, "spawns": [ 0,0,1 ] },
			]
		]
	},
	{
		"title": "Act 2",
	},
	{
		"title": "Act 3",
	}
]
