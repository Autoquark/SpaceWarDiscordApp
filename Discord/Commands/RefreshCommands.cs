using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord.Commands;

public class RefreshCommands : IInteractionHandler<RefreshActionInteraction>
{
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync<TBuilder>(TBuilder builder,
        RefreshActionInteraction interactionData,
        Game game, IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        await RefreshOperations.Refresh(builder, game, game.GetGamePlayerByGameId(interactionData.ForGamePlayerId));

        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Main, serviceProvider);

        return new SpaceWarInteractionOutcome(true, builder);
    }
}