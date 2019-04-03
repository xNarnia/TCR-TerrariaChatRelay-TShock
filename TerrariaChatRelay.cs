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
using Terraria.Net;

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
            On.Terraria.NetMessage.BroadcastChatMessage += NetMessage_BroadcastChatMessage;

            // Add subscribers to list
            EventManager.Subscribers = new List<Clients.Interfaces.IChatClient>();

            // Clients auto subscribe to list.
            // new TestChatClient(EventManager.Subscribers);
            new DiscordChatClient(EventManager.Subscribers);

            EventManager.ConnectClients();
        }

        /// <summary>
        /// Handle disconnect for all clients, remove events, and finally dispose of config.
        /// </summary>
        public override void Unload()
        {
            EventManager.DisconnectClients();
            NetTextModule.DeserializeAsServer -= NetTextModule_DeserializeAsServer;
            On.Terraria.NetMessage.BroadcastChatMessage -= NetMessage_BroadcastChatMessage;
            Config = null;
        }

        /// <summary>
        /// Intercept all other messages from Terraria. E.g. blood moon, death notifications, and player join/leaves.
        /// </summary>
        private void NetMessage_BroadcastChatMessage(On.Terraria.NetMessage.orig_BroadcastChatMessage orig, NetworkText text, Color color, int excludedPlayer)
        {
            NetPacket packet = Terraria.GameContent.NetModules.NetTextModule.SerializeServerMessage(text, color, byte.MaxValue);
            EventManager.RaiseTerrariaMessageReceived(this, -1, Color.White, text.ToString());
            NetManager.Instance.Broadcast(packet, excludedPlayer);
        }

        /// <summary>
        /// Intercept chat messages sent from players.
        /// </summary>
        private bool NetTextModule_DeserializeAsServer(NetTextModule.orig_DeserializeAsServer orig, Terraria.GameContent.NetModules.NetTextModule self, BinaryReader reader, int senderPlayerId)
        {
            ChatMessage message = ChatMessage.Deserialize(reader);
            EventManager.RaiseTerrariaMessageReceived(this, senderPlayerId, Color.White, message.Text);
            ChatManager.Commands.ProcessReceivedMessage(message, senderPlayerId);

            return false;
        }
    }
}
