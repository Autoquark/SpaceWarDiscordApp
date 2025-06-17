using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.GameLogic.Techs;
using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord.ChoiceProvider;

public class MarketTechIdChoiceProvider : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        var game = await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.GetGameForChannelAsync(context.Channel.Id));
        if (game == null)
        {
            return [];
        }
        
        var prefix = context.UserInput ?? "";
        return game.TechMarket.WhereNonNull()
            .Select(x => Tech.TechsById[x])
            .Where(x => x.DisplayName.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
            .OrderBy(x => x.DisplayName)
            .Select(x => new DiscordAutoCompleteChoice(x.DisplayName, x.Id.ToString()));
    }
}