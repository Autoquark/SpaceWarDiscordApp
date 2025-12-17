using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Produce;
using SpaceWarDiscordApp.Database.InteractionData.Tech.Isolationism;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_Isolationism : Tech, IInteractionHandler<ApplyIsolationismBonusInteraction>
{
    private const int ProductionBonus = 1;
    
    public Tech_Isolationism() : base("isolationism", "Isolationism",
        "When you produce on a planet, produce 1 additional forces if there are no adjacent planets controlled by opponents.",
        "We are the only truly intelligent life form in the universe. The recent missile strikes against our military installations were merely a primitive instinctual behaviour.")
    {
    }

    public override int GetDisplayedProductionBonus(Game game, BoardHex hex, GamePlayer player) => IsHexAffected(game, hex, player) ? ProductionBonus : 0;

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_BeginProduce beginProduce)
        {
            var (hex, producingPlayer) = beginProduce.GetProducingHexAndPlayer(game);
            if (producingPlayer == player && IsHexAffected(game, hex, player))
            {
                return
                [
                    new TriggeredEffect
                    {
                        AlwaysAutoResolve = true,
                        DisplayName = DisplayName,
                        IsMandatory = true,
                        ResolveInteractionData = new ApplyIsolationismBonusInteraction
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

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyIsolationismBonusInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        interactionData.Event.EffectiveProductionValue += ProductionBonus;
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        builder?.AppendContentNewline($"Produced {ProductionBonus} additional forces due to {DisplayName}");
        
        return new SpaceWarInteractionOutcome(true);
    }

    private static bool IsHexAffected(Game game, BoardHex hex, GamePlayer owningPlayer) 
        => BoardUtils.GetNeighbouringHexes(game, hex)
            .All(x => x.IsNeutral || x.Planet?.OwningPlayerId == owningPlayer.GamePlayerId);
}