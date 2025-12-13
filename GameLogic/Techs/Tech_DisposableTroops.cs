using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.DisposableTroops;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_DisposableTroops : Tech, IInteractionHandler<ApplyDisposableTroopsBonusInteraction>, IInteractionHandler<DisposableTroopsDestroyForcesInteraction>
{
    private const int ProductionBonus = 2;
    
    public Tech_DisposableTroops() : base("disposableTroops", "Disposable Troops",
        $"When you produce, produce {ProductionBonus} additional forces, but all previously existing forces on that planet are destroyed.",
        "You can tell these ones are from the previous batch, they're already going a bit limp.")
    {
    }

    public override int GetDisplayedProductionBonus(Game game, BoardHex hex, GamePlayer player) => 2;

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_BeginProduce beginProduce)
        {
            var (hex, producer) = beginProduce.GetProducingHexAndPlayer(game);
            if (producer == player)
            {
                return
                [
                    new TriggeredEffect
                    {
                        AlwaysAutoResolve = true,
                        IsMandatory = true,
                        DisplayName = DisplayName,
                        ResolveInteractionData = new ApplyDisposableTroopsBonusInteraction
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
        else if (gameEvent is GameEvent_PostProduce postProduce && postProduce.PlayerGameId == player.GamePlayerId)
        {
            return
            [
                new TriggeredEffect
                {
                    AlwaysAutoResolve = true,
                    IsMandatory = true,
                    DisplayName = DisplayName,
                    ResolveInteractionData = new DisposableTroopsDestroyForcesInteraction
                    {
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                        EventId = postProduce.EventId,
                        Event = postProduce
                    },
                    TriggerId = GetTriggerId(1)
                }
            ];
        }

        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyDisposableTroopsBonusInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        interactionData.Event.EffectiveProductionValue += ProductionBonus;
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        builder?.AppendContentNewline($"Produced {ProductionBonus} additional forces due to {DisplayName}");
        
        return new SpaceWarInteractionOutcome(true);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        DisposableTroopsDestroyForcesInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Event.Location);
        var toDestroy = hex.Planet!.ForcesPresent - interactionData.Event.ForcesProduced;
        GameFlowOperations.DestroyForces(game, hex, toDestroy, interactionData.Event.PlayerGameId, ForcesDestructionReason.Tech, Id);
        
        builder?.AppendContentNewline($"{toDestroy} preexisting disposable forces have been sent to landfill");
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }
}