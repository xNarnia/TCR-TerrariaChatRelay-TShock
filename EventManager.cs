using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients.Interfaces;

namespace TerrariaChatRelay
{
    public class EventManager
    {
        /// <summary>
        /// IChatClients list for clients to register with.
        /// </summary>
        public static List<IChatClient> Subscribers { get; set; }

        //public static event EventHandler<TerrariaChatEventArgs> OnServiceMessageReceived;
        //public static event EventHandler<TerrariaChatEventArgs> OnServiceMessageSent;
        public static event EventHandler<TerrariaChatEventArgs> OnGameMessageReceived;
        public static event EventHandler<TerrariaChatEventArgs> OnGameMessageSent;


        public static void RaiseTerrariaMessageReceived(object sender, byte playerId, Color color, string msg)
        {
            OnGameMessageReceived(sender, new TerrariaChatEventArgs(playerId, color, msg));
        }
    }

    public class TerrariaChatEventArgs : EventArgs
    {
        public byte PlayerId { get; set; }
        public Color Color { get; set; }
        public string Message { get; set; }

        public TerrariaChatEventArgs(byte playerId, Color color, string msg)
        {
            PlayerId = playerId;
            Color = color;
            Message = msg;
        }
    }
}
