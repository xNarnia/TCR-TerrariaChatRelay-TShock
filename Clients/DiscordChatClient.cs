using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients.Interfaces;

namespace TerrariaChatRelay.Clients
{
    public class DiscordChatClient : BaseClient, IHandlesEmotes, IHandlesAttachments
    {
        private List<IChatClient> _parent { get; set; }

        public DiscordChatClient(List<IChatClient> _parent) 
            : base(_parent) { }

        public override void Connect()
        {
            throw new NotImplementedException();
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        public string HandleEmotes(object obj)
        {
            throw new NotImplementedException();
        }

        public string HandleAttachment(object obj)
        {
            throw new NotImplementedException();
        }

        public override void GameMessageReceived_Handler(object sender, TerrariaChatEventArgs msg)
        {
            throw new NotImplementedException();
        }

        public override void GameMessageSent_Handler(object sender, TerrariaChatEventArgs msg)
        {
            throw new NotImplementedException();
        }
    }
}
