using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Movement;
using SpaceWarDiscordApp.Database.GameEvents.Produce;
using SpaceWarDiscordApp.Database.InteractionData.Tech.EliteTroops;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_EliteTroops : Tech, IInteractionHandler<ApplyEliteTroopsBonusInteraction>, IInteractionHandler<ApplyEliteTroopsProductionReductionInteraction>
{
    public Tech_EliteTroops(): base("eliteTroops", "Elite Troops", 
        "You have +1 Combat Strength. Whenever you produce, produce 1 fewer forces (minimum of 1).", 
        "In retrospect we shouldn't have given each of them their own personal coffee machine.")
    {

    }
    
    public override int GetDisplayedCombatStrengthBonus(Game game, BoardHex hex, GamePlayer player) => 1;
    
    public override int GetDisplayedProductionBonus(Game game, BoardHex hex, GamePlayer player) => -1;
    
    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        // Apply production penalty
        if (gameEvent is GameEvent_BeginProduce BeginProduce)
        {
            var hex = game.GetHexAt(BeginProduce.Location);
            if (hex.Planet?.OwningPlayerId == player.GamePlayerId)
            {
                return
                [
                    new TriggeredEffect
                    {
                        AlwaysAutoResolve = true,
                        DisplayName = DisplayName,
                        IsMandatory = true,
                        ResolveInteractionData = new ApplyEliteTroopsProductionReductionInteraction
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            EventId = BeginProduce.EventId,
                            Event = BeginProduce
                        },
                        TriggerId = GetTriggerId(0)
                    }
                ];
            }
        }
        
        // Apply Strength bonus to all attack / defence forces
        else if (gameEvent is GameEvent_PreMove preMove)
        {
            var destination = game.GetHexAt(preMove.Destination);
            bool? isAttacker = null;
            if (preMove.MovingPlayerId == player.GamePlayerId)
            {
                isAttacker = true;
            }
            else if (destination.Planet!.OwningPlayerId == player.GamePlayerId)
            {
                isAttacker = false;
            }

            if (isAttacker != null)
            {
                return
                [
                    new TriggeredEffect
                    {
                        AlwaysAutoResolve = true,
                        DisplayName = DisplayName,
                        IsMandatory = true,
                        ResolveInteractionData = new ApplyEliteTroopsBonusInteraction
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            EventId = preMove.EventId,
                            Event = preMove,
                            IsAttacker = isAttacker.Value
                        },
                        TriggerId = GetTriggerId(0)
                    }
                ];
            }
            
        }

        return [];
    }
    
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyEliteTroopsProductionReductionInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }
    
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyEliteTroopsBonusInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        if (interactionData.IsAttacker)
        {
            interactionData.Event.AttackerCombatStrengthSources.Add(new CombatStrengthSource
            {
                DisplayName = DisplayName,
                Amount = 1
            });
        }
        else
        {
            interactionData.Event.DefenderCombatStrengthSources.Add(new CombatStrengthSource
            {
                DisplayName = DisplayName,
                Amount = 1
            });
        }
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }
}