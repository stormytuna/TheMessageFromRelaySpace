# The Relay from Deep Space

A mod for the game [The Message from Deep Space](https://store.steampowered.com/app/4080030/The_Message_from_Deep_Space/) that allows players to communicate over the [Deep Space Communication Relay](https://dscr.dixonary.co.uk/) in-game.

TRFDS is in beta currently. Expect things to break.

## Installation
Before you install, it is highly, **highly** recommended you complete the game first!

Install [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) as per their instructions:
- Download BepInEx_win_x64_5.X.X.X.zip from [BepInEx's Releases](https://github.com/BepInEx/BepInEx/releases)
- Navigate to the Game's directory through steam: Right click TMfDS -> Manage -> Browse local files
- Extract the downloaded file such that the `BepInEx` folder is alongside `The Message From Deep Space_Data`
- LINUX ONLY: Set the game's launch command to `WINEDLLOVERRIDES="winhttp=n,b" %command%`
- Run the game once to setup BepInEx. After you close the game, you should see BepInEx has created a folder, `%GAMEROOT%/BepInEx/Plugins`

Install the mod:
- Download `TMFRS.dll` from the [releases](https://github.com/stormytuna/TheRelayFromDeepSpace/releases/latest)
- Place the file in the `%GAMEROOT%/BepInEx/Plugins` folder
- Run the game
