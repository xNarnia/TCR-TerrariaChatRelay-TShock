using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients.Interfaces;
using TerrariaChatRelay.Helpers;
using Newtonsoft.Json;
using TerrariaChatRelay.Clients.Discord;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using System.Net;
using WebSocketSharp;
using Terraria.Net;
using System.Text.RegularExpressions;
using Terraria.ModLoader.IO;
using Terraria.UI.Chat;
using Terraria.ModLoader;

namespace TerrariaChatRelay.Clients
{
    public class DiscordChatClient : BaseClient
    {
        public const string GATEWAY_URL = "wss://gateway.discord.gg/?v=6&encoding=json";
        public const string API_URL = "https://discordapp.com/api/v6";

        // Discord Variables
        public List<ulong> Channel_IDs { get; set; }
        private string BOT_TOKEN;
        private int? LastSequenceNumber = 0;
        private DiscordChatParser chatParser { get; set; }
        private System.Timers.Timer heartbeatTimer { get; set; }

        // Message Queue
        private System.Timers.Timer messageQueueTimer { get; set; }
        private DiscordMessageQueue messageQueue { get; set; }
        private bool queueing { get; set; }

        // TCR Variables
        private List<IChatClient> _parent { get; set; }
        private WebSocket Socket;
        private int errorCounter;

        // Other
        private bool debug = false;

        public DiscordChatClient(List<IChatClient> _parent, string bot_token, ulong[] channel_ids) 
            : base(_parent)
        {
            BOT_TOKEN = bot_token;
            chatParser = new DiscordChatParser();
            Channel_IDs = channel_ids.ToList();

            messageQueue = new DiscordMessageQueue(500);
            messageQueue.OnReadyToSend += delegate (Dictionary<ulong, Queue<string>> messages) {
                foreach(var queue in messages)
                {
                    string output = "";

                    foreach(var msg in queue.Value)
                    {
                        output += msg + '\n';
                    }

                    if(output.Length > 2000)
                        output = output.Substring(0, 2000);

                    SendMessageToDiscordChannel(queue.Key, output);
                }
            };
        }

        /// <summary>
        /// Create a new WebSocket and initiate connection with Discord servers. 
        /// Utilizes BOT_TOKEN and CHANNEL_ID found in Mod Config.
        /// </summary>
        public override void Connect()
        {
            if (BOT_TOKEN == "BOT_TOKEN" || Channel_IDs.Contains(0))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("TerrariaChatRelay [Discord] - Please update your Mod Config. Mod reload required.");

                if (BOT_TOKEN == "BOT_TOKEN")
                    Console.WriteLine(" Invalid Token: BOT_TOKEN");
                if (Channel_IDs.Contains(0))
                    Console.WriteLine(" Invalid Channel Id: 0");

                Console.WriteLine("  Config path: " + TerrariaChatRelay.Config.FileName);
                Console.ResetColor();
                this.Dispose();
                return;
            }

            errorCounter = 0;

            Socket = new WebSocket(GATEWAY_URL);
            Socket.Compression = CompressionMethod.Deflate;
            Socket.OnOpen += (object sender, EventArgs e) =>
            {
                Socket.Send(DiscordMessageFactory.CreateLogin(BOT_TOKEN));
            };

            Socket.OnMessage += Socket_OnDataReceived;
            Socket.OnMessage += Socket_OnHeartbeatReceived;
            Socket.OnError += Socket_OnError;
            Socket.Connect();

            if(!TerrariaChatRelay.Config.Discord.FirstTimeMessageShown 
                || TerrariaChatRelay.Config.Discord.AlwaysShowFirstTimeMessage)
            {
                messageQueue.QueueMessage(Channel_IDs,
                    $"**This bot is powered by TerrariaChatRelay**\nUse {TerrariaChatRelay.Config.Discord.CommandPrefix}info for more commands!");
                TerrariaChatRelay.Config.Discord.FirstTimeMessageShown = true;
                TerrariaChatRelay.Config.SaveJson();
            }
        }

        /// <summary>
        /// Unsubscribes all WebSocket events, then releases all resources used by the WebSocket.
        /// </summary>
        public override void Disconnect()
        {
            Socket.OnMessage -= Socket_OnDataReceived;
            Socket.OnMessage -= Socket_OnHeartbeatReceived;
            Socket.OnError -= Socket_OnError;

            if (Socket.ReadyState != WebSocketState.Closed)
                Socket.Close();

            heartbeatTimer.Stop();
            heartbeatTimer.Dispose();
            heartbeatTimer = null;
        }

        /// <summary>
        /// Handles the heartbeat acknowledgement when the server asks for it.
        /// </summary>
        private void Socket_OnHeartbeatReceived(object sender, MessageEventArgs e)
        {
            var json = e.Data;

            if (json.Length <= 1)
                return;

            Discord.Models.DiscordMessage msg;
            if (!DiscordMessageFactory.TryParseMessage(json, out msg))
                return;

            if (msg.OpCode == DiscordGatewayOpcode.Hello)
            {
                if(heartbeatTimer != null)
                    heartbeatTimer.Dispose();

                heartbeatTimer = new System.Timers.Timer(((JObject)msg.Data).Value<int>("heartbeat_interval") / 2);
                heartbeatTimer.Elapsed += (senderr, ee) =>
                {
                    Socket.Send(DiscordMessageFactory.CreateHeartbeat(GetLastSequenceNumber()));
                    if (errorCounter > 0)
                        errorCounter--;
                };
                heartbeatTimer.Start();

                Socket.Send(DiscordMessageFactory.CreateHeartbeat(GetLastSequenceNumber()));
            }
        }

