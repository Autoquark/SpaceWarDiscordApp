using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Refresh;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord.Commands;

public class RefreshCommands : IInteractionHandler<RefreshActionInteraction>
{
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        RefreshActionInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        if (game.EventStack.Count > 0)
        {
            builder?.AppendContentNewline("You can't click this right now because the game is waiting on a different decision:");
            await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, serviceProvider);
            return new SpaceWarInteractionOutcome(false);
        }

        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_FullRefresh
            {
                GamePlayerId = interactionData.ForGamePlayerId
            },
            new GameEvent_ActionComplete
            {
                ActionType = ActionType.Main,
            });

        return new SpaceWarInteractionOutcome(true);
    }
}