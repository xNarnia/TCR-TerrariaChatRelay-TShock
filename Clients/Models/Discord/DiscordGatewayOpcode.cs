namespace TerrariaChatRelay.Clients.Models.Discord
{
    public enum DiscordGatewayOpcode
    {
        Dispatch = 0,
        Heartbeat = 1,
        Identify = 2,
        StatusUpdate = 3,
        VoiceStatusUpdate = 4,
        Resume = 6,
        Reconnect = 7,
        RequestGuildMembers = 8,
        InvalidSession = 9,
        Hello = 10,
        HeartbeatAcknowledged = 11
    }
}
