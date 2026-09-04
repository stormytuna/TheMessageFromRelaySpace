# The Relay from Deep Space

A mod for the game [The Message from Deep Space](https://store.steampowered.com/app/4080030/The_Message_from_Deep_Space/) that allows players to communicate over the [Deep Space Communication Relay](https://dscr.dixonary.co.uk/) in-game.

TRfDS is in beta currently. Expect things to break.

## Installation
Before you install, it is highly, **highly** recommended you complete the game first!

> [!IMPORTANT]
> Using Linux? That's fine! For manual installation, please follow the guide exactly. The Message from Deep Space has no Linux version, so all Linux computers run the Windows version through Proton. Follow the Windows installation instructions below, plus the extra step for Linux only.

### Windows + Linux

Install [BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) as per their instructions:
1. Download `BepInEx_win_x64_5.X.X.X.zip` from [BepInEx's Releases](https://github.com/BepInEx/BepInEx/releases)
2. Navigate to the Game's directory through steam: Right click TMfDS -> Manage -> Browse local files
3. Extract the downloaded file such that the `BepInEx` folder and its contents are alongside `The Message From Deep Space_Data`

<img width="1537" height="220" alt="image" src="https://github.com/user-attachments/assets/dc913f2c-71b6-40d8-b376-97549a87ee6a" />

> [!IMPORTANT]
> LINUX ONLY: Right click TMfDS -> Properties. Under the General tab, set the Launch Options to `WINEDLLOVERRIDES="winhttp=n,b" %command%`

Install the mod:
1. Run the game once to setup BepInEx. After you close the game, you should see BepInEx has created a folder, `%GAMEROOT%/BepInEx/plugins`
2. Download [TRFDS.dll](https://github.com/stormytuna/TheRelayFromDeepSpace/releases/latest/download/TRFDS.dll)
3. Place the file in the `%GAMEROOT%/BepInEx/plugins` folder
4. Run the game

## Basic Usage

After installing the mod, you will see a new `RELAY` button on the main tabs view. This is how you access the Relay.

<img width="382" height="140" alt="image" src="https://github.com/user-attachments/assets/212a1566-370a-483c-80cb-67039cd49f01" />

The `RELAY MANAGER`, `RELAY INPUT`, and `RELAY` screens are how you interact with the Relay. The `RELAY MANAGER` screen is daunting at first, but all of the features are explaiend in the [Features](README.md#Features) section below.

For now, enter a callsign, in base 8, and then click `SET` to begin chatting.

<img width="2822" height="1043" alt="image" src="https://github.com/user-attachments/assets/990568d7-3965-4080-877c-3c3ff130a817" />

After you enter a callsign, the `RELAY` screen will populate with messages. You can input your own Meteor0ese message into the `RELAY INPUT` screen, then click `SEND` to send it. When you're finished chatting, you can click `LOGOUT` to leave the Relay.

There is another button on the `RELAY` screen, `RECOMPILE`. Whenever you define a new signal, you'll need to `RECOMPILE` the output to update older uses of it.

## Features

You'll want to be on the `RELAY MANAGER` screen for most of this. It's recommended to set a callsign, then return to this screen by clicking the `MANAGER` button. You can return to the `RELAY INPUT` screen by clicking `INPUT`.

### Adding new signals to the dictionary

<img width="1097" height="212" alt="image" src="https://github.com/user-attachments/assets/56663eec-5793-436a-b98a-7a55c5dfe663" />

Under the `ADD NEW SIGNALS` text, there are 2 text entry fields and a `MAKE` button. The `ID` text entry expects a negative integer, corresponding to the ID of the new signal you want to create. The `NAME` text entry expects a plaintext name, no more than 18 characters, that the new signal will be called. After entering these, press `MAKE` to update your dictionary with the new signal. You can then press `RECOMPILE` to update the Relay messages with your new definition.

This signal persists with your dictionary like every signal from the game does. You can edit it, add formatting, add notes, etc, just as you can with any other signal. If you want to permanently remove a custom signal, hold control and click the delete button in the dictionary menu.

### Message actions

<img width="569" height="169" alt="image" src="https://github.com/user-attachments/assets/75ae8dab-382f-441d-9323-04e49c4e5991" />

When people send pretty visuals, you can use this to see them. Enter the ID of the message (the grey number to the right of the callsign in the `RELAY` window) into the text entry field and click `VIEW`.

<img width="1136" height="891" alt="image" src="https://github.com/user-attachments/assets/382485c8-87c4-4491-9713-88fa21911b66" />

Image created by [astro (callsign 1574)](https://github.com/A5TR0spud)

### Channels

<img width="853" height="189" alt="image" src="https://github.com/user-attachments/assets/b0a7242f-4cc8-4d66-86db-deb5c0e62430" />

Channels allow people on the Relay to communicate in isolates spaces, usually so they don't clog up the main channel. Adding and removing channels requires defining 2 custom signals, if you wish to find these out on the Relay, skip this section.

<details>
  <summary>How to join and leave channels</summary>
  
  You will need to define two signals, -65534 and -65533 for joining and leaving respectively. Name them however you wish. Once you have them defined, you can join a channel by entering `JOIN SIGNAL`, where `JOIN` is what you named -65534 and `SIGNAL` is the signal corresponding to the channel you'd like to join. You can leave channels in a similar fashion with `LEAVE SIGNAL`, where `LEAVE` is what you named -65533 and `SIGNAL` is the signal corresponding to the channel you'd like to leave.

</details>

After you join channels, you can cycle through ones you're active in, and the `RELAY` output will update with the messages in that channel.

Channels you're in are saved and loaded from disk. You can find this file by navigating in-game to `MENU` -> `HANDLE SAVE FILE` -> `SHOW WHERE MY FILES ARE STORED`, then opening that path in your file manager of choice. All files this mod creates are stored in the TRFDS folder in this path. It's recommended you don't change the `enabledChannels.save` file, but you may view it in any text editor. If you modify it and break something by accident, it's safe to delete, the mod will save a new one when you join a channel again.

## Configuration

Many aspects of this mod are configurable, you can find details on each config option in the config file that BepInEx generates. Note: It is only generated after launching with the mod enabled, so you will need to launch once to see the config file at all.
