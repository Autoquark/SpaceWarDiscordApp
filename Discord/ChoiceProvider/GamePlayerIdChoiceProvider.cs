using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Discord.ChoiceProvider;

public class GamePlayerIdChoiceProvider : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        var game = await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.GetGameForChannelAsync(context.Channel.Id));
        if (game == null)
        {
            return [];
        }

        var prefix = context.UserInput ?? "";

        var playersWithNames = await Task.WhenAll(
            game.Players.Select(async player => (player, name: await player.GetNameAsync(false, false)))
        );
        
        return playersWithNames.OrderBy(x => x.name)
            .Where(x => x.name.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
            .Select(x => new DiscordAutoCompleteChoice(x.name, x.player.GamePlayerId.ToString()));
    }
}