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

namespace TerrariaChatRelay
{
	public class TerrariaChatRelay : Mod
    {

        public TerrariaChatRelay()
		{
		}

        public override void Load()
        {
            base.Load();

            // Intercept DeserializeAsServer method
            NetTextModule.DeserializeAsServer += NetTextModule_DeserializeAsServer;

            // Add subscribers to list
            EventManager.Subscribers = new List<Clients.Interfaces.IChatClient>();

            var discord = new MessyTestDiscordChatClient(EventManager.Subscribers);

            EventManager.Subscribers.Add(new TestChatClient(EventManager.Subscribers));
            EventManager.Subscribers.Add(discord);

            // Test Connect method specifically
            discord.ConnectAsync();
        }

        // Override text receive method from server
        private bool NetTextModule_DeserializeAsServer(NetTextModule.orig_DeserializeAsServer orig, Terraria.GameContent.NetModules.NetTextModule self, BinaryReader reader, int senderPlayerId)
        {
            ChatMessage message = ChatMessage.Deserialize(reader);

            EventManager.RaiseTerrariaMessageReceived(this, senderPlayerId, Color.White, message.Text);

            // Mimic original chat format since we're overriding the message
            NetMessage.BroadcastChatMessage(
                NetworkText.FromLiteral(
                    "<" + Main.player[senderPlayerId].name + "> " + message.Text), Color.White, -1);

            return false;
        }
    }
}
