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
        public event Func<string, Task> MessageReceived;
        public event Func<string, Task> MessageSent;
        private List<IChatClient> _parent;

        /// <summary>
        /// Base class for IChatClients. Registers self into static ClientRepo.
        /// </summary>
        /// <param name="parent"></param>
        public BaseClient(List<IChatClient> parent)
            => Init(parent);

        /// <summary>
        /// Handle disposing of client.
        /// </summary>
        ~BaseClient()
            => Dispose();

        /// <summary>
        /// Registers self to ClientRepo
        /// </summary>
        /// <param name="parent"></param>
        public void Init(List<IChatClient> parent)
        {
            _parent = parent;
            _parent.Add(this);
        }

        /// <summary>
        /// De-registers self from ClientRepo and destroys events
        /// </summary>
        public void Dispose()
        {
            _parent.Remove(this);
            MessageReceived = null;
            MessageSent = null;
        }

        public abstract void Connect();
        public abstract void Disconnect();
    }
}
