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

namespace TerrariaChatRelay.Clients
{
    public class DiscordChatClient : BaseClient
    {
        // URLs
        public const string GATEWAY_URL = "wss://gateway.discord.gg/?v=6&encoding=json";
        public const string API_URL = "https://discordapp.com/api/v6";

        // Discord Variables
        private string BOT_TOKEN;
        private ulong[] CHANNEL_IDs;
        private int? LastSequenceNumber = 0;
        private DiscordChatParser chatParser { get; set; }
        private System.Timers.Timer heartbeatTimer { get; set; }

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
            CHANNEL_IDs = channel_ids;
            chatParser = new DiscordChatParser();
        }

        /// <summary>
        /// Create a new WebSocket and initiate connection with Discord servers. 
        /// Utilizes BOT_TOKEN and CHANNEL_ID found in Mod Config.
        /// </summary>
        public override void Connect()
        {
            if (BOT_TOKEN == "BOT_TOKEN" || CHANNEL_IDs.Length == 1)
            {
                if (BOT_TOKEN == "BOT_TOKEN")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("TerrariaChatRelay [Discord] - Please update your Mod Config. Mod reload required.");
                    Console.WriteLine("  Config path: " + TerrariaChatRelay.Config.FileName);
                    Console.ResetColor();
                    this.Dispose();
                    return;
                }
            }

            errorCounter = 0;

            Socket = new WebSocket(GATEWAY_URL);
            Socket.OnOpen += (object sender, EventArgs e) =>
            {
                Socket.Send(DiscordMessageFactory.CreateLogin(BOT_TOKEN));
            };

            Socket.OnMessage += Socket_OnDataReceived;
            Socket.OnMessage += Socket_OnHeartbeatReceived;
            Socket.OnError += Socket_OnError;
            Socket.Connect();
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

            if (debug && false)
            {
                Console.WriteLine("\n" + json + "\n");
            }

            Discord.Models.DiscordDispatchMessage msg;
            if(!DiscordMessageFactory.TryParseDispatchMessage(json, out msg)) return;
            LastSequenceNumber = msg.SequenceNumber;

            var chatmsg = msg.GetChatMessageData();
            if(chatmsg != null && CHANNEL_IDs.Contains(chatmsg.ChannelId))
            {
                if (!chatmsg.Author.IsBot)
                {
                    string msgout = chatmsg.Message;

                    msgout = chatParser.ConvertUserIdsToNames(msgout, chatmsg.UsersMentioned);
                    msgout = chatParser.ShortenEmojisToName(msgout);
                    msgout = $"<{chatmsg.Author.Username}> {msgout}";

                    NetHelpers.BroadcastChatMessageWithoutTCR(
                        NetworkText.FromFormattable("[c/7489d8:Discord] - " + msgout),
                        new Color(255, 255, 255), -1);

                    foreach(var channelid in CHANNEL_IDs)
                    {
                        if(channelid != chatmsg.ChannelId)
                            SendMessageToDiscordChannel(channelid, $"**[Discord]** {msgout}");
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

        public override async void GameMessageReceivedHandler(object sender, TerrariaChatEventArgs msg)
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

                foreach (var channelid in CHANNEL_IDs)
                {
                    SendMessageToDiscordChannel(channelid, PlayerName + Bold + msg.Message + Bold);
                }
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
    }
}
