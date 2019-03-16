using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Clients.Models.Discord
{
    public class DiscordDispatchMessage : DiscordMessage
    {
        /// <summary>
        /// Sequence number, used for resuming sessions and heartbeats. For more info, visit Discord API Docs -> Gateway -> Payloads.
        /// </summary>
        [JsonProperty("s")]
        public int? SequenceNumber { get; set; }

        /// <summary>
        /// The event name for this payload. For more info, visit Discord API Docs -> Gateway -> Payloads.
        /// </summary>
        [JsonProperty("t")]
        public string MessageType { get; set; }
    }

    public class DiscordMessage
    {
        /// <summary>
        /// Opcode for the payload. For more info, visit Discord API Docs -> Gateway -> Payloads.
        /// </summary>
        [JsonProperty("op")]
        public DiscordGatewayOpcode OpCode { get; set; }

        /// <summary>
        /// Event data. A JSON value represented as a string. For more info, visit Discord API Docs -> Gateway -> Payloads.
        /// </summary>
        [JsonProperty("d")]
        public DiscordMessageData Data { get; set; }
    }
}
