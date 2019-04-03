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
using TerrariaChatRelay.Clients.Models.Discord;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using System.Net;
using WebSocketSharp;
using Terraria.Net;
using System.Text.RegularExpressions;

namespace TerrariaChatRelay.Clients
{
    public class DiscordChatClient : BaseClient
    {
        // URLs
        public const string GATEWAY_URL = "wss://gateway.discord.gg/?v=6&encoding=json";
        public const string API_URL = "https://discordapp.com/api/v6";

        // Discord Variables
        private string BOT_TOKEN = TerrariaChatRelay.Config["DiscordBotToken"];
        private string CHANNEL_ID = TerrariaChatRelay.Config["DiscordChannelId"];

        private List<IChatClient> _parent { get; set; }
        private WebSocket Socket;
        private int? LastSequenceNumber = 0;
        private System.Timers.Timer heartbeatTimer;
        private bool debug = false;
        private Regex specialFinder { get; set; }

        public DiscordChatClient(List<IChatClient> _parent) 
            : base(_parent) { }

        public override void Connect()
        {
            if (BOT_TOKEN == "BOT_TOKEN" || CHANNEL_ID == "CHANNEL_ID")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("TerrariaChatRelay [Discord] - Please update your Mod Config. Mod reload required.");
                Console.WriteLine("  Config path: " + TerrariaChatRelay.Config.FilePath);
                Console.ResetColor();
                this.Dispose();
                return;
            }

            specialFinder = new Regex(@":[^:\s]*(?:::[^:\s]*)*>");

            Socket = new WebSocket(GATEWAY_URL);
            Socket.OnOpen += (object sender, EventArgs e) =>
            {
                Socket.Send(DiscordMessageFactory.CreateLogin(BOT_TOKEN));
            };

            Socket.OnMessage += Websocket_OnDataReceived;
            Socket.OnMessage += Websocket_OnHeartbeatReceived;
            Socket.Connect();
        }

        public override void Disconnect()
        {
            Socket.Close();
            heartbeatTimer.Stop();
            heartbeatTimer.Dispose();
        }

        private void Websocket_OnHeartbeatReceived(object sender, MessageEventArgs e)
        {
            var json = e.Data;

            if (json.Length <= 1)
                return;

            Models.Discord.JSON.DiscordMessage msg;
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
                };
                heartbeatTimer.Start();

                Socket.Send(DiscordMessageFactory.CreateHeartbeat(GetLastSequenceNumber()));
            }
        }

        private void Websocket_OnDataReceived(object sender, MessageEventArgs e)
        {
            var json = e.Data;

            if (json == null) return;
            if (json.Length <= 1) return;

            if (debug && false)
            {
                Console.WriteLine("\n" + json + "\n");
            }

            Models.Discord.JSON.DiscordDispatchMessage msg;
            if(!DiscordMessageFactory.TryParseDispatchMessage(json, out msg)) return;
            LastSequenceNumber = msg.SequenceNumber;

            var chatmsg = msg.GetChatMessageData();
            if(chatmsg != null && chatmsg.ChannelId == ulong.Parse(CHANNEL_ID))
            {
                if (!chatmsg.Author.IsBot)
                {
                    string msgout = chatmsg.Message;

                    foreach (var user in chatmsg.UsersMentioned)
                    {
                        msgout = msgout.Replace($"<@{user.Id}>", $"[c/{Color.Cyan.Hex3()}:@" + user.Username.Replace("[", "").Replace("]", "") + "]");
                    }

                    msgout = specialFinder.Replace(msgout, ":");
                    msgout = msgout.Replace("<:", ":");

                    NetPacket packet = Terraria.GameContent.NetModules.NetTextModule
                        .SerializeServerMessage(NetworkText.FromFormattable("[c/7489d8:Discord] - <" + chatmsg.Author.Username + "> " + msgout), new Color(255, 255, 255), byte.MaxValue);
                    NetManager.Instance.Broadcast(packet, -1);
                }
            }
        }

        public override async void GameMessageReceivedHandler(object sender, TerrariaChatEventArgs msg)
        {
            try
            {
                // TO-DO: Implement NewtonsoftJson 

                string PlayerName = "";
                string Bold = "";
                if (msg.PlayerId != -1)
                {
                    PlayerName = "**" + Main.player[msg.PlayerId].name + ":** ";
                }
                else
                {
                    Bold = "**";
                }

                var json = DiscordMessageFactory.CreateTextMessage(PlayerName + Bold + msg.Message + Bold);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var contentbyte = await content.ReadAsByteArrayAsync().ConfigureAwait(false);

                // Legacy Post. Put in it's own Helper Class for use?
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.CreateHttp(new Uri(API_URL + "/channels/" + CHANNEL_ID + "/messages"));
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.ContentLength = json.Length;
                webRequest.Headers.Add("Authorization", "Bot " + BOT_TOKEN);

                var reqStream = await webRequest.GetRequestStreamAsync().ConfigureAwait(false);
                reqStream.Write(contentbyte, 0, json.Length);

                var res = await webRequest.GetResponseAsync().ConfigureAwait(false);

                using (StreamReader sr = new StreamReader(res.GetResponseStream()))
                {
                    var responseString = sr.ReadToEnd();
                    if (debug)
                    {
                        Console.WriteLine(responseString);
                    }
                }
            }
            catch (Exception e) { Console.WriteLine(e); }
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
