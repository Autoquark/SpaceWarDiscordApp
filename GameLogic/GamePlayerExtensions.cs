using SpaceWarDiscordApp.DatabaseModels;

namespace SpaceWarDiscordApp.GameLogic;

public static class GamePlayerExtensions
{
    public static async Task<string> GetNameAsync(this GamePlayer player, bool mention)
    {
        if (player.IsDummyPlayer)
        {
            return player.DummyPlayerName;
        }

        var user = await Program.DiscordClient.GetUserAsync(player.DiscordUserId);
        return mention ? user.Mention : user.GlobalName;
    }
}