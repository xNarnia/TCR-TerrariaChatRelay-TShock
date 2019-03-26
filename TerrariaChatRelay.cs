using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using TerrariaChatRelay.Clients;
using On.Terraria.GameContent.NetModules;
using Terraria.Chat;
using System.Reflection;
using TerrariaChatRelay.Helpers;
using Terraria.UI.Chat;

namespace TerrariaChatRelay
{
	public class TerrariaChatRelay : Mod
    {
        public static SimpleConfig Config { get; set; }

        public TerrariaChatRelay()
		{
		}

        public override void Load()
        {
            base.Load();
            Config = new TCRConfig();
            Console.WriteLine(Config.FilePath);

            // Intercept DeserializeAsServer method
            NetTextModule.DeserializeAsServer += NetTextModule_DeserializeAsServer;

            // Add subscribers to list
            EventManager.Subscribers = new List<Clients.Interfaces.IChatClient>();

            // Clients auto subscribe to list.
            new TestChatClient(EventManager.Subscribers);
            new DiscordChatClient(EventManager.Subscribers);

            EventManager.ConnectClients();
        }
        
        // Override text receive method from server
        private bool NetTextModule_DeserializeAsServer(NetTextModule.orig_DeserializeAsServer orig, Terraria.GameContent.NetModules.NetTextModule self, BinaryReader reader, int senderPlayerId)
        {
            ChatMessage message = ChatMessage.Deserialize(reader);

            EventManager.RaiseTerrariaMessageReceived(this, senderPlayerId, Color.White, message.Text);

            ChatManager.Commands.ProcessReceivedMessage(message, senderPlayerId);

            return false;
        }
    }
}
