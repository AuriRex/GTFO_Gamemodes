# Hide and Seek

A custom gamemode for GTFO!

<p align="center">
  <img src="https://raw.githubusercontent.com/AuriRex/GTFO_Gamemodes/refs/heads/master/HNS/Resources/HNS_Banner.png" alt="Hide and Seek Banner Image"/>
</p>

Hide from the Hiders and Seek the Seekers ... no wait, that's not how this works ...  
Ah whatever, you know the deal!

Both *regular* and *team* games are available, team games are enabled automatically as soon as one player joins a team using `/hnsteam`

***There is currently no automatic hinting system, house rules apply I suppose;***  
Some recommendations:
* Seekers call left/right or top/bottom and hiders have to give which side they are on at the 5-minute mark.
* Hiders post their current zone number into chat at the 10-minute mark.
* Hiders post their current zone AND area into chat at the 15-minute mark.
* Hiders give a more detailed response about where in the current room they are at, at the 20-minute mark.

> By playing with this mod you are (non)-LEGALLY REQUIRED to send me your funniest clips! (If you want to ofc) >:3

### Hide and Seek mode chat-commands list:
```cs
[Pre-Game Commands]
/seeker // asign yourself to team seeker
/hider // assign yourself to team hider
/lobby // assign yourself to the pre-game team
// Note: Anyone that is still on neither team (lobby) upon round start will be assigned to team hiders automatically.
/hnsteam [0-4] // Switch teams for team games, 0 = none
/disinfect // Clears your infection during downtime
/hnshelp // Prints the welcome text that gets sent after touching ground again

[Game Commands]
/tool // Switch your tool (has cooldown during rounds)
/melee // Switch your melee weapon
// Note: Melee weapons affect your ability to break doors as seeker (all buffed)
// Running with knife out is slightly faster than with anything else. (Seekers only!)
/unstuck <'confirm'> // If you get stuck, use this to teleport back onto the navmesh (you will die -> turn into a seeker)
/spectate // Allows you to spectate your teammates, quick exit via [Backspace] key
// Scroll wheel to zoom, LMB / RMB to switch targets, F to toggle *local* flashlight
/time // Displays recorded hider times, only resets if gamemode is switched / game restarted
/total // alias for /time

[Master/Host Only]
/hnsstart [setupTimeSeconds] // Optional setup time from 1 to 255 seconds
/hnsstop
/hnsabort // Same as stop but does not save times
/dimension <dimensionIndex> // Teleport all players into a dimension; 0 = Reality / Main Dimension

// Note: Do not include '[]', '<>' or '''' in the command arguments lol
// '[]' = Optional, '<>' = Required
```