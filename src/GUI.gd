extends CanvasLayer


func setScore(var score: int, var multi: int):
	$Score.text = "%06d" % score
	$ScoreMulti.text = "x" + "%d" % multi
