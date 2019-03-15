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
        public ClientWebSocket WebSocket { get; set; }
        /// <summary>
        /// Token used by the WebSocket.
        /// </summary>
        public CancellationTokenSource CancellationToken { get; }
        /// <summary>
        /// Specifies the current state of the WebSocket.
        /// </summary>
        public WebSocketState ConnectState { get { return WebSocket.State; } }
        /// <summary>
        /// Uri used to initiate WebSocket connection.
        /// </summary>
        public Uri SocketUri { get; }
        /// <summary>
        /// Event fired when WebSocket receives one set of data from start to finish.
        /// </summary>
        public event Action<string> OnDataReceived;

        private ArraySegment<Byte> buffer { get; set; }
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
            WebSocket = new ClientWebSocket();
            CancellationToken = new CancellationTokenSource();
            SocketUri = uri;

            buffer = new ArraySegment<byte>();
            data = new StringBuilder();
            sendQueue = new Queue<byte[]>();

            InitializeAsync();
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
        private async void InitializeAsync()
        {
            await WebSocket.ConnectAsync(SocketUri, token);

            var MessageReceivedListener = Task.Run(async () =>
            {
                bool IncomingMessage = false;

                while (SocketOpen())
                {
                    var result = await WebSocket.ReceiveAsync(buffer, token).ConfigureAwait(false);

                    if (!result.EndOfMessage && !IncomingMessage)
                        IncomingMessage = true;

                    if (IncomingMessage)
                    {
                        foreach (var byteValue in buffer)
                        {
                            data.Append(Convert.ToChar(byteValue));
                        }

                        if (result.EndOfMessage)
                        {
                            OnDataReceived(data.ToString());
                            data.Clear();
                            IncomingMessage = false;
                        }
                    }

                    await Task.Delay(10);
                }
            });

            var MessageSentListener = Task.Run(async () =>
            {
                while (SocketOpen())
                {
                    while(sendQueue.Count > 0)
                    {
                        byte[] sendData = sendQueue.Dequeue();

                        buffer = new ArraySegment<Byte>(sendData, 0, sendData.Length);
                        await WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, token);
                    }
                }
            });

            Task.WaitAll(MessageReceivedListener, MessageSentListener);
        }

        /// <summary>
        /// Returns whether the WebSocket is open and hasn't been canceled.
        /// </summary>
        /// <returns></returns>
        private bool SocketOpen()
        {
            if (WebSocket.State != WebSocketState.Open)
                return false;
            if (token.IsCancellationRequested)
                return false;

            return true;
        }

    }
}
