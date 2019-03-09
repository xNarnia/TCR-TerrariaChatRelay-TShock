using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace TerrariaChatRelay
{
	public class TerrariaChatRelay : Mod
	{
		public TerrariaChatRelay()
		{

		}

        public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
        {
            // Chat Message    [25]
            // Chat Message v2 [107]

            // Test Code
            if (messageType == 25 || messageType == 107)
            {
                byte PlayerId = reader.ReadByte();
                Color color = reader.ReadRGB();
                string msg = reader.ReadString();

                // 
            }

            return false;
        }
    }
}
