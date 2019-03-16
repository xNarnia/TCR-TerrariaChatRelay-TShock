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

namespace TerrariaChatRelay.Clients
{
    public class MessyTestDiscordChatClient : BaseClient
    {
        private List<IChatClient> _parent { get; set; }
        private const string GATEWAY_URL = "wss://gateway.discord.gg/?v=6&encoding=json";
        private const string API_URL = "https://discordapp.com/api/v6";
        private const string BOT_TOKEN = "NTU0MzExNzI2MjIxMzYxMTYy.D26hNg._Q7mTpzVAuo35JOj5sWu8wIDXBY";
        private const string CHANNEL_ID = "455716114761121795";
        private readonly HttpClient client;
        private SimpleSocket Socket;

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
            Socket = new SimpleSocket(GATEWAY_URL);
            Socket.OnDataReceived += Websocket_OnDataReceived;
        }

        public override Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        private void Websocket_OnDataReceived(string obj)
        {
            Console.WriteLine("\n" + obj + "\n");

            if (doLogin)
            {
                doLogin = false;
                Socket.SendData(DoLogin());
            }
            else
            {
                try
                {
                    var message = JsonConvert.DeserializeObject<DiscordDispatchMessage>(obj);

                    if (message.MessageType == "MESSAGE_CREATE" && !message.Data.Author.IsBot)
                    {
                        NetMessage.BroadcastChatMessage(
                            NetworkText.FromLiteral(
                                "[Discord] <" + message.Data.Author.Username + "> " + message.Data.Message), Color.White, -1);
                    }
                }
                catch (Exception) { }


                //JObject jObject = JObject.Parse(obj);
                //if(jObject.TryGetValue("op", out JToken opcode))
                //{
                //    if(opcode.Value<int>() == 0)
                //    {
                //        if (jObject.TryGetValue("t", out JToken type))
                //        {
                //            if (type.Value<string>() == "MESSAGE_CREATE")
                //            {
                //                var message = JsonConvert.DeserializeObject<DiscordCreateMessage>(obj);

                //                NetMessage.BroadcastChatMessage(
                //                    NetworkText.FromLiteral(
                //                        "[Discord] <" + message.Author.Username + "> " + message.Message), Color.White, -1);
                //            }
                //        }
                //    }
                //}
            }
        }

        // Example login json
        private string DoLogin()
        {
            return "{\"op\":2,\"d\":{\"token\":\"" + BOT_TOKEN + "\",\"properties\":{\"$os\":\"linux\",\"$browser\":\"app\",\"$device\":\"mono\"},\"compress\":false}}";
        }

        public override async void GameMessageReceivedHandlerAsync(object sender, TerrariaChatEventArgs msg)
        {
            // TO-DO: Implement NewtonsoftJson 
            //var json = "{\"content\":\"Incoming! <@&554312082137546762> <@446048405844918272>\",\"tts\":false,\"embed\":{\"title\":\"" + msg.Message + "\",\"description\":\"This message was sent from Terraria.\"}}";
            var json = "{\"content\":\"" + Main.player[msg.PlayerId].name + ": " + msg.Message + "\",\"tts\":false}";

            var response = await client.PostAsync(new Uri(API_URL + "/channels/" + CHANNEL_ID + "/messages"), new StringContent(json, Encoding.UTF8, "application/json"));
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        public override void GameMessageSentHandlerAsync(object sender, TerrariaChatEventArgs msg)
        {

        }
    }
}
