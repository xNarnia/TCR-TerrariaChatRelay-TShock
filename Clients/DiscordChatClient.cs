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

namespace TerrariaChatRelay.Clients
{
    public class MessyTestDiscordChatClient : BaseClient
    {
        private List<IChatClient> _parent { get; set; }
        private const string GATEWAY_URL = "wss://gateway.discord.gg/?v=6&encoding=json";
        private const string API_URL = "https://discordapp.com/api/v6";
        private const string BOT_TOKEN = "NTU0MzExNzI2MjIxMzYxMTYy.D22adw.zqY8nv_qDJqkgabduPmS2E9WQi4";
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

        private void Websocket_OnDataReceived(string obj)
        {
            Console.WriteLine(obj);
            if (doLogin)
            {
                doLogin = false;
                Socket.SendData(DoLogin());
            }
        }

        // Test Connect method just to get it working
        public async Task ConnectAsyncWithoutSimpleSocket()
        {
            var websocket = new ClientWebSocket();
            var buffer = new ArraySegment<Byte>(new Byte[1024]);
            var data = new StringBuilder();
            var source = new CancellationTokenSource();
            var token = source.Token;
            bool doLogin = false;
            bool loginDone = false;

            await websocket.ConnectAsync(new Uri(GATEWAY_URL), token).ConfigureAwait(false);

            // Consider a better way to handle this loop
            // Is this blocking the thread?
            while (websocket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                if (!doLogin || loginDone == true)
                {
                    var result = await websocket.ReceiveAsync(buffer, token).ConfigureAwait(false);

                    foreach (var byteValue in buffer)
                    {
                        data.Append(Convert.ToChar(byteValue));
                    }

                    if (result.EndOfMessage)
                    {
                        //HandleData(data.ToString());
                        Console.WriteLine(data.ToString());
                        data.Clear();
                        doLogin = true;
                    }
                }
                else
                {
                    var encoded = Encoding.UTF8.GetBytes(DoLogin());
                    buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);

                    await websocket.SendAsync(buffer, WebSocketMessageType.Text, true, token).ConfigureAwait(false);
                    loginDone = true;
                }
            }

            Console.WriteLine("Disconnected.");
        }

        public override Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        // Example login json
        private string DoLogin()
        {
            return "{\"op\":2,\"d\":{\"token\":\"" + BOT_TOKEN + "\",\"properties\":{\"$os\":\"linux\",\"$browser\":\"app\",\"$device\":\"mono\"},\"compress\":false}}";
        }

        public override async void GameMessageReceivedHandlerAsync(object sender, TerrariaChatEventArgs msg)
        {
            // TO-DO: Implement NewtonsoftJson 
            var json = "{\"content\":\"Incoming!\",\"tts\":false,\"embed\":{\"title\":\"" + msg.Message + "\",\"description\":\"This message was sent from Terraria.\"}}";

            var response = await client.PostAsync(new Uri(API_URL + "/channels/" + CHANNEL_ID + "/messages"), new StringContent(json, Encoding.UTF8, "application/json"));
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        public override void GameMessageSentHandlerAsync(object sender, TerrariaChatEventArgs msg)
        {

        }
    }
}
