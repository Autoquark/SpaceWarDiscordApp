using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Production;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord.Commands;

public class ProduceCommands : IInteractionHandler<ShowProduceOptionsInteraction>,
    IInteractionHandler<ProduceInteraction>
{
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        ShowProduceOptionsInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var candidates = game.Hexes
            .Where(x => x.Planet?.OwningPlayerId == interactionData.ForGamePlayerId && !x.Planet.IsExhausted)
            .ToList();

        var interactionIds = serviceProvider.AddInteractionsToSetUp(candidates.Select(x => new ProduceInteraction
        {
            Game = game.DocumentId,
            Hex = x.Coordinates,
            EditOriginalMessage = true,
            ForGamePlayerId = player.GamePlayerId
        }));

        var cancelId = serviceProvider.AddInteractionToSetUp(new RepromptInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId
        });

        builder?.AppendContentNewline("Choose a ready planet to produce on:");
        builder?.AppendHexButtons(game, candidates, interactionIds);
        builder?.AppendCancelButton(cancelId);

        return new SpaceWarInteractionOutcome(false);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        ProduceInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Hex);
        if (hex.Planet?.IsExhausted != false)
        {
            throw new Exception();
        }

        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            ProduceOperations.CreateProduceEvent(game, hex.Coordinates),
            new GameEvent_ActionComplete
            {
                ActionType = ActionType.Main
            });
        
        return new SpaceWarInteractionOutcome(true);
    }
}