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
        "Gain +1 Combat Strength until the start of your next turn.",
        "Some of you may die. Many of you, in fact. We must seriously consider the possibility that all of you will die. Nevertheless...",
        [TechKeyword.FreeAction, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        CheckTriggersWhenExhausted = true;
    }
    
    public override PlayerTech CreatePlayerTech(Game game, GamePlayer player) => new PlayerTech_RousingSpeech()
    {
        TechId = Id,
    };

    public override string GetTechStatusLine(Game game, GamePlayer player)
    {
        var playerTech = GetThisTech<PlayerTech_RousingSpeech>(player);
        return base.GetTechStatusLine(game, player) + (playerTech.TurnsActiveRemaining > 0 ? " [Active]" : " [Inactive]");
    }

    public override int GetDisplayedCombatStrengthBonus(Game game, BoardHex hex, GamePlayer player)
        => GetThisTech<PlayerTech_RousingSpeech>(player).TurnsActiveRemaining > 0 ? 1 : 0;

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var thisTech = GetThisTech<PlayerTech_RousingSpeech>(player);
        thisTech.IsExhausted = true;
        
        // Lasts until the start of the next turn
        thisTech.TurnsActiveRemaining = 1;
        
        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} performs a rousing speech!");
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });
        
        return builder;
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        var playerTech = GetThisTech(player) as PlayerTech_RousingSpeech;

        if (playerTech == null || playerTech.TurnsActiveRemaining <= 0)
        {
            // Only decrement or utilise tech if turns remaining at least 1
            return [];
        }

        if (gameEvent is GameEvent_TurnBegin turnBegin && turnBegin.PlayerGameId == player.GamePlayerId)
        { 
            playerTech.TurnsActiveRemaining--;
        }
        
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
                        IsMandatory = true,
                        DisplayName = DisplayName,
                        ResolveInteractionData = new ApplyRousingSpeechBonusInteraction
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            IsAttacker = isAttacker.Value,
                            Event = preMove,
                            EventId = preMove.EventId
                        },
                        TriggerId = GetTriggerId(0)
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
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }
}
