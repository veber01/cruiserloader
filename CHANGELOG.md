# v0.1.2

- added a new config entry for client use without host having the mod. It does work just fine but i cannot guarantee that it wont ever break, so i just keep it disabled by default (to enable it, go into the config and set "Experimental ClientUse" to true)
- added an auto-restock feature, can be enabled or disabled using `.cload restock` command or in the configs.

# v0.1.0

- items are now also can be moved from CUPBOARDS to the cruiser (many who using sort mod that sorts into cupboard were unable to move their stuff)
- Host Client verifications, msg sync etc. (many unnecessary networking just for looks 0 functionalities :))
- changed feedback message from chat to displaytip.
- general cleanups (possibly)
- Planning to add "ART" loader soon too.

# v0.0.8

- added failsafe to networkhandler to always get initialized after rehosting lobbies or joining lobbies.

# v0.0.7

- added for specific items a default count (radar 1, shotgun 2, knife 3, weed 15, tzp 15)
- added zone override for single item load(`.cload <itemname> <count> <zone>`)
- added chat message to have a feedback for loaded items.
- added some custom networking for clients so they are also able to use the commands, that executes on host to ensure proper networking.

# v0.0.5

- changed prefix for load command from /cload to .cload, because ChatCommandAPI compeltely blocks my command from running instead of passing it trought. (so dumb)
- added support to only load individual items
- added some default zones for some items.

# v0.0.3

- Items are only getting loaded up to the specified amount when using the command multiple times.

# v0.0.2
- changes to readme!