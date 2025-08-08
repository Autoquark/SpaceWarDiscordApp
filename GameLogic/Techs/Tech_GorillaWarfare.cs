using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.GorillaWarfare;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_GorillaWarfare : Tech, IInteractionHandler<ApplyGorillaWarfareBonusInteraction>
{
    public Tech_GorillaWarfare() : base("gorillaWarfare", "Gorilla Warfare",
        "If you have only 1 forces present, +1 Combat Strength",
        "What began as an unfortunate miscommunication has developed into a valuable mastery of covert simian operations.")
    {
        
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_PreMove preMove)
        {
            bool? isAttacker = null;
            if (preMove.MovingPlayerId == player.GamePlayerId && preMove.Sources.Sum(x => x.Amount) == 1)
            {
                isAttacker = true;
            }
            var destination = game.GetHexAt(preMove.Destination);
            if (destination.Planet!.OwningPlayerId == player.GamePlayerId && destination.Planet!.ForcesPresent == 1)
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
                        ResolveInteractionData = new ApplyGorillaWarfareBonusInteraction
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            Event = preMove,
                            EventDocumentId = preMove.DocumentId!,
                            IsAttacker = isAttacker.Value
                        }
                    }
                ];
            }
        }

        return [];
    }

    public Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyGorillaWarfareBonusInteraction interactionData,
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
        
        GameFlowOperations.TriggerResolved(game, interactionData.InteractionId);
        
        return Task.FromResult(new SpaceWarInteractionOutcome(true, builder));
    }
}