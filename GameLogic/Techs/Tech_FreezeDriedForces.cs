using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
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
        ["Action", "Exhaust"])
    {
        HasSimpleAction = true;
    }

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId).ToList();

        if (targets.Count == 0)
        {
            builder.AppendContentNewline("No suitable targets");
            return builder;
        }
            
        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(targets.Select(x =>
            new UseFreezeDriedForcesInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Target = x.Coordinates
            }), serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData.InteractionGroupId);
        
        return builder.AppendHexButtons(game, targets, interactionIds);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        UseFreezeDriedForcesInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Target);
        hex.Planet!.ForcesPresent += 3;
        
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var name = await player.GetNameAsync(false);
        builder?.AppendContentNewline($"{name} produces 3 forces on {hex.Coordinates} using {DisplayName}");
        ProduceOperations.CheckPlanetCapacity(builder, hex);
        
        player.GetPlayerTechById(Id).IsExhausted = true;
        
        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Main, serviceProvider);
        
        return new SpaceWarInteractionOutcome(true, builder);
    }
}