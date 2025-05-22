using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord.Commands;

[RequireGameChannel]
public class ProduceActionCommands : IInteractionHandler<ShowProduceOptionsInteraction>,
    IInteractionHandler<ProduceInteraction>
{
    public async Task HandleInteractionAsync(ShowProduceOptionsInteraction interactionData, Game game, InteractionCreatedEventArgs args)
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

        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    public async Task HandleInteractionAsync(ProduceInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var hex = game.GetHexAt(interactionData.Hex);
        if (hex?.Planet?.IsExhausted != false)
        {
            throw new Exception();
        }
        
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        var player = game.GetGamePlayerByGameId(hex.Planet.OwningPlayerId);
        var name = await player.GetNameAsync(false);
        
        hex.Planet.ForcesPresent += hex.Planet.Production;
        hex.Planet.IsExhausted = true;
        player.Science += hex.Planet.Science;
        var producedScience = hex.Planet.Science > 0;

        builder.AppendContentNewline(
            $"{name} is producing on {hex.Coordinates}. Produced {hex.Planet.Production} forces" + (producedScience ? $" and {hex.Planet.Science} science" : ""));
        if (producedScience)
        {
            builder.AppendContentNewline($"{name} now has {player.Science} science");
        }

        await GameFlowOperations.MarkActionTakenForTurn(builder, game);
        
        await Program.FirestoreDb.RunTransactionAsync(transaction =>
        {
            transaction.Set(game);
            return Task.CompletedTask;
        });
        
        await args.Interaction.EditOriginalResponseAsync(builder);
    }
}