using System;
using System.Collections.Generic;
using System.Linq;
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
        public ConnectionState ConnectState { get { return connectionState; } }
        /// <summary>
        /// Uri used to initiate WebSocket connection.
        /// </summary>
        public Uri SocketUri { get; }

        private ArraySegment<Byte> buffer { get; set; }
        private StringBuilder builder { get; set; }
        private CancellationToken token { get { return CancellationToken.Token;  } }
        private ConnectionState connectionState { get; set; }

        public enum ConnectionState
        {
            Closed,
            Open,
            Sending,
            Receiving,
            Error
        }

        /// <summary>
        /// Initializes a maintained WebSocket on another thread using the Uri provided. 
        /// </summary>
        /// <param name="uri">Uri to initialize WebSocket with. Supports ws and wss format.</param>
        public SimpleSocket(Uri uri)
        {
            SocketUri = uri;

            // Remove this as these are Discord variables; not generic enough
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
        }
    }
}
