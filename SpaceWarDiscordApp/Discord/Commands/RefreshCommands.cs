using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Refresh;
using SpaceWarDiscordApp.Database.Interactions;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord.Commands;

public class RefreshCommands : ISpaceWarInteractionHandler<RefreshActionInteraction>
{
    public async Task<InteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        RefreshActionInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        if (game.EventStack.Count > 0)
        {
            builder?.AppendContentNewline("You can't click this right now because the game is waiting on a different decision:");
            await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, serviceProvider);
            return new InteractionOutcome(false);
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

        return new InteractionOutcome(true);
    }
}