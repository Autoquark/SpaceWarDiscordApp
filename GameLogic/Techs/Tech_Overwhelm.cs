using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.Overwhelm;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_Overwhelm : Tech, IInteractionHandler<ApplyOverwhelmBonusInteraction>
{
    public Tech_Overwhelm() : base("overwhelm", "Overwhelm",
        "When you have 5 or more forces present, +1 Combat Strength.",
        "Pleased to report that our bombardment has inflicted heavy casualties, some of them among the enemy.")
    {
        
    }

    public override int GetDisplayedCombatStrengthBonus(Game game, BoardHex hex, GamePlayer player) => hex.ForcesPresent >= 5 ? 1 : 0;

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        bool? isAttacker = null;
        if (gameEvent is GameEvent_PreMove preMove)
        {
            if (preMove.MovingPlayerId == player.GamePlayerId && preMove.Sources.Sum(x => x.Amount) >= 5)
            {
                isAttacker = true;
            }
            var destination = game.GetHexAt(preMove.Destination);
            if(destination.Planet!.OwningPlayerId == player.GamePlayerId && destination.Planet!.ForcesPresent >= 5)
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
                        IsMandatory = true,
                        DisplayName = DisplayName,
                        ResolveInteractionData = new ApplyOverwhelmBonusInteraction()
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            Event = preMove,
                            EventId = preMove.EventId,
                            IsAttacker = isAttacker.Value
                        }
                    }
                ];
            }
        }

        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyOverwhelmBonusInteraction interactionData,
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