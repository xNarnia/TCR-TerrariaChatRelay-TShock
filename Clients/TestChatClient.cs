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
            return Task.CompletedTask;
        }

        public override Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public override void GameMessageReceived_Handler(object sender, TerrariaChatEventArgs e)
        {
            NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(e.Message + " - TestChatClient"), Color.Cyan, -1);
        }

        public override void GameMessageSent_Handler(object sender, TerrariaChatEventArgs msg)
        {

        }
    }
}
