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
    public class MessyTestDiscordChatClient : BaseClient
    {
        private List<IChatClient> _parent { get; set; }
        private const string GATEWAY_URL = "wss://gateway.discord.gg/?v=6&encoding=json";
        private const string API_URL = "https://discordapp.com/api/v6";
        private string BOT_TOKEN = TerrariaChatRelay.Config["DiscordBotToken"];
        private string CHANNEL_ID = TerrariaChatRelay.Config["DiscordChannelId"];
        private readonly HttpClient client;
        private SimpleSocket Socket;
        private int? LastSequenceNumber = 0;
        private System.Timers.Timer heartbeatTimer;
        private bool debug = false;

        private bool doLogin = true;

        // TO-DO: Make sure code supports C# 6 and that there are no drawbacks so coding is less annoying in the future

        public MessyTestDiscordChatClient(List<IChatClient> _parent) 
            : base(_parent)
        {
            client = new HttpClient(new HttpClientHandler
            {
                UseCookies = false
            });
            client.DefaultRequestHeaders.Add("Authorization", "Bot " + BOT_TOKEN);
        }

        public override async Task ConnectAsync()
        {
            if (BOT_TOKEN == "BOT_TOKEN")
                return;
            if (CHANNEL_ID == "CHANNEL_ID")
                return;

            Socket = new SimpleSocket(GATEWAY_URL);
            Socket.OnDataReceived += Websocket_OnDataReceived;
            Socket.OnDataReceived += Websocket_OnHeartbeatReceived;
        }

        private void Websocket_OnHeartbeatReceived(string json)
        {
            if (json.Length <= 1)
                return;

            var rawMessage = JsonConvert.DeserializeObject<DiscordMessage>(json);

            if (rawMessage.OpCode == DiscordGatewayOpcode.Hello)
            {
                if(heartbeatTimer != null)
                    heartbeatTimer.Dispose();

                heartbeatTimer = new System.Timers.Timer(((JObject)rawMessage.Data).Value<int>("heartbeat_interval") / 2);
                heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;

                Socket.SendData(SendHeartbeat());
            }
        }

        private void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // TO-DO: Implement NewtonsoftJson 
            //var json = "{\"content\":\"Incoming! <@&554312082137546762> <@446048405844918272>\",\"tts\":false,\"embed\":{\"title\":\"" + msg.Message + "\",\"description\":\"This message was sent from Terraria.\"}}";
            Socket.SendData(SendHeartbeat());
        }

        public override Task DisconnectAsync()
        {
            return null;
        }

        private void Websocket_OnDataReceived(string json)
        {
            if (json == null) return;
            if (json.Length <= 1) return;

            if(debug)
                Console.WriteLine("\n" + json + "\n");

            if (doLogin)
            {
                doLogin = false;
                Socket.SendData(DoLogin());
            }
            else
            {
                try
                {
                    var rawMessage = JsonConvert.DeserializeObject<DiscordMessage>(json);

                    if(rawMessage.OpCode == 0)
                    {
                        var rawDispatch = JsonConvert.DeserializeObject<DiscordDispatchMessage>(json);
                        LastSequenceNumber = rawDispatch.SequenceNumber;

                        if(rawDispatch.MessageType == "MESSAGE_CREATE")
                        {
                            var message = ((JObject)rawDispatch.Data).ToObject<DiscordMessageData>();

                            if (!message.Author.IsBot)
                            {
                                NetMessage.BroadcastChatMessage(
                                    NetworkText.FromLiteral(
                                        "[Discord] <" + message.Author.Username + "> " + message.Message), Color.White, -1);
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        // Example login json
        private string DoLogin()
        {
            return "{\"op\":2,\"d\":{\"token\":\"" + BOT_TOKEN + "\",\"properties\":{\"$os\":\"linux\",\"$browser\":\"app\",\"$device\":\"mono\"},\"compress\":false}}";
        }

        private string SendHeartbeat()
        {
            return "{\"op\": 1,\"d\": \"" + LastSequenceNumber + "\"}";
        }

        public override async void GameMessageReceivedHandlerAsync(object sender, TerrariaChatEventArgs msg)
        {
            // TO-DO: Implement NewtonsoftJson 
            var json = "{\"content\":\"" + Main.player[msg.PlayerId].name + ": " + msg.Message + "\",\"tts\":false}";

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var contentbyte = await content.ReadAsByteArrayAsync();

            // Legacy Post. Put in it's own Helper Class for use
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.CreateHttp(new Uri(API_URL + "/channels/" + CHANNEL_ID + "/messages"));
            webRequest.Method = "POST";
            webRequest.ContentType = "application/json";
            webRequest.ContentLength = json.Length;
            webRequest.Headers.Add("Authorization", "Bot " + BOT_TOKEN);

            var reqStream = await webRequest.GetRequestStreamAsync();
            reqStream.Write(contentbyte, 0, json.Length);

            var res = await webRequest.GetResponseAsync();
        }

        public override void GameMessageSentHandlerAsync(object sender, TerrariaChatEventArgs msg)
        {

        }
    }
}
