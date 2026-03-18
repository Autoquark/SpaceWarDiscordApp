using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.Discord.ChoiceProvider;

public class HexCoordsAutoCompleteProvider : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        var game = await Program.FirestoreDb.RunTransactionAsync(transaction =>
            transaction.GetGameForChannelAsync(context.Channel));

        if (game == null)
        {
            return [];
        }

        var matches =  Filter(game.Hexes).ToList();

        if (context.UserInput != null)
        {
            var partialMatch = matches.Where(x => x.Coordinates.ToHexNumberString().StartsWith(context.UserInput))
                .ToList();
            if (partialMatch.Count != 0)
            {
                matches = partialMatch.ToList();
            }
        }
        
        return matches.Select(x => new DiscordAutoCompleteChoice(x.Coordinates.ToString(), x.Coordinates.ToCoordsString()));
    }

    protected virtual IEnumerable<BoardHex> Filter(IEnumerable<BoardHex> boardHexes) => boardHexes;
}