using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Tech;
using SpaceWarDiscordApp.Database.InteractionData.Tech.WarpNodes;
using SpaceWarDiscordApp.Database.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

// ReSharper disable once InconsistentNaming
public class Tech_WarpNodes : Tech,
    IInteractionHandler<WarpNodes_ChooseSourceInteraction>,
    IInteractionHandler<WarpNodes_ChooseAmountInteraction>,
    IPlayerChoiceEventHandler<GameEvent_ChooseWarpNodesDestination, WarpNodes_ChooseDestinationInteraction>

{
    public Tech_WarpNodes() : base("warpNodes", 
        "Warp Nodes", 
        "**Action**: Choose a planet you control. Move any number of forces to any number of adjacent planets.", 
        "They're nodes that are made out of warp. I really don't know how I can make this any simpler")
    {
        HasSimpleAction = true;
    }
    
    public override PlayerTech CreatePlayerTech(Game game, GamePlayer player) => new PlayerTech_WarpNodes
    {
        TechId = Id
    };

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var playerTech = player.GetPlayerTechById<PlayerTech_WarpNodes>(Id);
        playerTech.MovedTo = [];
        
        var name = await player.GetNameAsync(true);
        builder.AppendContentNewline($"{name}, choose a hex to move from:")
            .AllowMentions(player);
        
        var sources = game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId && x.Planet.ForcesPresent > 0)
            .ToList();

        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(sources.Select(x =>
            new WarpNodes_ChooseSourceInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Source = x.Coordinates,
                Game = game.DocumentId
            }), serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData.InteractionGroupId);
        
        return builder.AppendHexButtons(game, sources, interactionIds);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync<TBuilder>(TBuilder builder,
        WarpNodes_ChooseSourceInteraction interactionData,
        Game game, IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var playerTech = player.GetPlayerTechById<PlayerTech_WarpNodes>(Id);
        playerTech.Source = interactionData.Source;

        await GameFlowOperations.PushGameEventsAsync(builder, game, serviceProvider, new GameEvent_ChooseWarpNodesDestination
        {
            PlayerGameId = player.GamePlayerId,
            Source = interactionData.Source
        });
        await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, serviceProvider);

        return new SpaceWarInteractionOutcome(true, builder);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync<TBuilder>(TBuilder builder,
        WarpNodes_ChooseAmountInteraction interactionData,
        Game game, IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var playerTech = player.GetPlayerTechById<PlayerTech_WarpNodes>(Id);
        var source = game.GetHexAt(playerTech.Source);
        
        var events = new List<GameEvent>();
        
        if (interactionData.Amount > 0)
        {
            events.AddRange(await MovementOperations.GetResolveMoveEventsAsync(builder, game, player, new PlannedMove
            {
                Sources = [new SourceAndAmount { Source = source.Coordinates, Amount = interactionData.Amount }],
                Destination = interactionData.Destination
            }, serviceProvider));
            playerTech.MovedTo.Add(interactionData.Destination);
        }
        
        // After resolving this move: If we could perform another Warp Nodes move, reprompt
        // otherwise, action is complete
        if (source.Planet?.ForcesPresent > 0 
            && source.Planet.OwningPlayerId == player.GamePlayerId
            && playerTech.MovedTo.Count < BoardUtils.GetNeighbouringHexes(game, source).Count)
        {
            events.Add(new GameEvent_ChooseWarpNodesDestination
            {
                PlayerGameId = interactionData.ForGamePlayerId,
                Source = playerTech.Source,
            });
        }
        else
        {
            events.Add(new GameEvent_ActionComplete
            {
                ActionType = ActionType.Main
            });
        }
        
        await GameFlowOperations.PushGameEventsAsync(builder, game, serviceProvider, events);
        await GameFlowOperations.ContinueResolvingEventStackAsync(builder, game, serviceProvider);
        
        return new SpaceWarInteractionOutcome(true, builder);
    }

    public async Task<TBuilder> ShowPlayerChoicesAsync<TBuilder>(
        TBuilder builder, GameEvent_ChooseWarpNodesDestination gameEvent, Game game, IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var player = game.GetGamePlayerByGameId(gameEvent.PlayerGameId);
        var source = game.GetHexAt(gameEvent.Source);
        var name = await player.GetNameAsync(true);
        var playerTech = player.GetPlayerTechById<PlayerTech_WarpNodes>(Id);
        
        var destinations = BoardUtils.GetNeighbouringHexes(game, source)
            // Can't move to the same planet more than once per usage of Warp Nodes
            .Where(x => !playerTech.MovedTo.Contains(x.Coordinates))
            .ToList();
        
        var interactionIds =
            await InteractionsHelper.SetUpInteractionsAsync(destinations.Select<BoardHex, HexCoordinates?>(x => x.Coordinates)
                // Add null for declining further moves
                .Append(null)
                .Select(x => new WarpNodes_ChooseDestinationInteraction
                {
                    ForGamePlayerId = player.GamePlayerId,
                    Game = game.DocumentId,
                    Destination = x,
                    ResolvesChoiceEvent = gameEvent.DocumentId
                }), serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData.InteractionGroupId);
        
        return builder.AppendContentNewline($"{name}, choose a planet to move to:")
            .AllowMentions(player)
            .AppendHexButtons(game, destinations, interactionIds)
            .AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Success, 
                    interactionIds.Last(), 
                    "Done making Warp Nodes moves"));
    }

    public async Task<TBuilder?> HandlePlayerChoiceEventResolvedAsync<TBuilder>(TBuilder? builder,
        GameEvent_ChooseWarpNodesDestination gameEvent,
        WarpNodes_ChooseDestinationInteraction choice,
        Game game, IServiceProvider serviceProvider) 
        where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var player = game.GetGamePlayerByGameId(choice.ForGamePlayerId);
        var playerTech = player.GetPlayerTechById<PlayerTech_WarpNodes>(Id);
        var name = await player.GetNameAsync(true);

        // Null value indicates done making moves
        if (!choice.Destination.HasValue)
        {
            await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Main, serviceProvider);
            return builder;
        }

        var maxAmount = game.GetHexAt(playerTech.Source).Planet!.ForcesPresent;
        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(Enumerable.Range(0, maxAmount + 1).Select(x => new WarpNodes_ChooseAmountInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId,
            Amount = x,
            Destination = choice.Destination.Value
        }), serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData.InteractionGroupId);
        
        return builder?.AppendContentNewline($"{name}, choose amount of forces to move:")
            .AllowMentions(player)
            .AppendButtonRows(interactionIds.ZipWithIndices().Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, x.item, x.index.ToString())));
    }
}