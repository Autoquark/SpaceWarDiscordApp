using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Produce;
using SpaceWarDiscordApp.Database.InteractionData.Tech.DisposableTroops;
using SpaceWarDiscordApp.Database.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_DisposableTroops : Tech, IInteractionHandler<ApplyDisposableTroopsBonusInteraction>, IInteractionHandler<DisposableTroopsDestroyForcesInteraction>,
    IInteractionHandler<DisposableTroopsClearPendingDestroyInteraction>
{
    private const int ProductionBonus = 2;
    
    public Tech_DisposableTroops() : base("disposableTroops", "Disposable Troops",
        $"When you produce, produce {ProductionBonus} additional forces, but all previously existing forces on that planet are destroyed.",
        "You can tell these ones are from the previous batch, they're already going a bit limp.")
    {
    }

    public override int GetDisplayedProductionBonus(Game game, BoardHex hex, GamePlayer player) => 2;

    public override PlayerTech CreatePlayerTech(Game game, GamePlayer player) =>
        new PlayerTech_DisposableTroops
        {
            TechId = Id
        };

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
        // If we somehow get to the end of an action with items still in the pendingdestroy collection, clear them out
        else if (gameEvent is GameEvent_ActionComplete actionComplete &&
                 GetThisTech<PlayerTech_DisposableTroops>(player).PendingDestroy.Count != 0)
        {
            return
            [
                new TriggeredEffect
                {
                    AlwaysAutoResolve = true,
                    IsMandatory = true,
                    DisplayName = DisplayName,
                    ResolveInteractionData = new DisposableTroopsClearPendingDestroyInteraction
                    {
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                        EventId = actionComplete.EventId,
                        Event = actionComplete
                    },
                    TriggerId = GetTriggerId(2)
                }
            ];
        }

        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyDisposableTroopsBonusInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        interactionData.Event.EffectiveProductionValue += ProductionBonus;
        
        var player = game.GetGamePlayerForInteraction(interactionData);
        GetThisTech<PlayerTech_DisposableTroops>(player).PendingDestroy.Add(interactionData.Event.Location);
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        builder?.AppendContentNewline($"Produced {ProductionBonus} additional forces due to {DisplayName}");
        
        return new SpaceWarInteractionOutcome(true);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        DisposableTroopsDestroyForcesInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        if (!GetThisTech<PlayerTech_DisposableTroops>(player).PendingDestroy.Remove(interactionData.Event.Location))
        {
            // If for some reason we didn't apply the production bonus to this produce (e.g. we gained this tech off the
            // produce), don't destroy preexisting forces.
            await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
            return new SpaceWarInteractionOutcome(true);
        }
        
        var hex = game.GetHexAt(interactionData.Event.Location);
        var toDestroy = hex.Planet!.ForcesPresent - interactionData.Event.ForcesProduced;
        GameFlowOperations.DestroyForces(game, hex, toDestroy, interactionData.Event.PlayerGameId, ForcesDestructionReason.Tech, Id);
        
        builder?.AppendContentNewline($"{toDestroy} preexisting disposable forces have been sent to landfill");
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        DisposableTroopsClearPendingDestroyInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        GetThisTech<PlayerTech_DisposableTroops>(player).PendingDestroy.Clear();
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }
}