using SpaceWarDiscordApp.DatabaseModels;

namespace SpaceWarDiscordApp.GameLogic;

public static class GamePlayerExtensions
{
    /// <summary>
    /// Gets the name by which to address the given player. May contact the discord API
    /// </summary>
    /// <param name="mention">If true and player is not a dummy, returns a string that will mention the player in a discord message</param>
    /// <param name="includeDieEmoji">If true, follows the name with a die emoji of the player's colour</param>
    public static async Task<string> GetNameAsync(this GamePlayer player, bool mention, bool includeDieEmoji = true)
    {
        string name;
        if (player.IsDummyPlayer)
        {
            name = player.DummyPlayerName!;
        }
        else
        {
            var user = await Program.DiscordClient.GetUserAsync(player.DiscordUserId);
            name = mention ? user.Mention : user.GlobalName;    
        }

        return includeDieEmoji ? 
            $"{name} {player.PlayerColourInfo.GetDieEmoji(6)}"
            : name;
    }
}