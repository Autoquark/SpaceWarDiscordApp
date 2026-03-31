using Tumult.Database;
using Tumult.Database.Interactions;

namespace Tumult.Discord;

public interface IInteractionHandler<TInteractionData, TGame>
    where TInteractionData : InteractionData
    where TGame : BaseGame
{
    Task<InteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        TInteractionData interactionData,
        TGame game,
        IServiceProvider serviceProvider);
}
