using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
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

namespace TerrariaChatRelay.Clients
{
    public class DiscordChatClient : BaseClient
    {
        private List<IChatClient> _parent { get; set; }
        private const string GATEWAY_URL = "wss://gateway.discord.gg/?v=6&encoding=json";
        private const string API_URL = "https://discordapp.com/api/v6";
        private string BOT_TOKEN = TerrariaChatRelay.Config["DiscordBotToken"];
        private string CHANNEL_ID = TerrariaChatRelay.Config["DiscordChannelId"];
        private SimpleSocket Socket;
        private int? LastSequenceNumber = 0;
        private System.Timers.Timer heartbeatTimer;
        private bool debug = true;

        private bool doLogin = true;

        public DiscordChatClient(List<IChatClient> _parent) 
            : base(_parent) { }

        public override async Task ConnectAsync()
        {
            if (BOT_TOKEN == "BOT_TOKEN") return;
            if (CHANNEL_ID == "CHANNEL_ID") return;

            Socket = new SimpleSocket(GATEWAY_URL);

            Socket.Connected += () =>
            {
                Socket.SendData(DiscordMessageFactory.CreateLogin(BOT_TOKEN));
            };
            Socket.OnDataReceived += Websocket_OnDataReceived;
            Socket.OnDataReceived += Websocket_OnHeartbeatReceived;
        }

        public override Task DisconnectAsync()
        {
            return null;
        }

        private void Websocket_OnHeartbeatReceived(string json)
        {
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
                heartbeatTimer.Elapsed += (sender, e) =>
                {
                    Socket.SendData(DiscordMessageFactory.CreateHeartbeat(GetLastSequenceNumber()));
                };
                heartbeatTimer.Start();

                Socket.SendData(DiscordMessageFactory.CreateHeartbeat(GetLastSequenceNumber()));
            }
        }

        private void Websocket_OnDataReceived(string json)
        {
            if (json == null) return;
            if (json.Length <= 1) return;

            if (debug)
            {
                Console.WriteLine("\n" + json + "\n");
            }

            Models.Discord.JSON.DiscordDispatchMessage msg;
            if(!DiscordMessageFactory.TryParseDispatchMessage(json, out msg)) return;
            LastSequenceNumber = msg.SequenceNumber;

            var chatmsg = msg.GetChatMessageData();
            if(chatmsg != null)
            {
                if (!chatmsg.Author.IsBot)
                {
                    NetMessage.BroadcastChatMessage(
                        NetworkText.FromLiteral(
                            "[Discord] <" + chatmsg.Author.Username + "> " + chatmsg.Message), Color.White, -1);
                }
            }
        }

        public override async void GameMessageReceivedHandlerAsync(object sender, TerrariaChatEventArgs msg)
        {
            try
            {
                // TO-DO: Implement NewtonsoftJson 
                var json = DiscordMessageFactory.CreateTextMessage(Main.player[msg.PlayerId].name + ": " + msg.Message);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var contentbyte = await content.ReadAsByteArrayAsync();

                // Legacy Post. Put in it's own Helper Class for use?
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.CreateHttp(new Uri(API_URL + "/channels/" + CHANNEL_ID + "/messages"));
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.ContentLength = json.Length;
                webRequest.Headers.Add("Authorization", "Bot " + BOT_TOKEN);

                var reqStream = await webRequest.GetRequestStreamAsync();
                reqStream.Write(contentbyte, 0, json.Length);

                var res = await webRequest.GetResponseAsync();

            }
            catch (Exception e) { Console.WriteLine(e); }
        }

        public override void GameMessageSentHandlerAsync(object sender, TerrariaChatEventArgs msg)
        {

        }

        public int? GetLastSequenceNumber()
        {
            return LastSequenceNumber;
        }
    }
}
