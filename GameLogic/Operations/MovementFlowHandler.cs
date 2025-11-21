using System.Diagnostics;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
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
    IInteractionHandler<PerformPlannedMoveInteraction<T>>,
    IEventResolvedHandler<GameEvent_MovementFlowComplete<T>>
{
    protected MovementFlowHandler(Tech? tech)
    {
        Tech = tech;
    }

    protected Tech? Tech;
    
    /// <summary>
    /// Whether movement sources are required to be adjacent to the destination hex.
    /// </summary>
    protected bool RequireAdjacency { get; init; } = true;

    /// <summary>
    /// Whether forces can be moved from multiple sources at the same time.
    /// </summary>
    protected bool AllowManyToOne { get; init; } = true;
    
    /// <summary>
    /// Whether base implementation of PerformMoveAsync should mark main action taken for the turn.
    /// </summary>
    protected ActionType? ActionType { get; init; } = GameLogic.ActionType.Main;

    /// <summary>
    /// Id of a tech that base implementation of PerformMoveAsync should exhaust.
    /// </summary>
    protected string ExhaustTechId { get; init; } = "";
    
    /// <summary>
    /// Id of a tech that base implementation of PerformMoveAsync should mark as used this turn.
    /// </summary>
    protected string MarkUsedTechId { get; init; } = "";

    /// <summary>
    /// Restriction on destination ownership
    /// </summary>
    protected MoveDestinationRestriction DestinationRestriction { get; init; } = MoveDestinationRestriction.Unrestricted;
    
    /// <summary>
    /// Maximum amount of forces that may be moved from each source. This can also be limited dynamically when calling
    /// BeginPlanningMoveAsync
    /// </summary>
    protected int StaticMaxAmountPerSource { get; init; } = 99;
    
    /// <summary>
    /// Minimum amount of forces that may be moved from each source. This can also be limited dynamically when calling
    /// BeginPlanningMoveAsync
    /// </summary>
    protected int StaticMinAmountPerSource { get; init; } = 0;
    
    /// <summary>
    /// Whether to call GameFlowOperations.ContinueResolvingEventStackAsync after the move is complete
    /// </summary>
    protected bool ContinueResolvingStackAfterMove { get; init; } = false;

    /// <summary>
    /// If true, must move all forces from each source
    /// </summary>
    protected bool MustMoveAll { get; init; } = false;
    
    /// <summary>
    /// Enters the move planning flow. Displays buttons to select a destination
    /// </summary>
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        BeginPlanningMoveInteraction<T> interactionData,
        Game game,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        await BeginPlanningMoveAsync(builder, game, player, serviceProvider);
        
        return new SpaceWarInteractionOutcome(false);
    }

    public async Task<DiscordMultiMessageBuilder> BeginPlanningMoveAsync(DiscordMultiMessageBuilder builder,
        Game game, GamePlayer player,
        IServiceProvider serviceProvider,
        HexCoordinates? fixedSource = null,
        HexCoordinates? fixedDestination = null,
        int? dynamicMinAmountPerSource = null,
        int? dynamicMaxAmountPerSource = null,
        string? triggerToMarkResolved = null)
    {
        if (fixedDestination.HasValue)
        {
            player.PlannedMove = new PlannedMove
            {
                Destination = fixedDestination.Value
            };

            if (fixedSource.HasValue)
            {
                var amount = await GetOrPromptMovementAmountAsync(builder,
                    game,
                    player,
                    game.GetHexAt(fixedSource.Value),
                    game.GetHexAt(fixedDestination.Value),
                    dynamicMinAmountPerSource ?? StaticMinAmountPerSource,
                    dynamicMaxAmountPerSource ?? StaticMaxAmountPerSource,
                    isOnlySource: true,
                    triggerToMarkResolved,
                    serviceProvider);

                if (amount.HasValue)
                {
                    player.PlannedMove.Sources.Add(new SourceAndAmount
                    {
                        Source = fixedSource.Value,
                        Amount = amount.Value
                    });
                    await PerformMoveAsync(builder, game, player, triggerToMarkResolved, serviceProvider);
                    return builder;
                }
            }
            else
            {
                await ShowSpecifyMovementSourceButtonsAsync(builder,
                    game,
                    serviceProvider,
                    player,
                    game.GetHexAt(fixedDestination.Value),
                    dynamicMinAmountPerSource ?? StaticMinAmountPerSource,
                    dynamicMaxAmountPerSource ?? StaticMaxAmountPerSource,
                    triggerToMarkResolved);
                
                return builder;
            }
        }
        
        IEnumerable<BoardHex> destinations;
        if (RequireAdjacency)
        {
            var destinationsSet = new HashSet<BoardHex>();
            var allowedSources = fixedSource.HasValue
                ? [game.GetHexAt(fixedSource.Value)]
                : GetAllowedMoveSources(game, player, null);
            foreach (var sourceHex in allowedSources)
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
        builder.AppendContentNewline($"{GetMoveName()}: {playerName}, choose a {"destination".DiscordBold()} for your move: ")
            .WithAllowedMentions(player);

        destinations = destinations.ToList();

        var interactionIds = serviceProvider.AddInteractionsToSetUp(destinations.Select(x =>
            new SetMoveDestinationInteraction<T>
            {
                Game = game.DocumentId,
                Destination = x.Coordinates,
                ForGamePlayerId = player.GamePlayerId,
                EditOriginalMessage = true,
                FixedSource = fixedSource,
                MaxAmountPerSource = dynamicMaxAmountPerSource,
                MinAmountPerSource = dynamicMinAmountPerSource,
                TriggerToMarkResolvedId = triggerToMarkResolved
            }));
        
        return builder.AppendHexButtons(game, destinations, interactionIds);
    }

    /// <summary>
    /// Called when a move destination is selected. Displays buttons to add a source, or skips straight to amount if there is only one possible source
    /// </summary>
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        SetMoveDestinationInteraction<T> interactionData,
        Game game,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var destination = game.GetHexAt(interactionData.Destination);
        if (destination == null)
        {
            throw new Exception();
        }

        var player = game.GetGamePlayerForInteraction(interactionData);
        player.PlannedMove = new PlannedMove
        {
            Destination = interactionData.Destination
        };
        
        var sources = interactionData.FixedSource.HasValue
            ? [game.GetHexAt(interactionData.FixedSource.Value)]
            : GetAllowedMoveSources(game, player, destination);

        if (sources.Count == 0)
        {
            throw new Exception();
        }
        
        if (sources.Count == 1)
        {
            var onlySource = sources.Single();
            // Only one planet we can move from, skip straight to specifying amount
            var amount = await GetOrPromptMovementAmountAsync(builder,
                game,
                player,
                sources.Single(),
                destination,
                interactionData.MinAmountPerSource ?? StaticMinAmountPerSource,
                interactionData.MaxAmountPerSource ?? StaticMaxAmountPerSource,
                isOnlySource: true,
                interactionData.TriggerToMarkResolvedId,
                serviceProvider);

            // If the amount can be automatically determined, the move is now fully specified
            if (amount != null)
            {
                player.PlannedMove.Sources = [new SourceAndAmount { Source = onlySource.Coordinates, Amount = amount.Value }];
                await PerformMoveAsync(builder, game, player, interactionData.TriggerToMarkResolvedId, serviceProvider);
            }
            
            return new SpaceWarInteractionOutcome(true);
        }

        // Let player specify a source
        await ShowSpecifyMovementSourceButtonsAsync(builder,
            game,
            serviceProvider,
            player, 
            destination,
            interactionData.MinAmountPerSource ?? StaticMinAmountPerSource,
            interactionData.MaxAmountPerSource ?? StaticMaxAmountPerSource,
            interactionData.TriggerToMarkResolvedId);

        return new SpaceWarInteractionOutcome(true);
    }

    /// <summary>
    /// Called when a movement source is selected to add. Shows buttons to select amount of forces to move
    /// </summary>
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        AddMoveSourceInteraction<T> interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);

        if (player.PlannedMove == null)
        {
            throw new Exception();
        }

        // If we must move all forces:
        // If player has already specified this source, they can choose it again to have the option of cancelling
        // moving forces from it.
        // Otherwise just add all forces to the planned move and go back to source selection
        if (MustMoveAll && player.PlannedMove.Sources.All(x => x.Source != interactionData.Source))
        {
            player.PlannedMove.Sources.Add(new SourceAndAmount
            {
                Source = interactionData.Source, Amount = game.GetHexAt(interactionData.Source).Planet!.ForcesPresent
            });

            var sources = GetAllowedMoveSources(game, player, game.GetHexAt(player.PlannedMove.Destination));
            // If there's no possibility of wanting to add another source, perform the move now
            if (!AllowManyToOne || sources.Count == 1)
            {
                await PerformMoveAsync(builder, game, player, interactionData.TriggerToMarkResolvedId, serviceProvider);
                return new SpaceWarInteractionOutcome(true);
            }

            await ShowSpecifyMovementSourceButtonsAsync(builder,
                game,
                serviceProvider,
                player,
                game.GetHexAt(player.PlannedMove.Destination),
                interactionData.MinAmountPerSource ?? StaticMinAmountPerSource,
                interactionData.MaxAmountPerSource ?? StaticMaxAmountPerSource,
                interactionData.TriggerToMarkResolvedId);
            
            ShowConfirmMoveButton(builder, game, serviceProvider, player, interactionData.TriggerToMarkResolvedId);
            
            return new SpaceWarInteractionOutcome(true);
        }

        var amount = await GetOrPromptMovementAmountAsync(
            builder,
            game,
            player,
            game.GetHexAt(interactionData.Source),
            game.GetHexAt(player.PlannedMove.Destination),
            interactionData.MinAmountPerSource ?? StaticMinAmountPerSource,
            interactionData.MaxAmountPerSource ?? StaticMaxAmountPerSource,
            false,
            interactionData.TriggerToMarkResolvedId,
            serviceProvider);

        if (amount.HasValue)
        {
            player.PlannedMove.Sources.Add(new SourceAndAmount
            {
                Source = interactionData.Source,
                Amount = amount.Value
            });

            // If there are any other possible sources, go back to source selection
            if (AllowManyToOne && GetAllowedMoveSources(game, player, game.GetHexAt(player.PlannedMove.Destination))
                .ExceptBy(player.PlannedMove.Sources.Select(x => x.Source), x => x.Coordinates).Any())
            {
                // There must be other possible sources, or we would not be specifying source via buttons, so go back to
                // source selection
                await ShowSpecifyMovementSourceButtonsAsync(builder,
                    game,
                    serviceProvider,
                    player,
                    game.GetHexAt(player.PlannedMove.Destination),
                    interactionData.MinAmountPerSource ?? StaticMinAmountPerSource,
                    interactionData.MaxAmountPerSource ?? StaticMaxAmountPerSource,
                    interactionData.TriggerToMarkResolvedId);
                
                ShowConfirmMoveButton(builder, game, serviceProvider, player, interactionData.TriggerToMarkResolvedId);
            }
            else
            {
                await PerformMoveAsync(builder, game, player, interactionData.TriggerToMarkResolvedId, serviceProvider);
            }
            
            // Require save because of planned move data change
            return new SpaceWarInteractionOutcome(true);
        }
        
        return new SpaceWarInteractionOutcome(false);
    }

    /// <summary>
    /// Called when a movement amount from a source is selected.
    /// </summary>
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        SetMovementAmountFromSourceInteraction<T> interactionData,
        Game game,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
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
        
        var interactions = new List<InteractionData>();
        // If this was the only place we could move from, and a nonzero amount was specified, perform the move now
        var sources = GetAllowedMoveSources(game, player, destinationHex);
        if ((sources.Count == 1 || !AllowManyToOne || interactionData.IsFixedSource) && entry != null)
        {
            await PerformMoveAsync(builder, game, player, interactionData.TriggerToMarkResolvedId, serviceProvider);
        }
        else
        {
            // Otherwise, go back to showing possible sources
            await ShowSpecifyMovementSourceButtonsAsync(builder,
                game,
                serviceProvider,
                player,
                destinationHex,
                interactionData.MinAmountPerSource ?? StaticMinAmountPerSource,
                interactionData.MaxAmountPerSource ?? StaticMaxAmountPerSource,
                interactionData.TriggerToMarkResolvedId);
            
            ShowConfirmMoveButton(builder, game, serviceProvider, player, interactionData.TriggerToMarkResolvedId);
        }
        
        return new SpaceWarInteractionOutcome(true);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        PerformPlannedMoveInteraction<T> interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        await PerformMoveAsync(builder, game, game.GetGamePlayerForInteraction(interactionData), interactionData.TriggerToMarkResolvedId, serviceProvider);

        return new SpaceWarInteractionOutcome(true);
    }

    protected async Task<int?> GetOrPromptMovementAmountAsync(DiscordMultiMessageBuilder builder,
        Game game,
        GamePlayer player,
        BoardHex source,
        BoardHex destination,
        int dynamicMinAmountPerSource,
        int dynamicMaxAmountPerSource,
        bool isOnlySource,
        string? triggerToMarkResolvedId,
        IServiceProvider serviceProvider) 
    {
        if (source.Planet == null)
        {
            throw new Exception();
        }
        
        var max = Math.Min(source.Planet.ForcesPresent, dynamicMaxAmountPerSource);
        
        var options = MustMoveAll ? [0, source.Planet.ForcesPresent] : Enumerable.Range(dynamicMinAmountPerSource, max + 1 - dynamicMinAmountPerSource).ToArray();
        var existing = player.PlannedMove?.Sources.FirstOrDefault(x => x.Source == source.Coordinates);

        // If the options are (zero and) one value and we don't already have an amount for this source, assume the (non-zero) value
        if ((options.Length <= 2 && existing == null) || options.Length == 1)
        {
            return options.Last();
        }

        var interactionIds = serviceProvider.AddInteractionsToSetUp(
            options.Select(x => new SetMovementAmountFromSourceInteraction<T>
                    {
                        Amount = x,
                        From = source.Coordinates,
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                        EditOriginalMessage = true,
                        MinAmountPerSource = dynamicMinAmountPerSource,
                        MaxAmountPerSource = dynamicMaxAmountPerSource,
                        TriggerToMarkResolvedId = triggerToMarkResolvedId,
                        IsFixedSource = isOnlySource,
                    })
                ).ToList();

        await MovementOperations.ShowPlannedMoveAsync(builder, game, player);
        builder.AppendContentNewline($"{GetMoveName()}: How many forces do you wish to move from {source.Coordinates} to {destination.Coordinates}?");
        builder.AppendButtonRows(Enumerable.Range(0, max + 1).Select(x =>
            new DiscordButtonComponent(DiscordButtonStyle.Primary, interactionIds[x], x.ToString())));

        return null;
    }

    protected async Task<DiscordMultiMessageBuilder> ShowSpecifyMovementSourceButtonsAsync(
        DiscordMultiMessageBuilder builder,
        Game game,
        IServiceProvider serviceProvider,
        GamePlayer player,
        BoardHex destination,
        int dynamicMinAmountPerSource,
        int dynamicMaxAmountPerSource,
        string? triggerToMarkResolvedId)
    {
        var sources = GetAllowedMoveSources(game, player, destination);
        sources.Remove(destination);
        if (sources.Count == 0)
        {
            throw new Exception();
        }
        
        var name = await player.GetNameAsync(true);
        await MovementOperations.ShowPlannedMoveAsync(builder, game, player);
        builder.AppendContentNewline($"{GetMoveName()}: {name}, choose a planet to move forces from: ")
            .WithAllowedMentions(player);
        builder.AddActionRowComponent();

        var interactions = serviceProvider.AddInteractionsToSetUp(sources.Select(x =>
            new AddMoveSourceInteraction<T>
            {
                Game = game.DocumentId,
                Source = x.Coordinates,
                ForGamePlayerId = player.GamePlayerId,
                EditOriginalMessage = true,
                MaxAmountPerSource = dynamicMaxAmountPerSource,
                MinAmountPerSource = dynamicMinAmountPerSource,
                TriggerToMarkResolvedId = triggerToMarkResolvedId
            }));
        
        builder.AppendButtonRows(sources.Zip(interactions).Select(x =>
        {
            var hex = x.First;
            var interaction = x.Second;

            var emoji = hex.GetDieEmoji(game);
            var existingSourceData = player.PlannedMove?.Sources.FirstOrDefault(y => y.Source == hex.Coordinates);

            return new DiscordButtonComponent(
                existingSourceData != null
                    ? DiscordButtonStyle.Secondary
                    : DiscordButtonStyle.Primary,
                interaction,
                hex.Coordinates + (existingSourceData != null ? $" [moving {existingSourceData.Amount}]" : ""),
                emoji: (emoji! == null! ? null : new DiscordComponentEmoji(emoji))!
            );
        }));

        return builder;
    }

    protected DiscordMultiMessageBuilder ShowConfirmMoveButton(DiscordMultiMessageBuilder builder,
        Game game, IServiceProvider serviceProvider, GamePlayer player, string? triggerToMarkResolvedId)
    {
        var confirmInteractionId = serviceProvider.AddInteractionToSetUp(new PerformPlannedMoveInteraction<T>()
        {
            Game = game.DocumentId,
            ForGamePlayerId = player.GamePlayerId,
            TriggerToMarkResolvedId = triggerToMarkResolvedId,
        });
        builder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Success,
            confirmInteractionId, "Confirm move"));

        return builder;
    }

    protected async Task<DiscordMultiMessageBuilder> PerformMoveAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player, string? triggerToMarkResolved, IServiceProvider serviceProvider) =>
        (await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider, 
            (await MovementOperations.GetResolveMoveEventsAsync(builder, game, player, player.PlannedMove!, serviceProvider, Tech))
            .Append(new GameEvent_MovementFlowComplete<T>
            {
                PlayerGameId = player.GamePlayerId,
                TriggerToMarkResolved = triggerToMarkResolved,
                Sources = player.PlannedMove!.Sources.ToList(),
                Destination = player.PlannedMove!.Destination
            })))!;

    protected virtual List<BoardHex> GetAllowedMoveSources(Game game, GamePlayer player, BoardHex? destination)
        => ((RequireAdjacency && destination != null)
                ? BoardUtils.GetStandardMoveSources(game, destination, player)
                : game.Hexes.WhereOwnedBy(player))
            .Except(destination)!.ToList<BoardHex>();

    public virtual async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_MovementFlowComplete<T> gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerByGameId(gameEvent.PlayerGameId);
        if (!string.IsNullOrEmpty(ExhaustTechId))
        {
            player.GetPlayerTechById(ExhaustTechId).IsExhausted = true;
        }

        if (!string.IsNullOrEmpty(MarkUsedTechId))
        {
            player.GetPlayerTechById(MarkUsedTechId).UsedThisTurn = true;
        }

        if (ActionType.HasValue)
        {
            await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
                new GameEvent_ActionComplete
                {
                    ActionType = ActionType.Value,
                });
        }

        if (!string.IsNullOrEmpty(gameEvent.TriggerToMarkResolved))
        {
            await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, gameEvent.TriggerToMarkResolved);
        }

        return builder;
    }

    protected string GetMoveName() => Tech?.DisplayName ?? MovementOperations.DefaultMoveName;
}