        /// <summary>
        /// Parses data when Discord sends a message.
        /// </summary>
        private void Socket_OnDataReceived(object sender, MessageEventArgs e)
        {
            var json = e.Data;

            if (json == null) return;
            if (json.Length <= 1) return;

            if (debug)
                Console.WriteLine("\n" + json + "\n");

            Discord.Models.DiscordDispatchMessage msg;
            if(!DiscordMessageFactory.TryParseDispatchMessage(json, out msg)) return;
            LastSequenceNumber = msg.SequenceNumber;

            var chatmsg = msg.GetChatMessageData();
            if(chatmsg != null && Channel_IDs.Contains(chatmsg.ChannelId))
            {
                if (!chatmsg.Author.IsBot)
                {
                    string msgout = chatmsg.Message;

                    // Lazy add commands until I take time to design a command service properly
                    if (ExecuteCommand(chatmsg))
                        return;

                    msgout = chatParser.ConvertUserIdsToNames(msgout, chatmsg.UsersMentioned);
                    msgout = chatParser.ShortenEmojisToName(msgout);
                    msgout = $"<{chatmsg.Author.Username}> {msgout}";

                    NetHelpers.BroadcastChatMessageWithoutTCR(
                        NetworkText.FromFormattable("[c/7489d8:Discord] - " + msgout),
                        new Color(255, 255, 255), -1);

                    if (Channel_IDs.Count > 1)
                    {
                        messageQueue.QueueMessage(
                            Channel_IDs.Where(x => x != chatmsg.ChannelId), 
                            $"**[Discord]** <{chatmsg.Author.Username}> {chatmsg.Message}");
                    }

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("[Discord] ");
                    Console.ResetColor();
                    Console.Write(msgout);
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Attempts to reconnect after receiving an error.
        /// </summary>
        private void Socket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Disconnect();
            Connect();
        }

        public override void GameMessageReceivedHandler(object sender, TerrariaChatEventArgs msg)
        {
            if (errorCounter > 2)
                return;
            try
            {
                string PlayerName = "";
                string Bold = "";

                if (msg.PlayerId != -1)
                    PlayerName = "**" + Main.player[msg.PlayerId].name + ":** ";
                else
                    Bold = "**";

                messageQueue.QueueMessage(Channel_IDs, PlayerName + Bold + msg.Message + Bold);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                errorCounter++;

                if(errorCounter > 2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Discord Client has been terminated. Please reload the mod to issue a reconnect.");
                    Console.ResetColor();
                }
            }
        }

        public async void SendMessageToDiscordChannel(ulong channelId, string message)
        {
            message = message.Replace("\\", "\\\\");
            message = message.Replace("\"", "\\\"");
            message = message.Replace("\n", "\\n");
            string json = DiscordMessageFactory.CreateTextMessage(message);

            string response = await SimpleRequest.SendJsonDataAsync($"{API_URL}/channels/{channelId}/messages",
                new WebHeaderCollection()
                    {
                        { "Authorization", $"Bot {BOT_TOKEN}" }
                    }, json);

            if (debug)
            {
                Console.WriteLine(response);
            }
        }

        public override void GameMessageSentHandler(object sender, TerrariaChatEventArgs msg)
        {

        }

        public int? GetLastSequenceNumber()
        {
            return LastSequenceNumber;
        }

        public bool ExecuteCommand(Discord.Models.DiscordMessageData chatmsg)
        {
            var message = chatmsg.Message;
            var prefix = TerrariaChatRelay.Config.Discord.CommandPrefix;

            if (message.Length > 0)
            {
                if (message.StartsWith(prefix))
                {
                    message = message.Substring(prefix.Length, message.Length - 1);
                }
            }

			if (message.StartsWith("cmd "))
			{
				message = message.Replace("cmd ", "");
				//Main.ExecuteCommand(message, new TCRCommandCaller());
			}

			switch (message)
            {
                case "info":
                    messageQueue.QueueMessage(chatmsg.ChannelId,
                        $"**Command List**\n```\n{prefix}playing - See who's online\n{prefix}world - See world information```");
                    return true;
                case "playing":
                    var playersOnline = string.Join(", ", Main.player.Where(x => x.name.Length != 0).Select(x => x.name));

                    if (playersOnline == "")
                        playersOnline = "No players online!";

                    messageQueue.QueueMessage(chatmsg.ChannelId,
                        $"**Currently Playing:**\n```{playersOnline} ```");
                    return true;
                case "world":
                    messageQueue.QueueMessage(chatmsg.ChannelId,
                        $"**World:** {Main.worldName}\n```\nDifficulty: {(Main.expertMode == false ? "Normal" : "Expert")}\nHardmode: {(Main.hardMode == false ? "No" : "Yes")}\nEvil Type: {(WorldGen.crimson == false ? "Corruption" : "Crimson")}```");
                    return true;
                default:
					return false;
            }
        }

		private class TCRCommandCaller : CommandCaller
		{
			public CommandType CommandType => CommandType.Console;

			public Player Player => null;

			public void Reply(string text, Color color = default(Color))
			{
				string[] array = text.Split('\n');
				foreach (string value in array)
				{
					EventManager.RaiseTerrariaMessageReceived(null, -1, Color.Aqua, value);
				}
			}
		}
	}
}
