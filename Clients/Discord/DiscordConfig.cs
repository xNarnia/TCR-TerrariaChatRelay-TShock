using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay.Clients.Discord
{
    public class DiscordConfig
    {
        public string Comment { get; set; } = "Get a BOT_TOKEN from https://discordapp.com/developers/applications/";
        public bool EnableDiscord { get; set; } = false;
        public List<Endpoint> EndPoints { get; set; } = new List<Endpoint>();
    }

    public class Endpoint
    {
        public string BotToken { get; set; } = "BOT_TOKEN";
        public ulong[] Channel_IDs { get; set; } = { 0 };
    }
}
