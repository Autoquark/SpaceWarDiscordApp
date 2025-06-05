using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.Discord.ChoiceProvider;

public class TechIdChoiceProvider : IAutoCompleteProvider
{
    public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        var prefix = context.UserInput ?? "";
        return ValueTask.FromResult(Tech.TechsById.OrderBy(x => x.Value.DisplayName)
            .Where(x => x.Value.DisplayName.StartsWith(prefix))
            .Select(x => new DiscordAutoCompleteChoice(x.Value.DisplayName, x.Key.ToString())));
    }
}