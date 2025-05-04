using SpaceWarDiscordApp.DatabaseModels;

namespace SpaceWarDiscordApp.GameLogic;

public static class GamePlayerExtensions
{
    /// <summary>
    /// Gets the name by which to address the given player. May contact the discord API
    /// </summary>
    /// <param name="mention">If true and player is not a dummy, returns a string that will mention the player in a discord message</param>
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