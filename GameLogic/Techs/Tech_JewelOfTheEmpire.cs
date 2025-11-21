using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.JewelOfTheEmpire;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_JewelOfTheEmpire : Tech, IInteractionHandler<ApplyJewelOfTheEmpireBonusInteraction>
{
    public Tech_JewelOfTheEmpire() : base("jewel-of-the-empire",
        "Jewel of the Empire",
        "When you produce on a home planet, produce an additional 2 forces.",
        "Everybody wants to live in the capital. I hear they have three different flavours of nutrient paste!")
    {
    }

    public override int GetDisplayedProductionBonus(Game game, BoardHex hex, GamePlayer player) => hex.Planet!.IsHomeSystem ? 2 : 0;

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_BeginProduce beginProduce)
        {
            var hex = game.GetHexAt(beginProduce.Location);
            if (hex.Planet?.OwningPlayerId == player.GamePlayerId && hex.Planet.IsHomeSystem)
            {
                return
                [
                    new TriggeredEffect
                    {
                        AlwaysAutoResolve = true,
                        DisplayName = DisplayName,
                        IsMandatory = true,
                        ResolveInteractionData = new ApplyJewelOfTheEmpireBonusInteraction
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

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyJewelOfTheEmpireBonusInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        interactionData.Event.EffectiveProductionValue += 2;
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        builder?.AppendContentNewline($"Produced 2 additional forces due to {DisplayName}");
        
        return new SpaceWarInteractionOutcome(true);
    }
}