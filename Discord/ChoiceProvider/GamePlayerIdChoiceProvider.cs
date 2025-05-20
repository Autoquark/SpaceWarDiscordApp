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
        
        return await Task.WhenAll(game.Players.Select(async x =>
            new DiscordAutoCompleteChoice(await x.GetNameAsync(false, false), x.GamePlayerId)));
    }
}