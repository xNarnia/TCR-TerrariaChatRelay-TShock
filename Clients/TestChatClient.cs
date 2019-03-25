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
        public TestChatClient(List<IChatClient> parent) : base(parent) { }

        public override Task ConnectAsync()
        {
            return null;
        }

        public override Task DisconnectAsync()
        {
            return null;
        }

        public override void GameMessageReceivedHandlerAsync(object sender, TerrariaChatEventArgs e)
        {
            NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(e.Message + " - TestChatClient"), Color.Cyan, -1);
        }

        public override void GameMessageSentHandlerAsync(object sender, TerrariaChatEventArgs msg)
        {

        }
    }
}
