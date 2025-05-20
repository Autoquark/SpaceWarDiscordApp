using System.Diagnostics;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Move;
using SpaceWarDiscordApp.Discord.ContextChecks;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.Discord.Commands;

[RequireGameChannel]
public class MoveActionCommands : IInteractionHandler<ShowMoveOptionsInteraction>,
    IInteractionHandler<BeginPlanningMoveActionInteraction>,
    IInteractionHandler<ShowSpecifyMovementAmountFromPlanetInteraction>,
    IInteractionHandler<SubmitSpecifyMovementAmountFromPlanetInteraction>,
    IInteractionHandler<PerformPlannedMoveInteraction>
{
    public async Task HandleInteractionAsync(ShowMoveOptionsInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        ISet<BoardHex> destinations = new HashSet<BoardHex>();
        foreach (var fromHex in game.Hexes.Where(x => x.Planet?.OwningPlayerId == interactionData.ForGamePlayerId))
        {
            destinations.UnionWith(BoardUtils.GetNeighbouringHexes(game, fromHex));
        }

        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var playerName = await player.GetNameAsync(true);
        var messageBuilder = new DiscordWebhookBuilder()
            .EnableV2Components()
            .AppendContentNewline($"{playerName}, you may move to the following hexes: ");

        IDictionary<BoardHex, string> interactionIds = await Program.FirestoreDb.RunTransactionAsync(transaction
            => destinations.ToDictionary(
                x => x,
                x => InteractionsHelper.SetUpInteraction(new BeginPlanningMoveActionInteraction
                {
                    Game = game.DocumentId,
                    Destination = x.Coordinates,
                    AllowedGamePlayerIds = player.IsDummyPlayer ? [] : [player.GamePlayerId],
                    MovingGamePlayerId = player.GamePlayerId,
                    EditOriginalMessage = true
                }, transaction))
            );
        
        foreach(var group in destinations.ZipWithIndices().GroupBy(x => x.Item2 / 5))
        {
            messageBuilder.AddActionRowComponent(
                group.Select(x => DiscordHelpers.CreateButtonForHex(game, x.Item1, interactionIds[x.Item1])));
        }
        
        await args.Interaction.EditOriginalResponseAsync(messageBuilder);
    }
    
    public async Task HandleInteractionAsync(BeginPlanningMoveActionInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var destination = game.GetHexAt(interactionData.Destination);
        if (destination == null)
        {
            throw new Exception();
        }

        game.GetGamePlayerByGameId(interactionData.MovingGamePlayerId)
            .PlannedMove = new PlannedMove
            {
                Destination = interactionData.Destination
            };

        var player = game.GetGamePlayerByGameId(interactionData.MovingGamePlayerId);
        var sources = BoardUtils.GetStandardMoveSources(game, destination, player);
        var playerName = await player.GetNameAsync(true);

        if (!sources.Any())
        {
            throw new Exception();
        }

        var messageBuilder = new DiscordWebhookBuilder().EnableV2Components();
        MovementOperations.ShowPlannedMove(messageBuilder, player);
        
        if (sources.Count == 1)
        {
            await Program.FirestoreDb.RunTransactionAsync(transaction =>
            {
                transaction.Set(game);
                return Task.CompletedTask;
            });
            // Only one planet we can move from, skip straight to specifying amount
            await MovementOperations.ShowSpecifyMovementAmountButtonsAsync(messageBuilder, game, player, sources.Single(), destination);
        }
        else
        {
            var interactionsToSetUp = await MovementOperations.ShowSpecifyMovementSourceButtonsAsync(messageBuilder, game, player, destination);
            await Program.FirestoreDb.RunTransactionAsync(transaction =>
            {
                // Save updated planned move data
                transaction.Set(game);

                InteractionsHelper.SetUpInteractions(interactionsToSetUp, transaction);

                return Task.CompletedTask;
            });
        }
        
        await args.Interaction.EditOriginalResponseAsync(messageBuilder);
    }

    public async Task HandleInteractionAsync(ShowSpecifyMovementAmountFromPlanetInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerByGameId(interactionData.MovingPlayerId);

        if (player.PlannedMove == null)
        {
            throw new Exception();
        }
        
        await args.Interaction.EditOriginalResponseAsync(
            await MovementOperations.ShowSpecifyMovementAmountButtonsAsync(
                new DiscordWebhookBuilder().EnableV2Components(),
                game,
                player,
                game.GetHexAt(interactionData.Source),
                game.GetHexAt(player.PlannedMove.Destination))
            );
    }

    public async Task HandleInteractionAsync(SubmitSpecifyMovementAmountFromPlanetInteraction interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerByGameId(interactionData.MovingPlayerId);

        var entry = player.PlannedMove!.Sources.FirstOrDefault(x => x.Source == interactionData.From);
        if (entry == null && interactionData.Amount > 0)
        {
            entry = new SourceAndAmount
            {
                Source = interactionData.From,
                Amount = interactionData.Amount
            };
            player.PlannedMove.Sources.Add(entry);
        }
        else if(interactionData.Amount > 0)
        {
            entry!.Amount = interactionData.Amount;
        }
        else if(entry != null)
        {
            player.PlannedMove.Sources.Remove(entry);
        }

        var destinationHex = game.GetHexAt(player.PlannedMove.Destination);
        Debug.Assert(destinationHex != null);
        
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        
        List<InteractionData> interactions = new List<InteractionData>();
        // If this was the only place we could move from, and a nonzero amount was specified, perform the move now
        var sources = BoardUtils.GetStandardMoveSources(game, destinationHex, player);
        if (sources.Count == 1 && entry != null)
        {
            Debug.Assert(entry.Source == sources.First().Coordinates);
            await MovementOperations.PerformPlannedMoveAsync(builder, game, player);
        }
        else
        {
            // Otherwise, go back to showing possible sources
            MovementOperations.ShowPlannedMove(builder, player);
            interactions.AddRange(await MovementOperations.ShowSpecifyMovementSourceButtonsAsync(builder, game, player, destinationHex));
            var confirmInteraction = new PerformPlannedMoveInteraction()
            {
                Game = game.DocumentId,
                PlayerId = player.GamePlayerId,
                AllowedGamePlayerIds = player.IsDummyPlayer ? [] : [player.GamePlayerId]
            };
            interactions.Add(confirmInteraction);
            builder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Success,
                confirmInteraction.InteractionId, "Confirm move"));
        }
        
        await Program.FirestoreDb.RunTransactionAsync(transaction =>
        {
            transaction.Set(game);
            InteractionsHelper.SetUpInteractions(interactions, transaction);
            return Task.CompletedTask;
        });
        
        await args.Interaction.EditOriginalResponseAsync(builder);
    }

    public async Task HandleInteractionAsync(PerformPlannedMoveInteraction interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var builder = new DiscordFollowupMessageBuilder().EnableV2Components();
        await MovementOperations.PerformPlannedMoveAsync(builder, game, game.GetGamePlayerByGameId(interactionData.PlayerId));
        await args.Interaction.DeleteOriginalResponseAsync();
        await args.Interaction.CreateFollowupMessageAsync(builder);
    }
}