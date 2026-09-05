# The Relay from Deep Space

A mod for the game [The Message from Deep Space](https://store.steampowered.com/app/4080030/The_Message_from_Deep_Space/) that allows players to communicate over the [Deep Space Communication Relay](https://dscr.dixonary.co.uk/) in-game.

## Installation
Before you install, it is highly, **highly**, recommended you complete the game first! The Relay will not show up until you have, to prevent your playthrough of TMfDS being spoiled.

### Thunderstore

The mod is available on [Thunderstore!](https://thunderstore.io/c/the-message-from-deep-space/p/stormytuna/The_Relay_from_Deep_Space/)

### Manual (Windows + Linux)

> [!IMPORTANT]
> Using Linux? That's fine! For manual installation, please follow the guide exactly. The Message from Deep Space has no Linux version, so all Linux computers run the Windows version through Proton. Follow the Windows installation instructions below, plus the extra step for Linux only.

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

<img width="355" height="118" alt="image" src="https://github.com/user-attachments/assets/3b9eb9be-83cd-4c96-ade5-31f554ac146f" />

The `RELAY MANAGER`, `RELAY INPUT`, and `RELAY` screens are how you interact with the Relay. Enter a callsign, in base 8, and then click `SET` to begin chatting.

<img width="2822" height="1043" alt="image" src="https://github.com/user-attachments/assets/990568d7-3965-4080-877c-3c3ff130a817" />

After you enter a callsign, the `RELAY` screen will populate with messages. You can input your own Meteor0ese message into the `RELAY INPUT` screen, then click `SEND` to send it. When you're finished chatting, you can click `LOGOUT` to leave the Relay.

There is another button on the `RELAY` screen, `RECOMPILE`. Whenever you define a new signal, you'll need to `RECOMPILE` the output to update older uses of it.

## Features

You need to set a callsign to access most of the `RELAY MANAGER` features. After entering a callsign, you can open the the `RELAY MANAGER` screen by clicking the `MANAGER` button, and you can return to the `RELAY INPUT` screen by clicking `INPUT`.

### Hotkeys

TRfDS attaches hotkeys to many of the buttons it adds. When hovering over a button, you can see if there's a hotkey attached to it on the left hand side, underneath the currently playing track.
- `RECOMPILE`: CTRL+T
- `LOGOUT`: CTRL+L
- `MANAGER`/`INPUT`: CTRL+R
- `SEND`: CTRL+E or CTRL+ENTER

### Sending signals that aren't defined

Sometimes, you will want to send a signal you haven't defined in your dictionary yet. You can do this by entering `|-<signal>`. For example, if you define `-2` as "STOP" in your dictionary, sending `|-2` will appear (for you) as "STOP". Similarly, if you haven't defined `-246` and you enter `|-246`, it will appear (for you) as `@-246_UNDEF`.

This is useful for when you are learning a new word and aren't sure what to call it yet, but you want to ask a clarifying question without creating a temporary definition.

### Adding new signals to the dictionary

<img width="1097" height="212" alt="image" src="https://github.com/user-attachments/assets/56663eec-5793-436a-b98a-7a55c5dfe663" />

Under the `ADD NEW SIGNALS` text, there are 2 text entry fields and a `MAKE` button. The `ID` text entry expects a negative integer, corresponding to the ID of the new signal you want to create. The `NAME` text entry expects a plaintext name, no more than 18 characters, that the new signal will be called. After entering these, press `MAKE` to update your dictionary with the new signal. You can then press `RECOMPILE` to update the Relay messages with your new definition.

This signal persists with your dictionary like every signal from the game does. You can edit it, add formatting, add notes, etc, just as you can with any other signal. If you want to permanently remove a custom signal, hold control and click the delete button in the dictionary menu.

### Message actions

You can use this section to copy the text of a message, copy the signals of a message, and view visuals in a message. Before clicking any of the buttons, you need to select a message by entering the ID of it. The ID is the gray number to the right of the callsign in the `RELAY` window.

To copy a message's text, simply click `COPY`. To copy a message's signals, hold CTRL and click `COPY`.

<img width="535" height="164" alt="image" src="https://github.com/user-attachments/assets/74c9872f-4512-4c88-a368-e0e4663ee512" />

To view a message's visual data, click `VIEW`.

There is another type of data that messages can contain. Huge thanks to electraminer (callsign 3544) for allowing me to reference their implementation of this feature when I was writing mine. The protocol was created for the Relay specifically, and isn't something you'd receive from Meteor 0. If you wish to find out about it on the Relay (which I highly recommend you do), please skip this section.

<details>
  <summary>Spoilers lie here, beware!</summary>
  
  Music is able to be transmitted over the Relay. The grammar is not documented here as to preserve the creators' intentions. Once you define `-577`, a new `PLAY` button appears. If someone sends a message with a song, you can enter the ID and click `PLAY` to listen to it. The `PLAY` button becomes a `STOP` button while Relay music is playing, and you can see the track's total length and your current position. Additionally, TMfDS's music will pause while you're listening to Relay music, and begin again once it stops.

</details>

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

## Limitations

Currently, TRfDS will fail to parse any Relay message that has a negative number lower than -2147483648 (AKA the signed 32 bit integer minimum value). This is because the Relay allows signals to be sent as 64 bit integers, while TMfDS's compiler expects all signals to be 32 bit integers. This shouldn't matter in practice, as people should never be creating signals lower than this number, but it is worth knowing about. TRfDS can parse positive integers of any value, even larger than the Relay can send.
