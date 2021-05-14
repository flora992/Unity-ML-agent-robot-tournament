# Unity ML-agent robot tournament

##
- Runs on Unity `2019.4.11.f1`
- ML-agent is called `rotten_apple`

##
Capture ball game
###
The game starts with 2 robots at their respective bases at the corners of the stage and 9 ball targets in the middle
###
Each robot’s goal is to get as many ball targets back to its base as possible in the 2-minute time limit
If a robot goes into the enemy’s base, it can steal ball targets from the enemy
After 2 minutes, the robot with more targets in its base wins
###
Ties are broken by the number of targets currently being carried, followed by distance from your home base. In other words, if the number of targets in both bases are the same, then the robot carrying more targets will win; if the number of targets being carried are also the same (and at least 1), the robot closer to its home base will win
###
In case of ties, which happens when the number of targets in both bases are the same, and neither robots are carrying any targets, during the tournament, the match will be replayed!
