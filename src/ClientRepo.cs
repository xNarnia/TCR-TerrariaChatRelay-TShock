using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaChatRelay.Clients.Interfaces;

namespace TerrariaChatRelay
{
    public class ClientRepo
    {
        /// <summary>
        /// List for IChatClients to add themselves too for tracking.
        /// </summary>
        public static List<IChatClient> Clients { get; set; }
    }
}
