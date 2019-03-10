using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using TerrariaChatRelay.Clients;

namespace TerrariaChatRelay
{
	public class TerrariaChatRelay : Mod
    {

        public TerrariaChatRelay()
		{
		}

        public override void Load()
        {
            base.Load();

            EventManager.Subscribers = new List<Clients.Interfaces.IChatClient>();

            EventManager.Subscribers.Add(
                new TestChatClient(EventManager.Subscribers));
        }

        public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
        {
            // Chat Message    [25]
            // Chat Message v2 [107]
            // 82 packet is used when sending message?

            Main.NewText(messageType);
            // Test Code
            if (messageType == 25 || messageType == 107)
            {
                byte PlayerId = reader.ReadByte();
                Color color = reader.ReadRGB();
                string msg = reader.ReadString();

                EventManager.RaiseTerrariaMessageReceived(this, PlayerId, color, msg);
            }

            return false;
        }
    }
}
