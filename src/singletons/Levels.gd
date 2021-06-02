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
				{ "time": 11.0, "spawns": [ 0,0 ] },
				{ "time": 14.0, "spawns": [ 0,0 ] },
				{ "time": 17.0, "spawns": [ 0,0 ] },
				{ "time": 20.0, "spawns": [ 0,0 ] },
				{ "time": 25.0, "spawns": [ 0,0,0 ] },
			],
			[ # wave 2
				{ "time": 0.0, "spawns": [ 0 ] },
				{ "time": 1.0, "spawns": [ 0 ] },
				{ "time": 2.0, "spawns": [ 0 ] },
				{ "time": 3.0, "spawns": [ 0 ] },
				{ "time": 6.0, "spawns": [ 0 ] },
				{ "time": 7.0, "spawns": [ 0 ] },
				{ "time": 8.0, "spawns": [ 0 ] },
				{ "time": 9.0, "spawns": [ 0 ] },
				{ "time": 14.0, "spawns": [ 0,0,0 ] },
				{ "time": 19.0, "spawns": [ 0,0 ] },
				{ "time": 21.0, "spawns": [ 0,0 ] },
				{ "time": 25.0, "spawns": [ 0,0,0 ] },
			],
			[ # wave 3
				{ "time": 0.0, "spawns": [ 0,0 ] },
				{ "time": 3.0, "spawns": [ 0,0 ] },
				{ "time": 7.0, "spawns": [ 0,0,0,0 ] },
				{ "time": 10.0, "spawns": [ 0 ] },
				{ "time": 11.0, "spawns": [ 0 ] },
				{ "time": 12.0, "spawns": [ 0 ] },
				{ "time": 15.0, "spawns": [ 1 ] },
				{ "time": 18.0, "spawns": [ 0 ] },
				{ "time": 21.0, "spawns": [ 0 ] },
				{ "time": 23.0, "spawns": [ 1 ] },
				{ "time": 25.0, "spawns": [ 0,0 ] },
			],
			[ # wave 4
				{ "time": 0.0, "spawns": [ 0,0 ] },
				{ "time": 2.0, "spawns": [ 0,0 ] },
				{ "time": 4.0, "spawns": [ 0,0 ] },
				{ "time": 6.0, "spawns": [ 0,0 ] },
				{ "time": 8.0, "spawns": [ 0,0 ] },
				{ "time": 12.0, "spawns": [ 1,1,1 ] },
				{ "time": 18.0, "spawns": [ 0 ] },
				{ "time": 19.0, "spawns": [ 0 ] },
				{ "time": 20.0, "spawns": [ 0 ] },
				{ "time": 21.0, "spawns": [ 0 ] },
				{ "time": 22.0, "spawns": [ 0 ] },
				{ "time": 23.0, "spawns": [ 0 ] },
				{ "time": 24.0, "spawns": [ 0 ] },
				{ "time": 25.0, "spawns": [ 0 ] },
			],
			[ # wave 5
				{ "time": 0.0, "spawns": [ 0,0,1 ] },
				{ "time": 5.0, "spawns": [ 0,0,1 ] },
				{ "time": 10.0, "spawns": [ 0,0,1 ] },
				{ "time": 15.0, "spawns": [ 0,0,1 ] },
				{ "time": 20.0, "spawns": [ 0,0,1 ] },
				{ "time": 20.0, "spawns": [ 0,1,1 ] },
			],
			[ # wave 6
				{ "time": 0.0, "spawns": [ 0 ] },
				{ "time": 1.0, "spawns": [ 0 ] },
				{ "time": 2.0, "spawns": [ 0 ] },
				{ "time": 3.0, "spawns": [ 0 ] },
				{ "time": 4.0, "spawns": [ 0 ] },
				{ "time": 5.0, "spawns": [ 0 ] },
				{ "time": 6.0, "spawns": [ 0 ] },
				{ "time": 7.0, "spawns": [ 0 ] },
				{ "time": 8.0, "spawns": [ 0 ] },
				{ "time": 9.0, "spawns": [ 0 ] },
				{ "time": 10.0, "spawns": [ 0 ] },
				{ "time": 11.0, "spawns": [ 0 ] },
				{ "time": 12.0, "spawns": [ 0 ] },
				{ "time": 13.0, "spawns": [ 0 ] },
				{ "time": 14.0, "spawns": [ 0 ] },
				{ "time": 15.0, "spawns": [ 0 ] },
				{ "time": 16.0, "spawns": [ 0 ] },
				{ "time": 17.0, "spawns": [ 0 ] },
				{ "time": 18.0, "spawns": [ 0 ] },
				{ "time": 19.0, "spawns": [ 0 ] },
				{ "time": 20.0, "spawns": [ 0 ] },
				{ "time": 21.0, "spawns": [ 0 ] },
				{ "time": 22.0, "spawns": [ 0 ] },
				{ "time": 23.0, "spawns": [ 0 ] },
				{ "time": 24.0, "spawns": [ 0 ] },
				{ "time": 25.0, "spawns": [ 0 ] },
			],
			[ # wave 7
				{ "time": 0.0, "spawns": [ 0,1,1 ] },
				{ "time": 2.0, "spawns": [ 0,0 ] },
				{ "time": 8.0, "spawns": [ 0,1,1 ] },
				{ "time": 10.0, "spawns": [ 0,0 ] },
				{ "time": 15.0, "spawns": [ 0,1,1 ] },
				{ "time": 17.0, "spawns": [ 0,0 ] },
				{ "time": 23.0, "spawns": [ 0,1,1 ] },
				{ "time": 25.0, "spawns": [ 0,0 ] },
			],
			[ # wave 8
				{ "time": 5.0, "spawns": [ 2 ] },
				{ "time": 10.0, "spawns": [ 0 ] },
				{ "time": 15.0, "spawns": [ 0 ] },
				{ "time": 20.0, "spawns": [ 2 ] },
				{ "time": 25.0, "spawns": [ 0 ] },
			],
		]
	}
]

#var Levels = [
#	{
#		"title": "Act 1",
#		"waves": [
#			[ # wave 1
#				{ "time": 0.0, "spawns": [ 0 ] },
#				{ "time": 4.0, "spawns": [ 0 ] }
#			],
#			[ # wave 2
#				{ "time": 1.0, "spawns": [ 0,0,0 ] },
#				{ "time": 6.0, "spawns": [ 0,0,0 ] },
#			],
#			[ # wave 2
#				{ "time": 0.0, "spawns": [ 0,0,0 ] },
#				{ "time": 3.0, "spawns": [ 0,0,0 ] },
#			]
#		]
#	}
#]
