using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay
{
    public class TCRConfig : SimpleConfig
    {
        public TCRConfig() : base("TerrariaChatRelay") { }

        public override Dictionary<string, string> DefaultData()
        {
            return new Dictionary<string, string>()
            {
                { "DiscordBotToken", "BOT_TOKEN" },
                { "DiscordChannelId", "CHANNEL_ID" }
            };
        }
    }
}
