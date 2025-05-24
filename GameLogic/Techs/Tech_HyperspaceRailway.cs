using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData.Tech.HyperspaceRailway;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_HyperspaceRailway : Tech, IInteractionHandler<SubmitHyperspaceRailwaySourceInteraction>,
    IInteractionHandler<SubmitHyperspaceRailwayDestinationInteraction>,
    IInteractionHandler<SubmitHyperspaceRailwayAmountInteraction>
{
    public Tech_HyperspaceRailway() : base("hyperspaceRailway",
        "Hyperspace Railway",
        "Action: Move any number of forces from one planet you control to another planet you control.",
        "The 7.15 service to Alpha Centauri has been delayed due to leaves on the toroidal manifold.")
    {
        HasSimpleAction = true;
    }

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
    {
        var sources = game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId)
            .ToList();
        
        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(sources.Select(x =>
            new SubmitHyperspaceRailwaySourceInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Source = x.Coordinates
            }));
        
        builder.AppendContentNewline("Choose a source planet:");
        
        return builder.AppendHexButtons(game, sources, interactionIds);
    }
    
    public async Task HandleInteractionAsync(SubmitHyperspaceRailwaySourceInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        var player = game.GetGamePlayerForInteraction(interactionData);
        var destinations = game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId && x.Coordinates != interactionData.Source)
            .ToList();
        
        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(destinations.Select(x =>
            new SubmitHyperspaceRailwayDestinationInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Source = interactionData.Source,
                Destination = x.Coordinates
            }));
        
        builder.AppendContentNewline("Choose a destination planet:");
        
        builder.AppendHexButtons(game, destinations, interactionIds);
        
        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    public async Task HandleInteractionAsync(SubmitHyperspaceRailwayDestinationInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        var source = game.GetHexAt(interactionData.Source);
        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(Enumerable.Range(0, source.Planet!.ForcesPresent + 1).Select(x =>
            new SubmitHyperspaceRailwayAmountInteraction()
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Source = interactionData.Source,
                Destination = interactionData.Destination,
                Amount = x
            }));
        
        var builder = new DiscordWebhookBuilder().EnableV2Components()
            .AppendContentNewline("Choose amount of forces to move:")
            .AppendButtonRows(interactionIds.ZipWithIndices().Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, x.item, x.index.ToString())));
        
        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    public async Task HandleInteractionAsync(SubmitHyperspaceRailwayAmountInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        
        player.GetPlayerTechById(Id).IsExhausted = true;

        await MovementOperations.ResolveMoveAsync(builder, game, player, new PlannedMove
        {
            Destination = interactionData.Destination,
            Sources =
            [
                new SourceAndAmount
                {
                    Amount = interactionData.Amount,
                    Source = interactionData.Source
                }
            ]
        });
        
        await GameFlowOperations.MarkActionTakenForTurn(builder, game);
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));
        
        await args.Interaction.EditOriginalResponseAsync(builder);
    }
}