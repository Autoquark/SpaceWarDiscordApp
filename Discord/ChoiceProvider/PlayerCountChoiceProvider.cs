using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Discord.ChoiceProvider;

public class PlayerCountChoiceProvider : IChoiceProvider
{
    public ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
        => ValueTask.FromResult(CollectionExtensions.Between(2, GameConstants.MaxPlayerCount).Select(x => new DiscordApplicationCommandOptionChoice(x.ToString(), x)));
}