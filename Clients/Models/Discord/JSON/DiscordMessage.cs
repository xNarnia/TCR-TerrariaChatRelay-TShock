using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaChatRelay.Clients.Models.Discord.JSON
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

        /// <summary>
        /// Checks to see if the DispatchMessage has any associated Chat Message data. If not, it returns null.
        /// </summary>
        /// <returns>Data if it is present.</returns>
        public DiscordMessageData GetChatMessageData()
        {
            if(MessageType == "MESSAGE_CREATE")
            {
                return ((JObject)Data).ToObject<DiscordMessageData>();
            }
            else
            {
                return null;
            }
        }
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
        public object Data { get; set; }
    }
}
