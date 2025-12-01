using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.FreezeDriedForces;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_FreezeDriedForces : Tech, IInteractionHandler<UseFreezeDriedForcesInteraction>
{
    public Tech_FreezeDriedForces() : base(
        "freeze_dried_forces",
        "Freeze Dried Forces",
        "Produce 3 forces on a planet you control.",
        "War has never been so convenient!",
        [TechKeyword.Action, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
    }

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = game.Hexes.WhereOwnedBy(player).ToList();

        if (targets.Count == 0)
        {
            builder.AppendContentNewline("No suitable targets");
            return builder;
        }
        
        builder.AppendContentNewline($"{await player.GetNameAsync(true)}, choose a planet to deploy {DisplayName}:");
            
        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x =>
            new UseFreezeDriedForcesInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Target = x.Coordinates
            }));
        
        return builder.AppendHexButtons(game, targets, interactionIds);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        UseFreezeDriedForcesInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Target);
        hex.Planet!.AddForces(3);
        
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var name = await player.GetNameAsync(false);
        builder?.AppendContentNewline($"{name} added 3 forces to {hex.Coordinates} using {DisplayName}");
        
        player.GetPlayerTechById(Id).IsExhausted = true;
        
        ProduceOperations.CheckPlanetCapacity(game, hex);

        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });
        
        return new SpaceWarInteractionOutcome(true);
    }
}