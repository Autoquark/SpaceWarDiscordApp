using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord.ChoiceProvider;

public class HexCoordsAutoCompleteProvider : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        //TODO: Allow filtering further (e.g. only hexes with planets). Probably get config from context.Parameter.Attributes
        var game = await Program.FirestoreDb.RunTransactionAsync(transaction =>
            transaction.GetGameForChannelAsync(context.Channel));

        if (game == null)
        {
            return [];
        }

        return Filter(game.Hexes).Select(x =>
            new DiscordAutoCompleteChoice(x.Coordinates.ToString(), x.Coordinates.ToCoordsString()));
    }

    protected virtual IEnumerable<BoardHex> Filter(IEnumerable<BoardHex> boardHexes) => boardHexes;
}