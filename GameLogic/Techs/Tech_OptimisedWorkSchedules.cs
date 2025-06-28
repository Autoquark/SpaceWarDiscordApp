using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData.Tech.OptimisedWorkSchedules;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_OptimisedWorkSchedules : Tech, IInteractionHandler<TargetOptimisedWorkSchedulesInteraction>
{
    public Tech_OptimisedWorkSchedules() : base("optimisedWorkSchedules", 
        "Optimised Work Schedules",
        "Action, Exhaust: Produce from an exhausted planet", 
        "October will continue indefinitely until production quotas are met")
    {
        HasSimpleAction = true;
    }

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = game.Hexes
            .Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId && x.Planet!.IsExhausted)
            .ToList();

        if (targets.Count == 0)
        {
            builder.AppendContentNewline("No suitable targets");
            return builder;
        }
        
        builder.AppendContentNewline("Choose an exhausted planet to produce from:");

        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(targets.Select(x => new TargetOptimisedWorkSchedulesInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId,
            Target = x.Coordinates
        }), serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData.InteractionGroupId);
        
        return builder.AppendHexButtons(game, targets, interactionIds);
    }
    
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync<TBuilder>(TBuilder builder,
        TargetOptimisedWorkSchedulesInteraction interactionData,
        Game game, IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        await ProduceOperations.ProduceOnPlanetAsync(builder, game, game.GetHexAt(interactionData.Target), serviceProvider);
        
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        player.GetPlayerTechById(Id).IsExhausted = true;
        
        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Main, serviceProvider);
        
        return new SpaceWarInteractionOutcome(true, builder);
    }
}