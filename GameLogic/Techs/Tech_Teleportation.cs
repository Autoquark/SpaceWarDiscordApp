using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData.Tech.Teleportation;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_Teleportation : Tech,
    IInteractionHandler<SubmitTeleportationSourceInteraction>,
    IInteractionHandler<SubmitTeleportationDestinationInteraction>,
    IInteractionHandler<SubmitTeleportationAmountInteraction>
{
    public Tech_Teleportation() : base("teleportation",
        "Teleportation",
        "Action: Move any number of forces from one planet you control to any other planet.",
        "Teleportation isn't an exact science. As long as total limbs in = total limbs out we consider it a success")
    {
        HasSimpleAction = true;
    }

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
    {
        var sources = game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId).ToList();

        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(sources.Select(x =>
            new SubmitTeleportationSourceInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Source = x.Coordinates
            }));
        
        builder.AppendContentNewline($"{DisplayName}: Choose a source planet:");
        return builder.AppendHexButtons(game, sources, interactionIds);
    }
    
    public async Task HandleInteractionAsync(SubmitTeleportationSourceInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        var player = game.GetGamePlayerForInteraction(interactionData);
        var destinations = game.Hexes.Where(x => x.Planet != null && x.Coordinates != interactionData.Source)
            .ToList();
        
        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(destinations.Select(x =>
            new SubmitTeleportationDestinationInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Source = interactionData.Source,
                Destination = x.Coordinates
            }));
        
        builder.AppendContentNewline($"{DisplayName}: Choose a destination planet:");
        
        builder.AppendHexButtons(game, destinations, interactionIds);
        
        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    public async Task HandleInteractionAsync(SubmitTeleportationDestinationInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        var source = game.GetHexAt(interactionData.Source);
        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(Enumerable.Range(0, source.Planet!.ForcesPresent + 1).Select(x =>
            new SubmitTeleportationAmountInteraction()
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Source = interactionData.Source,
                Destination = interactionData.Destination,
                Amount = x
            }));
        
        var builder = new DiscordWebhookBuilder().EnableV2Components()
            .AppendContentNewline($"{DisplayName}: Choose amount of forces to move:")
            .AppendButtonRows(interactionIds.ZipWithIndices().Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, x.item, x.index.ToString())));
        
        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    public async Task HandleInteractionAsync(SubmitTeleportationAmountInteraction interactionData, Game game,
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