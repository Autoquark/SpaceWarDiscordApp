using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.Persuadatron;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;


namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_Persuadatron : Tech, IInteractionHandler<UsePersuadatronInteraction>
{
    public Tech_Persuadatron(): base("persuadatron", "Persuadatron 3000", 
    "Choose a planet adjacent to one you control. Replace all forces on it with the same quantity of your forces.",
    "Activate brain scanner... find all instances of 'blue'... replace with 'red'... and we're done!",
    ["Single Use", "Action"])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Main;
    }

    // Check if the action is available
    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) => base.IsSimpleActionAvailable(game, player) && GetTargets(game, player).Any();

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        // The first button click - get targets and ask for planet choice from player
        var targets = GetTargets(game, player).ToList();

        if (targets.Count == 0)
        {
            builder.AppendContentNewline("No targets available");
        }
        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x =>
            new UsePersuadatronInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                EditOriginalMessage = true,
                Target = x.Coordinates
            }));
        
        return builder.AppendHexButtons(game, targets, interactionIds);
    }
    
    private static IEnumerable<BoardHex> GetTargets(Game game, GamePlayer player) => game.Hexes.WhereOwnedBy(player)
        .SelectMany(x => BoardUtils.GetNeighbouringHexes(game, x))
        .WhereNotOwnedBy(player)
        .WhereForcesPresent()
        .DistinctBy(x => x.Coordinates);
    
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        UsePersuadatronInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Target);
        var player = game.GetGamePlayerForInteraction(interactionData);

        if (!hex.AnyForcesPresent)
        {
            throw new Exception();
        }
        
        // Replace all forces on the hex with the same number of our forces
        hex.Planet!.SetForces(hex.Planet!.ForcesPresent, player.GamePlayerId);
        
        player.CurrentTurnEvents.Add(new PlanetTargetedTechEventRecord
        {
            Coordinates = interactionData.Target
        });
        
        var name = await player.GetNameAsync(false);

        builder?.AppendContentNewline($"{name} took over {hex.Coordinates} using Persuadatron 3000.");

        await GameFlowOperations.CheckForPlayerEliminationsAsync(builder, game);
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_PlayerLoseTech
            {
                TechId = Id,
                PlayerGameId = player.GamePlayerId
            },
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });

        return new SpaceWarInteractionOutcome(true);
    }
}