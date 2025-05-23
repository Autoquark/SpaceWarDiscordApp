using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord.ChoiceProvider;

public class TechIdChoiceProvider : IAutoCompleteProvider
{
    public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        => ValueTask.FromResult(Tech.TechsById.Select(x =>
            new DiscordAutoCompleteChoice(x.Value.DisplayName, x.Key.ToString())));
}