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
    IInteractionHandler<ProduceInteraction>, IEventResolvedHandler<GameEvent_AlterPlanet>
{
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        ShowProduceOptionsInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        if (game.EventStack.Count > 0)
        {
            builder?.AppendContentNewline("You can't click this right now because the game is waiting on a different decision:");
            await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, serviceProvider);
            return new SpaceWarInteractionOutcome(false);
        }
        
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

    public Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_AlterPlanet gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(gameEvent.Coordinates);

        if (gameEvent.ProductionChange != 0)
        {
            var previous = hex.Planet!.Production;
            hex.Planet.Production += gameEvent.ProductionChange;

            builder?.AppendContentNewline(
                $"The production value of {hex.ToHexNumberWithDieEmoji(game)} has changed by {gameEvent.ProductionChange} ({previous} -> {hex.Planet.Production})");
        }

        if (gameEvent.ScienceChange != 0)
        {
            var previous = hex.Planet!.Science;
            hex.Planet.Science += gameEvent.ScienceChange;

            builder?.AppendContentNewline(
                $"The science value of {hex.ToHexNumberWithDieEmoji(game)} has changed by {gameEvent.ScienceChange} ({previous} -> {hex.Planet.Science})");
        }
        
        if (gameEvent.StarsChange != 0)
        {
            var previous = hex.Planet!.Stars;
            hex.Planet.Stars += gameEvent.StarsChange;

            builder?.AppendContentNewline(
                $"The star value of {hex.ToHexNumberWithDieEmoji(game)} has changed by {gameEvent.StarsChange} ({previous} -> {hex.Planet.Stars})");
        }
        
        return Task.FromResult(builder);
    }
}