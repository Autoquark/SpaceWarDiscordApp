using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.InteractionData.Tech.Persuadatron;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;


namespace SpaceWarDiscordApp.GameLogic.Techs;

public class PersuadatronTech : Tech, IInteractionHandler<UsePersuadatronInteraction>
{
    public PersuadatronTech(): base("persuadatron", "Persuadatron 3000", 
    "Single Use, Action: Choose a planet adjacent to one you control. Replace all forces on it with the same quantity of your forces.",
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
        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(targets.Select(x =>
            new UsePersuadatronInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                EditOriginalMessage = true,
                Target = x.Coordinates
            }), serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData.InteractionGroupId);
        
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

        // Subtract current forces, then add current players with new ownership
        // Do two steps to maintain game integrity
        var forcesNumber = hex.Planet!.ForcesPresent;
        hex.Planet!.SubtractForces(forcesNumber);
        hex.Planet!.SetForces(forcesNumber, player.GamePlayerId);

        player.Techs.Remove(GetThisTech(player));
        
        var name = await player.GetNameAsync(false);
        
        player.CurrentTurnEvents.Add(new PlanetTargetedTechEventRecord
        {
            Coordinates = hex.Coordinates
        });
        
        builder?.AppendContentNewline($"{name} took over {hex.Coordinates} using Persuadatron 3000.");

        await GameFlowOperations.CheckForPlayerEliminationsAsync(builder, game);
        
        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Main, serviceProvider);

        return new SpaceWarInteractionOutcome(true, builder);
    }
}