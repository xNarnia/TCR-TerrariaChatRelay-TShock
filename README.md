# TCR-TerrariaChatRelay
Self-serving Terraria Chat Relay with extensible chat service support  

# To-do:

* Implement Discord to Terraria relay
* Connect MessageSent events
* Have EventManager handle Inits and Connects
* Move Connects to World OnLoad, keep Inits in Mod Load
* Implement NewtonsoftJson to BaseClient or IChatClient for storing config files
* Have Clients register their own commands for setting themselves up (examples: tokens, channel id's, etc.)
* Organize DiscordChatClient to not be so messy (Use State instead of bools, Event socketclient?)

Other chat services considered: IRC, Slack

If you want to suggest a chat service, prioritize another, or contribute some code, please feel free to say so!