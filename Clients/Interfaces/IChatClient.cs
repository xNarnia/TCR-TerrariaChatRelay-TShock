using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Clients.Interfaces
{
    public interface IChatClient
    {
        /// <summary>
        /// Event fired when MessageReceived from service.
        /// </summary>
        event Func<string, Task> MessageReceived;
        /// <summary>
        /// Event fired when MessageSent from Terraria.
        /// </summary>
        event Func<string, Task> MessageSent;

        /// <summary>
        /// Initialize client to parent repo.
        /// </summary>
        /// <param name="parent">Parent repo to register with.</param>
        void Init(List<IChatClient> parent);
        
        /// <summary>
        /// Handle cleanup, de-register, and dispose client.
        /// </summary>
        void Dispose();
        
        /// <summary>
        /// Initiate connection to service.
        /// </summary>
        void Connect();

        /// <summary>
        /// Terminate connection to service.
        /// </summary>
        void Disconnect();
    }
}
