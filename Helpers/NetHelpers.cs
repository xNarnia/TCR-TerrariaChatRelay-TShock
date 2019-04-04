using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;
using Terraria.Net;

namespace TerrariaChatRelay.Helpers
{
    public static class NetHelpers
    {
        public static void BroadcastChatMessageWithoutTCR(NetworkText text, Color color, int excludedPlayer)
        {
            NetPacket packet = Terraria.GameContent.NetModules.NetTextModule.SerializeServerMessage(text, color, byte.MaxValue);
            NetManager.Instance.Broadcast(packet, excludedPlayer);
        }
    }
}
