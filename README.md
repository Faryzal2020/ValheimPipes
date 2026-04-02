# Valheim Pipes
## About
Adds hoppers and pipes to transport items. 
This is a fork of MSchmoecker's [ItemHopper](https://valheim.thunderstore.io/package/MSchmoecker/ItemHopper/) with several improvements and features.

The main difference is the improved item distribution algorithm. Where the original mod could be a bit unpredictable with throughput splitting, this fork uses monitored output neighbors scanning from the pipe to handle the push instead of asynchronous transfers between all the push/pulls from each hopper and pipes to make it more consistent and reliable.


## Features
### Tiered Hoppers and Pipes
Different hopper types are available, all can be found in the hammer crafting tab:

| Icon                                                                                                                                                 | Name                 | Cost                   |
|------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------|------------------------|
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/Wood_V.png" />                         | Bronze hopper        | 6 wood, 4 bronze nails |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/Wood_H.png" />                         | Bronze side hopper   | 6 wood, 4 bronze nails |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/PipeBronze_Horizontal_4m.png" />       | Bronze pipe 4m       | 4 wood, 2 bronze nails |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/PipeBronze_Horizontal_2m.png" />       | Bronze pipe 2m       | 2 wood, 1 bronze nails |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/PipeBronze_Vertical_Up_4m.png" />      | Bronze pipe up 4m    | 4 wood, 2 bronze nails |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/PipeBronze_Vertical_Up_2m.png" />      | Bronze pipe up 2m    | 2 wood, 1 bronze nails |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/PipeBronze_Vertical_Down_4m.png" />    | Bronze pipe down 4m  | 4 wood, 2 bronze nails |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/PipeBronze_Vertical_Down_2m.png" />    | Bronze pipe down 2m  | 2 wood, 1 bronze nails |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/PipeBronze_Diagonal_26_Up_4m.png" />   | Bronze pipe up 26°   | 4 wood, 2 bronze nails |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/PipeBronze_Diagonal_26_Down_4m.png" /> | Bronze pipe down 26° | 4 wood, 2 bronze nails |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/PipeBronze_Diagonal_45_Up_4m.png" />   | Bronze pipe up 45°   | 4 wood, 2 bronze nails |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/PipeBronze_Diagonal_45_Down_4m.png" /> | Bronze pipe down 45° | 4 wood, 2 bronze nails |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/Iron_V.png" />                         | Iron hopper          | 6 wood, 2 iron nails   |
| <img width="46" alt="icon" src="https://raw.githubusercontent.com/Faryzal2020/ValheimHopper/master/Docs/Icons/Iron_H.png" />                         | Iron side hopper     | 6 wood, 2 iron nails   |

Bronze hoppers and pipes have a transfer speed of 60 items per minute (1/sec).
Iron hoppers and pipes have a transfer speed of 120 items per minute (2/sec).
Both speeds are fully configurable in the config.


The bronze hopper has one slot while the iron hopper has three.
Hoppers can pickup and move items, while pipes can only move items.


### Individual hopper settings
Every hopper can have it's own setting. They appear in a custom UI when the hopper is opened.
- Filter Items: this can be used for automate item routing.
  The last item will be remembered with a "ghost" item and only this item type will be moved to the hopper.
- Enable Item Dropping: if enabled and the hopper has no target inventory they will dropped like the smelter does for example.
- Enable Item Pickup: if disabled the hopper will not pickup items from the ground.


### Supported Machines and Prefabs
The hoppers and pipes can interact with most vanilla containers, but we've also added specific support for:
- **Vanilla Oven** (`piece_oven`): Push and Pull support.
- **Vanilla Windmill** (`windmill`): Pull support.
- **Clay Collector** (`BCP_ClayCollector` from FineWoodPieces): Pull support.
- **Beehive** (`RDP_beehive` from Producers): Push and Pull support.


### Seamless multiplayer
The mod aims to work without interruption or major behavior differences of hoppers and pipes in multiplayer.


## Manual Installation
This mod requires [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/), [Jötunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/) and [MultiUserChest](https://valheim.thunderstore.io/package/MSchmoecker/MultiUserChest/).\
Extract the content of `ValheimHopper` into the `BepInEx/plugins` folder or any subfolder.

The mod must be installed on all clients and the server, otherwise the connection will fail.

IMPORTANT: Do not install MSchmoecker's ItemHopper mod together with this mod. They are not compatible.

## Links
- [Thunderstore](https://valheim.thunderstore.io/package/Faryzal2020/ValheimPipes/)
- [Github](https://github.com/Faryzal2020/ValheimHopper)
- [Nexus](https://www.nexusmods.com/valheim/mods/1974)
- Discord: Margmas. Feel free to DM or ping me about feedback or questions, for example in the [Jötunn discord](https://discord.gg/DdUt6g7gyA)


## Credits
The original mod was created by [MSchmoecker](https://valheim.thunderstore.io/package/MSchmoecker/ItemHopper/).
Big thanks to Bento#5066 for the hopper models and icons!


## Development
See [contributing](https://github.com/Faryzal2020/ValheimHopper/blob/master/CONTRIBUTING.md).


## Changelog
See [changelog](https://github.com/Faryzal2020/ValheimHopper/blob/master/CHANGELOG.md).
