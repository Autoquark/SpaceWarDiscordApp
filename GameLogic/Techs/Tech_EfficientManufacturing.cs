using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Produce;
using SpaceWarDiscordApp.Database.InteractionData.Tech.EfficientManufacturing;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_EfficientManufacturing : Tech, IInteractionHandler<ApplyEfficientManufacturingBonusInteraction>
{
    public Tech_EfficientManufacturing() : base("efficient-manufacturing",
        "Efficient Manufacturing",
        "When you produce on a planet with 1 or 0 production, produce 1 additional forces.",
        "Would you believe that this body armour is made out of mud? You would? Well, good! Because it is!")
    {
    }

    public override int GetDisplayedProductionBonus(Game game, BoardHex hex, GamePlayer player) => IsHexAffected(hex) ? 1 : 0;

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_BeginProduce beginProduce)
        {
            var hex = game.GetHexAt(beginProduce.Location);
            if (hex.Planet?.OwningPlayerId == player.GamePlayerId && IsHexAffected(hex))
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
                            EventId = beginProduce.EventId,
                            Event = beginProduce
                        },
                        TriggerId = GetTriggerId(0)
                    }
                ];
            }
        }

        return [];
    }


    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        ApplyEfficientManufacturingBonusInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        interactionData.Event.EffectiveProductionValue++;
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        builder?.AppendContentNewline($"Produced 1 additional forces due to {DisplayName}");
        
        return new SpaceWarInteractionOutcome(true);
    }
    
    private static bool IsHexAffected(BoardHex hex) => hex.Planet?.Production <= 1;
}