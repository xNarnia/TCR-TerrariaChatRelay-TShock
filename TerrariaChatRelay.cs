using Microsoft.Xna.Framework;
using On.Terraria.GameContent.NetModules;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Net;
using Terraria.UI.Chat;
using TerrariaChatRelay.Clients;

namespace TerrariaChatRelay
{
    public class TerrariaChatRelay : Mod
    {
        public static TCRConfig Config { get; set; }

        public TerrariaChatRelay()
		{
		}

        public override void Load()
        {
            base.Load();

            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            Config = (TCRConfig) new TCRConfig().GetOrCreateConfiguration();
            // Console.WriteLine(Config.FilePath);

            // Intercept DeserializeAsServer method
            NetTextModule.DeserializeAsServer += NetTextModule_DeserializeAsServer;
            On.Terraria.NetMessage.BroadcastChatMessage += NetMessage_BroadcastChatMessage;
        
            // Add subscribers to list
            EventManager.Subscribers = new List<Clients.Interfaces.IChatClient>();

            // Clients auto subscribe to list.
            // new TestChatClient(EventManager.Subscribers);
            if (Config.Discord.EnableDiscord)
            {
                foreach (var discordClient in Config.Discord.EndPoints)
                {
                    new DiscordChatClient(EventManager.Subscribers, discordClient.BotToken, discordClient.Channel_IDs);
                }
            }

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

            if (Config.ShowGameEvents)
                EventManager.RaiseTerrariaMessageReceived(this, -1, Color.White, text.ToString());

            NetManager.Instance.Broadcast(packet, excludedPlayer);
        }

        /// <summary>
        /// Intercept chat messages sent from players.
        /// </summary>
        private bool NetTextModule_DeserializeAsServer(NetTextModule.orig_DeserializeAsServer orig, Terraria.GameContent.NetModules.NetTextModule self, BinaryReader reader, int senderPlayerId)
        {
            ChatMessage message = ChatMessage.Deserialize(reader);

            if (Config.ShowChatMessages)
                EventManager.RaiseTerrariaMessageReceived(this, senderPlayerId, Color.White, message.Text);

            ChatManager.Commands.ProcessReceivedMessage(message, senderPlayerId);

            return false;
        }

        public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
        {
            if (messageType == 12)
            {
                NetPacket packet = Terraria.GameContent.NetModules.NetTextModule.SerializeServerMessage(NetworkText.FromLiteral("This chat is powered by TerrariaChatRelay"), Color.LawnGreen, byte.MaxValue);
                NetManager.Instance.SendToClient(packet, playerNumber);
            }

            return false;
        }
    }
}
