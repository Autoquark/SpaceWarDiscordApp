using System.Diagnostics;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Move;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public enum MoveDestinationRestriction
{
    Unrestricted,
    CannotAttack,
    MustAlreadyControl
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T">Type used to uniquely identify interactions to be handled by this handler.
/// The type is not otherwise interacted with and can be any type</typeparam>
public abstract class MovementFlowHandler<T> : IInteractionHandler<BeginPlanningMoveInteraction<T>>,
    IInteractionHandler<SetMoveDestinationInteraction<T>>,
    IInteractionHandler<AddMoveSourceInteraction<T>>,
    IInteractionHandler<SetMovementAmountFromSourceInteraction<T>>,
    IInteractionHandler<PerformPlannedMoveInteraction<T>>
{
    protected MovementFlowHandler(string moveName)
    {
        MoveName = moveName;
    }
    
    protected string MoveName { get; init; }
    
    /// <summary>
    /// Whether movement sources are required to be adjacent to the destination hex.
    /// </summary>
    protected bool RequireAdjacency { get; init; } = true;

    /// <summary>
    /// Whether forces can be moved from multiple sources at the same time.
    /// </summary>
    protected bool AllowManyToOne { get; init; } = true;
    
    /// <summary>
    /// Whether base implementation of PerformMoveAsync should mark action taken for the turn.
    /// </summary>
    protected ActionType ActionType { get; init; } = ActionType.Main;

    /// <summary>
    /// Id of a tech that base implementation of PerformMoveAsync should exhaust.
    /// </summary>
    protected string ExhaustTechId { get; init; } = "";

    /// <summary>
    /// Restriction on destination ownership
    /// </summary>
    protected MoveDestinationRestriction DestinationRestriction { get; init; } = MoveDestinationRestriction.Unrestricted;
    
    /// <summary>
    /// Enters the move planning flow. Displays buttons to select a destination
    /// </summary>
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(BeginPlanningMoveInteraction<T> interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var builder = new DiscordWebhookBuilder().EnableV2Components();
        await BeginPlanningMoveAsync(builder, game, player);
        
        return new SpaceWarInteractionOutcome(false, builder);
    }

    public async Task<TBuilder> BeginPlanningMoveAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        IEnumerable<BoardHex> destinations;
        if (RequireAdjacency)
        {
            var destinationsSet = new HashSet<BoardHex>();
            foreach (var sourceHex in game.Hexes.WhereOwnedBy(player))
            {
                destinationsSet.UnionWith(BoardUtils.GetNeighbouringHexes(game, sourceHex));
            }

            destinations = destinationsSet;
        }
        else
        {
            destinations = game.Hexes;
        }

        // Can only move to hexes with a planet
        destinations = destinations.WhereNonNull(x => x.Planet);

        switch (DestinationRestriction)
        {
            case MoveDestinationRestriction.Unrestricted:
                break;
            case MoveDestinationRestriction.CannotAttack:
                destinations = destinations.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId || x.IsNeutral);
                break;
            case MoveDestinationRestriction.MustAlreadyControl:
                destinations = destinations.WhereOwnedBy(player);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        var playerName = await player.GetNameAsync(true);
        builder.AppendContentNewline($"{MoveName}: {playerName}, choose a {"destination".DiscordBold()} for your move: ")
            .AllowMentions(player);

        destinations = destinations.ToList();

        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(destinations.Select(x =>
            new SetMoveDestinationInteraction<T>
            {
                Game = game.DocumentId,
                Destination = x.Coordinates,
                ForGamePlayerId = player.GamePlayerId,
                EditOriginalMessage = true
            }));
        
        return builder.AppendHexButtons(game, destinations, interactionIds);
    }

    /// <summary>
    /// Called when a move destination is selected. Displays buttons to add a source, or skips straight to amount if there is only one possible source
    /// </summary>
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(SetMoveDestinationInteraction<T> interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var destination = game.GetHexAt(interactionData.Destination);
        if (destination == null)
        {
            throw new Exception();
        }

        game.GetGamePlayerByGameId(interactionData.ForGamePlayerId)
            .PlannedMove = new PlannedMove
        {
            Destination = interactionData.Destination
        };

        var player = game.GetGamePlayerForInteraction(interactionData);
        var sources = GetAllowedMoveSources(game, player, destination);

        if (sources.Count == 0)
        {
            throw new Exception();
        }

        var messageBuilder = new DiscordWebhookBuilder().EnableV2Components();
        MovementOperations.ShowPlannedMove(messageBuilder, player);

        if (sources.Count == 1)
        {
            // Only one planet we can move from, skip straight to specifying amount
            await ShowSpecifyMovementAmountButtonsAsync(messageBuilder, game, player,
                sources.Single(), destination);
            return new SpaceWarInteractionOutcome(true, messageBuilder);
        }

        var interactionsToSetUp = await ShowSpecifyMovementSourceButtonsAsync(messageBuilder,
            game,
            player, 
            destination);
        
        await InteractionsHelper.SetUpInteractionsAsync(interactionsToSetUp);

        return new SpaceWarInteractionOutcome(true, messageBuilder);
    }

    /// <summary>
    /// Called when a movement source is selected to add. Shows buttons to select amount of forces to move
    /// </summary>
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(AddMoveSourceInteraction<T> interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);

        if (player.PlannedMove == null)
        {
            throw new Exception();
        }
        
        var builder = new DiscordWebhookBuilder().EnableV2Components();

        await ShowSpecifyMovementAmountButtonsAsync(
            builder,
            game,
            player,
            game.GetHexAt(interactionData.Source),
            game.GetHexAt(player.PlannedMove.Destination));
        
        return new SpaceWarInteractionOutcome(false, builder);
    }

    /// <summary>
    /// Called when a movement amount from a source is selected.
    /// </summary>
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(SetMovementAmountFromSourceInteraction<T> interactionData, Game game,
        InteractionCreatedEventArgs args)
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);

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
        
        var interactions = new List<InteractionData>();
        // If this was the only place we could move from, and a nonzero amount was specified, perform the move now
        var sources = GetAllowedMoveSources(game, player, destinationHex);
        if ((sources.Count == 1 || !AllowManyToOne) && entry != null)
        {
            await PerformMoveAsync(builder, game, player);
        }
        else
        {
            // Otherwise, go back to showing possible sources
            MovementOperations.ShowPlannedMove(builder, player);
            interactions.AddRange(await ShowSpecifyMovementSourceButtonsAsync(builder, game, player, destinationHex));
            var confirmInteraction = new PerformPlannedMoveInteraction<T>()
            {
                Game = game.DocumentId,
                ForGamePlayerId = player.GamePlayerId,
            };
            interactions.Add(confirmInteraction);
            builder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Success,
                confirmInteraction.InteractionId, "Confirm move"));
        }
        
        await Program.FirestoreDb.RunTransactionAsync(transaction =>
        {
            transaction.Set(game);
            InteractionsHelper.SetUpInteractions(interactions, transaction);
        });
        
        return new SpaceWarInteractionOutcome(false, builder);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(PerformPlannedMoveInteraction<T> interactionData, Game game, InteractionCreatedEventArgs args)
    {
        var builder = new DiscordFollowupMessageBuilder().EnableV2Components();
        await PerformMoveAsync(builder, game, game.GetGamePlayerForInteraction(interactionData));
        
        await Program.FirestoreDb.RunTransactionAsync(transaction => transaction.Set(game));

        return new SpaceWarInteractionOutcome(true, builder, true);
    }

    protected async Task<TBuilder> ShowSpecifyMovementAmountButtonsAsync<TBuilder>(TBuilder builder, Game game,
        GamePlayer player, BoardHex source, BoardHex destination) 
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        builder.AppendContentNewline($"{MoveName}: How many forces do you wish to move from {source.Coordinates} to {destination.Coordinates}?");

        if (source.Planet == null)
        {
            throw new Exception();
        }

        var interactionIds = await Program.FirestoreDb.RunTransactionAsync(transaction
            => Enumerable.Range(0, source.ForcesPresent + 1).Select(x => InteractionsHelper.SetUpInteraction(
                    new SetMovementAmountFromSourceInteraction<T>
                    {
                        Amount = x,
                        From = source.Coordinates,
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                        EditOriginalMessage = true
                    }, transaction))
                .ToList());

        builder.AppendButtonRows(Enumerable.Range(0, source.ForcesPresent + 1).Select(x =>
            new DiscordButtonComponent(DiscordButtonStyle.Primary, interactionIds[x], x.ToString())));

        return builder;
    }

    protected async Task<List<AddMoveSourceInteraction<T>>> ShowSpecifyMovementSourceButtonsAsync<TBuilder>(
        TBuilder builder, Game game, GamePlayer player, BoardHex destination)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var sources = RequireAdjacency ? BoardUtils.GetStandardMoveSources(game, destination, player).ToList() : game.Hexes.WhereOwnedBy(player).ToList();
        sources.Remove(destination);
        if (sources.Count == 0)
        {
            throw new Exception();
        }
        
        var name = await player.GetNameAsync(true);
        builder.AppendContentNewline($"{MoveName}: {name}, choose a planet to move forces from: ")
            .AllowMentions(player);
        builder.AddActionRowComponent();

        var interactions = sources.Select(x => 
            new AddMoveSourceInteraction<T>
            {
                Game = game.DocumentId,
                Source = x.Coordinates,
                ForGamePlayerId = player.GamePlayerId,
                EditOriginalMessage = true
            })
            .ToList();

        builder.AppendHexButtons(game, sources, interactions.Select(x => x.InteractionId));
        
        return interactions;
    }

    protected async Task<TBuilder> PerformMoveAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        await MovementOperations.PerformPlannedMoveAsync(builder, game, player);

        if (!string.IsNullOrEmpty(ExhaustTechId))
        {
            player.GetPlayerTechById(ExhaustTechId).IsExhausted = true;
        }
        
        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType);
        
        // Prompt player to choose another action, if possible. If MarkActionTakenForTurn already moved the turn on and 
        // printed the turn message for the new player, this will bail out on printing it again
        await GameFlowOperations.ShowSelectActionMessageAsync(builder, game);
        
        return builder;
    }
    
    private List<BoardHex> GetAllowedMoveSources(Game game, GamePlayer player, BoardHex destination)
        => RequireAdjacency ? BoardUtils.GetStandardMoveSources(game, destination, player).ToList() : game.Hexes.WhereOwnedBy(player).ToList();
}