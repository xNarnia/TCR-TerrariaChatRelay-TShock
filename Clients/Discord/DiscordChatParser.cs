using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Terraria;

namespace TerrariaChatRelay.Clients.Discord
{
    public class DiscordChatParser
    {
        Regex specialFinder { get; }

        public DiscordChatParser()
        {
            specialFinder = new Regex(@":[^:\s]*(?:::[^:\s]*)*>");
        }

        public string ConvertUserIdsToNames(string chatMessage, List<Models.DiscordUser> users)
        {
            var CyanColor = Color.Cyan.Hex3();

            foreach (var user in users)
            {
                chatMessage = chatMessage.Replace($"<@{user.Id}>", $"[c/{CyanColor}:@" + user.Username.Replace("[", "").Replace("]", "") + "]");
            }

            return chatMessage;
        }

        public string ShortenEmojisToName(string chatMessage)
        {
            chatMessage = specialFinder.Replace(chatMessage, ":");
            chatMessage = chatMessage.Replace("<:", ":");
            chatMessage = chatMessage.Replace("<a:", ":");

            return chatMessage;
        }
    }
}
