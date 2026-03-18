using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.OptimisedWorkSchedules;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_OptimisedWorkSchedules : Tech, IInteractionHandler<TargetOptimisedWorkSchedulesInteraction>
{
    public Tech_OptimisedWorkSchedules() : base("optimisedWorkSchedules", 
        "Optimised Work Schedules",
        "Produce from an exhausted planet you control", 
        "October will continue indefinitely until production quotas are met",
        [TechKeyword.Action, TechKeyword.Exhaust])
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

        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x => new TargetOptimisedWorkSchedulesInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId,
            Target = x.Coordinates
        }));
        
        return builder.AppendHexButtons(game, targets, interactionIds);
    }
    
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        TargetOptimisedWorkSchedulesInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        player.GetPlayerTechById(Id).IsExhausted = true;

        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            ProduceOperations.CreateProduceEvent(game, interactionData.Target, true),
            new GameEvent_ActionComplete
            {
                ActionType = ActionType.Main
            });
        
        return new SpaceWarInteractionOutcome(true);
    }
}