using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.RousingSpeech;
using SpaceWarDiscordApp.Database.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_RousingSpeech : Tech, IInteractionHandler<ApplyRousingSpeechBonusInteraction>
{

    
    public Tech_RousingSpeech (): base("rousing-speech", "Rousing Speech", 
        "Free Action, Exhaust: Gain +1 Combat Strength until the start of your next turn.",
        "Flavour: Some of you may die. Many of you, in fact. " +
        "We must seriously consider the possibility that all of you will die. Nevertheless...")
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }
    
    public override PlayerTech CreatePlayerTech(Game game, GamePlayer player) => new PlayerTech_TurnBased()
    {
        TechId = Id,
        Game = game.DocumentId!
    };

    
    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var thisTech = GetThisTech(player) as PlayerTech_TurnBased;
        thisTech!.IsExhausted = true;
        
        // Lasts until the start of the next turn
        thisTech.TurnsActiveRemaining = 1;
        
        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} performs a rousing speech!");
        
        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Free, serviceProvider);
        
        return builder;
    }

    public override IEnumerable<TriggeredEffect> GetTriggeredEffects(Game game, GameEvent gameEvent, GamePlayer player)
    {
        var playerTech = GetThisTech(player) as PlayerTech_TurnBased;
        if (playerTech!.TurnsActiveRemaining > 0 && gameEvent is GameEvent_PreMove preMove)
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
                        IsMandatory = true,
                        DisplayName = DisplayName,
                        ResolveInteractionData = new ApplyRousingSpeechBonusInteraction
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            IsAttacker = isAttacker.Value,
                            Event = preMove,
                            EventDocumentId = preMove.DocumentId!
                        }
                    }
                ];
            }
        }
    
        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyRousingSpeechBonusInteraction interactionData,
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
        
        return new SpaceWarInteractionOutcome(true, builder);
    }
}
