using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaChatRelay.Clients.Discord;
using TerrariaChatRelay.Helpers;

namespace TerrariaChatRelay
{
    public class TCRConfig : SimpleConfig<TCRConfig>
    {
        public override string FileName { get; set; }
            = Path.Combine(Main.SavePath, "Mod Configs", "TerrariaChatRelay.json");

        // TerrariaChatRelay
        public bool ShowChatMessages { get; set; } = true;
        public bool ShowGameEvents { get; set; } = true;

        // Discord
        public DiscordConfig Discord { get; set; }

        public TCRConfig()
        {
            if (!File.Exists(FileName))
            {
                // Discord
                Discord = new DiscordConfig();
                Discord.EndPoints.Add(new Endpoint());

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("TerrariaChatRelay - New Mod Config generated, update values. Mod reload required.");
                Console.WriteLine("  Config path: " + FileName);
                Console.ResetColor();
            }
        }
    }
}