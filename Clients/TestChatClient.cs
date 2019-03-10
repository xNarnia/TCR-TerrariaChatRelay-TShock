using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using TerrariaChatRelay.Clients.Interfaces;

namespace TerrariaChatRelay.Clients
{
    public class TestChatClient : BaseClient
    {
        public TestChatClient(List<IChatClient> parent) : base(parent) { EventManager.OnGameMessageReceived += GameMessageReceived_Handler; }

        public override void Connect()
        {

        }

        public override void Disconnect()
        {

        }

        public override void GameMessageReceived_Handler(object sender, TerrariaChatEventArgs msg)
        {
            NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(msg + " It worked!"), Color.Cyan, -1);
            throw new NotImplementedException();
        }

        public override void GameMessageSent_Handler(object sender, TerrariaChatEventArgs msg)
        {
            throw new NotImplementedException();
        }
    }
}
