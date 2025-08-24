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
        "Produce from an exhausted planet", 
        "October will continue indefinitely until production quotas are met",
        ["Action", "Exhaust"])
    {
        HasSimpleAction = true;
    }

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
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
    
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        TargetOptimisedWorkSchedulesInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        await ProduceOperations.PushProduceOnPlanetAsync(builder, game, game.GetHexAt(interactionData.Target), serviceProvider);
        
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        player.GetPlayerTechById(Id).IsExhausted = true;
        
        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Main, serviceProvider);
        
        return new SpaceWarInteractionOutcome(true, builder);
    }
}