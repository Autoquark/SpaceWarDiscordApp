using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord.Commands;

public class ProduceActionCommands : IInteractionHandler<ShowProduceOptionsInteraction>,
    IInteractionHandler<ProduceInteraction>
{
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(ShowProduceOptionsInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var builder = new DiscordWebhookBuilder().EnableV2Components();
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
                }, transaction))
        );

        builder.AppendContentNewline("Choose a ready planet to produce on:");
        foreach (var group in candidates.ZipWithIndices().GroupBy(x => x.Item2 / 5))
        {
            builder.AddActionRowComponent(
                group.Select(x => DiscordHelpers.CreateButtonForHex(game, x.Item1, interactionIds[x.Item1])));
        }

        return new SpaceWarInteractionOutcome(false, builder);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(ProduceInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var hex = game.GetHexAt(interactionData.Hex);
        if (hex.Planet?.IsExhausted != false)
        {
            throw new Exception();
        }
        
        var builder = new DiscordWebhookBuilder().EnableV2Components();

        await ProduceOperations.ProduceOnPlanetAsync(builder, game, hex);
        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Main);
        
        return new SpaceWarInteractionOutcome(true, builder);
    }
}