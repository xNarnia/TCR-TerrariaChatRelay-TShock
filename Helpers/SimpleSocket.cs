using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Helpers
{
    public class SimpleSocket
    {
        /// <summary>
        /// WebSocket that was initiated with provided Uri.
        /// </summary>
        public ClientWebSocket WebSocketClient { get; set; }
        /// <summary>
        /// Token used by the WebSocket.
        /// </summary>
        public CancellationTokenSource CancellationToken { get; }
        /// <summary>
        /// Specifies the current state of the WebSocket.
        /// </summary>
        public WebSocketState ConnectState { get { return WebSocketClient.State; } }
        /// <summary>
        /// Uri used to initiate WebSocket connection.
        /// </summary>
        public Uri SocketUri { get; }
        /// <summary>
        /// Event fired when WebSocket successfully establishes connection with socket provider.
        /// </summary>
        public event Action Connected;
        /// <summary>
        /// Event fired when WebSocket receives one set of data from start to finish.
        /// </summary>
        public event Action<string> OnDataReceived;

        private StringBuilder data { get; set; }
        private CancellationToken token { get { return CancellationToken.Token; } }
        private Queue<Byte[]> sendQueue { get; set; }

        public SimpleSocket(string uri) : this(new Uri(uri)) { }

        /// <summary>
        /// Initializes a maintained WebSocket on another thread using the Uri provided. 
        /// </summary>
        /// <param name="uri">Uri to initialize WebSocket with. Supports ws and wss format.</param>
        public SimpleSocket(Uri uri)
        {
            WebSocketClient = new ClientWebSocket();
            CancellationToken = new CancellationTokenSource();
            SocketUri = uri;

            data = new StringBuilder();
            sendQueue = new Queue<byte[]>();

            InitializeAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Uses UTF8 encoding to convert string into byte array, then queues the resulting data for sending.
        /// </summary>
        /// <param name="data">String data to encode into a byte sequence then send.</param>
        public void SendData(string data)
            => SendData(Encoding.UTF8.GetBytes(data));

        /// <summary>
        /// Queues the input data for sending.
        /// </summary>
        /// <param name="data">Byte data to send.</param>
        public void SendData(byte[] data)
        {
            sendQueue.Enqueue(data);
        }

        /// <summary>
        /// Initializes a Receive and Send listener using separate tasks. Sends and receives can happen asynchronously.
        /// </summary>
        private async Task InitializeAsync()
        {
            Console.WriteLine("Initializing Socket...");
            await WebSocketClient.ConnectAsync(SocketUri, token).ConfigureAwait(false);

            if (WebSocketClient.State != WebSocketState.Open)
            {
                Console.WriteLine($"Connection could not be established. SocketState: {WebSocketClient.State.ToString()}");
                return;
            }

            Connected?.Invoke();

            var MessageReceivedListener = Task.Run(async () =>
            {
                ArraySegment<Byte> receiveBuffer = WebSocket.CreateClientBuffer(1024, 1024);

                try
                {
                    while (SocketOpen())
                    {
                        WebSocketReceiveResult result = null;
                        result = await WebSocketClient.ReceiveAsync(receiveBuffer, token);

                        var i = result.Count;

                        //Console.WriteLine("Receiving data...");

                        foreach (var byteValue in receiveBuffer)
                        {
                            data.Append(Convert.ToChar(byteValue));
                            i--;

                            if (i <= 0)
                                break;
                        }

                        if (result.EndOfMessage)
                        {
                            OnDataReceived?.Invoke(data.ToString());
                            data.Clear();
                        }

                        await Task.Delay(50);
                    }


                    Console.WriteLine($"Connection State: " + WebSocketClient.State.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }).ConfigureAwait(false);

            var MessageSentListener = Task.Run(async () =>
            {
                while (SocketOpen())
                {
                    while(sendQueue.Count > 0)
                    {
                        byte[] sendData = sendQueue.Dequeue();

                        var sendBuffer = new ArraySegment<Byte>(sendData, 0, sendData.Length);
                        await WebSocketClient.SendAsync(sendBuffer, WebSocketMessageType.Text, true, token);
                    }
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns whether the WebSocket is open and hasn't been canceled.
        /// </summary>
        /// <returns></returns>
        private bool SocketOpen()
        {
            if (WebSocketClient.State != WebSocketState.Open)
                return false;
            if (token.IsCancellationRequested)
                return false;

            return true;
        }

    }
}
