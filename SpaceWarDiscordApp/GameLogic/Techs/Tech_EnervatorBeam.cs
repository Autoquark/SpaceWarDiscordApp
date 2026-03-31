using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.Interactions.Tech.EnervatorBeam;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_EnervatorBeam : Tech, ISpaceWarInteractionHandler<UseEnervatorBeamInteraction>
{
    public Tech_EnervatorBeam() : base("enervatorBeam",
        "Enervator Beam",
        "Exhaust any planet",
        "Does anybody else feel tired today?",
        [TechKeyword.FreeAction, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player)
        => base.IsSimpleActionAvailable(game, player) && game.Hexes.Any(x => x.Planet?.IsExhausted == false);

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = game.Hexes.Where(x => x.Planet?.IsExhausted == false)
            .ToList();

        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x =>
            new UseEnervatorBeamInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Target = x.Coordinates,
                EditOriginalMessage = true
            }));

        builder.AppendContentNewline("Enervator Beam: Choose a planet to exhaust:");
        
        return builder.AppendHexButtons(game, targets, interactionIds);
    }

    public async Task<InteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        UseEnervatorBeamInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        game.GetHexAt(interactionData.Target).Planet!.IsExhausted = true;
        var player = game.GetGamePlayerForInteraction(interactionData);
        var name = await player.GetNameAsync(false);
        
        builder?.AppendContentNewline($"{name} has exhausted {interactionData.Target} using Enervator Beam");
        
        GetThisTech(player).IsExhausted = true;
        player.CurrentTurnEvents.Add(new PlanetTargetedTechEventRecord
        {
            Coordinates = interactionData.Target
        });
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });
        
        return new InteractionOutcome(true);
    }
}