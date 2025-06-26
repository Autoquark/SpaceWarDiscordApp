using DSharpPlus.Entities;
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
        "Action, Exhaust: Produce 3 forces on a planet you control.",
        "War has never been so convenient!")
    {
        HasSimpleAction = true;
    }

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player)
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
            }));
        
        return builder.AppendHexButtons(game, targets, interactionIds);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync<TBuilder>(TBuilder builder,
        UseFreezeDriedForcesInteraction interactionData,
        Game game) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        var hex = game.GetHexAt(interactionData.Target);
        hex.Planet!.ForcesPresent += 3;
        
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} produces 3 forces on {hex.Coordinates} using {DisplayName}");
        ProduceOperations.CheckPlanetCapacity(builder, hex);
        
        player.GetPlayerTechById(Id).IsExhausted = true;
        
        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Main);
        
        return new SpaceWarInteractionOutcome(true, builder);
    }
}