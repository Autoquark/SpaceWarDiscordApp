using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.EfficientManufacturing;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_EfficientManufacturing : Tech, IInteractionHandler<ApplyEfficientManufacturingBonusInteraction>
{
    public Tech_EfficientManufacturing() : base("efficient-manufacturing",
        "Efficient Manufacturing",
        "Treat planets you control that have 1 production as having +1 production.",
        "Would you believe that this body armour is made out of mud? You would? Well, good! Because it is!")
    {
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_BeginProduce beginProduce)
        {
            var hex = game.GetHexAt(beginProduce.Location);
            if (hex.Planet?.OwningPlayerId == player.GamePlayerId && hex.Planet.Production == 1)
            {
                return
                [
                    new TriggeredEffect
                    {
                        AlwaysAutoResolve = true,
                        DisplayName = DisplayName,
                        IsMandatory = true,
                        ResolveInteractionData = new ApplyEfficientManufacturingBonusInteraction
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            EventDocumentId = beginProduce.DocumentId!,
                            Event = beginProduce
                        }
                    }
                ];
            }
        }

        return [];
    }


    public Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        ApplyEfficientManufacturingBonusInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        interactionData.Event.EffectiveProductionValue++;
        
        GameFlowOperations.TriggerResolved(game, interactionData.InteractionId);
        
        builder?.AppendContentNewline($"Produced 1 additional forces due to {DisplayName}");
        
        return Task.FromResult(new SpaceWarInteractionOutcome(true, builder));
    }
}