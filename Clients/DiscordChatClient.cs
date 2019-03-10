using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients.Interfaces;

namespace TerrariaChatRelay.Clients
{
    public class DiscordChatClient : BaseClient
    {
        private List<IChatClient> _parent { get; set; }

        public DiscordChatClient(List<IChatClient> _parent) 
            : base(_parent) { }

        public override void Connect()
        {

        }

        public override void Disconnect()
        {

        }

        public override void GameMessageReceived_Handler(object sender, TerrariaChatEventArgs msg)
        {

        }

        public override void GameMessageSent_Handler(object sender, TerrariaChatEventArgs msg)
        {

        }
    }
}
