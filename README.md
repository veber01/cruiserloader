![mainout logo](https://raw.githubusercontent.com/veber01/cruiserloader/8d42d89f6d88d8cd8082fa335bebb586c7556552/mainout.png?raw=true)




# CruiserLoader

A Cruiser auto loader mod currently made for pub Dine runs to load cruiser up with utilities.

Currently only "supports" Dine playstyle in which you load the back of the cruiser with stuff. (planning to add more loadouts and just generally more functionality into it)

Number of items loaded and items to load are fully customizable in the config file.

Only items that are currently inside of the ship are getting loaded. Use `.cload` chat command to load up cruiser. Use `.cload <itemname> <count>` to load only one type and amount of that item. Also without having a specific item configured in the configs, can use `.cload <itemname> <count> <zone>` to move items to specified zones. (example `.cload jet 2 D3` will move 2 jetpacks to D3 with/without having it configured in the configs) 

Any number of items can be loaded up to the maximum that are set in the configs for each item. For each zone, items are loaded in in a 5 offset configuration before wrapping back and stacking them.

Clients that has the mod installed can use the commands if the host itself also has it.

## Configuration

!!!Not all of the items have default zoning. Items that have default zones are shown in the picture below with their count aswell!!!

Config file lives in /config/CruiserLoader/Items.cfg

Item amount to load and their zones are configurable in the configs.

Zone names are specified on the image below.

D2 are specified as the radar booster spot (wouldnt specify that zone for anything else)

![Zones](https://raw.githubusercontent.com/veber01/cruiserloader/refs/heads/main/ZoneLayout.png?raw=true)

## Example

![Example](https://raw.githubusercontent.com/veber01/cruiserloader/refs/heads/main/CruiserExample.png?raw=true)

## Credits

This project uses [LethalCompanyTemplate](https://github.com/LethalCompany/LethalCompanyTemplate/blob/main/LICENSE) and slices of code of [ItemDeclutter](https://github.com/AndreyMrovol/LethalItemsDeclutter/blob/main/LICENSE) licensed under MIT License.