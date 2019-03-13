using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients.Interfaces;

namespace TerrariaChatRelay.Clients
{
    public abstract class BaseClient : IChatClient
    {
        private List<IChatClient> _parent;

        /// <summary>
        /// Base class for IChatClients. Registers self into static ClientRepo.
        /// </summary>
        /// <param name="parent"></param>
        public BaseClient(List<IChatClient> parent)
        {
            Init(parent);
        }

        /// <summary>
        /// Handle disposing of client.
        /// </summary>
        ~BaseClient()
        {
            Dispose();
        }

        /// <summary>
        /// Registers self to ClientRepo
        /// </summary>
        /// <param name="parent"></param>
        public void Init(List<IChatClient> parent)
        {
            _parent = parent;
            _parent.Add(this);

            //EventManager.OnClientMessageReceived += ClientMessageReceived_Handler;
            //EventManager.OnClientMessageSent += ClientMessageSent_Handler;
            EventManager.OnGameMessageReceived += GameMessageReceivedHandlerAsync;
            EventManager.OnGameMessageSent += GameMessageSentHandlerAsync;
        }

        /// <summary>
        /// De-registers self from ClientRepo and destroys events
        /// </summary>
        public void Dispose()
        {
            _parent.Remove(this);
            EventManager.OnGameMessageReceived -= GameMessageReceivedHandlerAsync;
            EventManager.OnGameMessageSent -= GameMessageSentHandlerAsync;
        }

        public abstract Task ConnectAsync();
        public abstract Task DisconnectAsync();

        // Events
        //public abstract Task ClientMessageReceived_Handler(string msg);
        //public abstract Task ClientMessageSent_Handler(string msg);
        public abstract void GameMessageReceivedHandlerAsync(object sender, TerrariaChatEventArgs msg);
        public abstract void GameMessageSentHandlerAsync(object sender, TerrariaChatEventArgs msg);
    }
}
