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
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync<TBuilder>(TBuilder builder,
        ShowProduceOptionsInteraction interactionData, Game game, IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var candidates = game.Hexes
            .Where(x => x.Planet?.OwningPlayerId == interactionData.ForGamePlayerId && !x.Planet.IsExhausted)
            .ToList();
        
        var interactionIds = await Program.FirestoreDb.RunTransactionAsync(transaction
            => candidates.ToDictionary(
                x => x,
                x => InteractionsHelper.SetUpInteraction(new ProduceInteraction
                {
                    Game = game.DocumentId,
                    Hex = x.Coordinates,
                    EditOriginalMessage = true,
                    ForGamePlayerId = player.GamePlayerId
                },transaction, serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData.InteractionGroupId))
        );

        builder.AppendContentNewline("Choose a ready planet to produce on:");
        foreach (var group in candidates.ZipWithIndices().GroupBy(x => x.Item2 / 5))
        {
            builder.AddActionRowComponent(
                group.Select(x => DiscordHelpers.CreateButtonForHex(game, x.Item1, interactionIds[x.Item1])));
        }

        return new SpaceWarInteractionOutcome(false, builder);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync<TBuilder>(TBuilder builder,
        ProduceInteraction interactionData, Game game, IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var hex = game.GetHexAt(interactionData.Hex);
        if (hex.Planet?.IsExhausted != false)
        {
            throw new Exception();
        }

        await GameFlowOperations.PushGameEventsAsync(builder, game, serviceProvider, new GameEvent_BeginProduce
        {
            Location = interactionData.Hex
        }, new GameEvent_ActionComplete
        {
            ActionType = ActionType.Main
        });
        //await ProduceOperations.ProduceOnPlanetAsync(builder, game, hex, serviceProvider);
        //await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Main, serviceProvider);
        await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, serviceProvider);
        
        return new SpaceWarInteractionOutcome(true, builder);
    }
}