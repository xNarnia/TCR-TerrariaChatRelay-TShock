using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.UI.Chat;
using TerrariaChatRelay.Clients.Interfaces;

namespace TerrariaChatRelay
{
    public class EventManager
    {
        /// <summary>
        /// IChatClients list for clients to register with.
        /// </summary>
        public static List<IChatClient> Subscribers { get; set; }

        public static event EventHandler<TerrariaChatEventArgs> OnGameMessageReceived;
        public static event EventHandler<TerrariaChatEventArgs> OnGameMessageSent;

        /// <summary>
        /// Emits a message to all subscribers that a game message has been received.
        /// </summary>
        /// <param name="sender">Object that is emitting this event.</param>
        /// <param name="playerId">Id of player in respect to Main.Player[i], where i is the index of the player.</param>
        /// <param name="color">Color to display the text.</param>
        /// <param name="msg">Text content of the message</param>
        public static void RaiseTerrariaMessageReceived(object sender, int playerId, Color color, string msg)
        {
            var snippets = ChatManager.ParseMessage(msg, color);

            string outmsg = "";
            foreach (var snippet in snippets)
            {
                outmsg += snippet.Text;
            }

            OnGameMessageReceived?.Invoke(sender, new TerrariaChatEventArgs(playerId, color, outmsg));
        }

        public static void ConnectClients()
        {
            for(var i = 0; i < Subscribers.Count; i++)
            {
                Subscribers[i].Connect();
            }
        }

        public static void DisconnectClients()
        {
            foreach (var subscriber in Subscribers)
            {
                subscriber.Disconnect();
            }
        }
    }

    public class TerrariaChatEventArgs : EventArgs
    {
        public int PlayerId { get; set; }
        public Color Color { get; set; }
        public string Message { get; set; }

        public TerrariaChatEventArgs(int playerId, Color color, string msg)
        {
            PlayerId = playerId;
            Color = color;
            Message = msg;
        }
    }
}
