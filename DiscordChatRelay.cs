using System.IO;
using Terraria.ModLoader;

namespace DiscordChatRelay
{
	public class DiscordChatRelay : Mod
	{
		public DiscordChatRelay()
		{
		}
        public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
        {
            return false;
        }
    }
}
