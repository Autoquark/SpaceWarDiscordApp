using System.Numerics;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Movement;
using SpaceWarDiscordApp.Database.GameEvents.Refresh;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech.SpecialisedWeaponry;
using SpaceWarDiscordApp.Database.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_SpecialisedWeaponry : Tech, IInteractionHandler<UseSpecialisedWeaponryInteraction>, IInteractionHandler<ApplySpecialisedWeaponryBonusInteraction>,
    IInteractionHandler<ResetSpecialisedWeaponryInteraction>
{
    public Tech_SpecialisedWeaponry() : base("specialisedWeaponry",
        "Specialised Weaponry",
        "Choose a player. Gain +1 Combat Strength against that player until you refresh this tech.",
        "Before opening fire, please consult Appendix A of the operator's manual and configure weapon appropriately. Failure to select appropriate ammunition type may result in death or voided warranty.",
        [TechKeyword.FreeAction, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        CheckTriggersWhenExhausted = true;
    }

    public override bool ShouldIncludeInGame(Game game) => base.ShouldIncludeInGame(game) && game.Players.Count > 2;
    
    public override PlayerTech CreatePlayerTech(Game game, GamePlayer player) =>
        new PlayerTech_SpecialisedWeaponry
        {
            TechId = Id,
        };

    public override async Task<string> GetTechStatusLineAsync(Game game, GamePlayer player)
    {
        var playerTech = GetThisTech<PlayerTech_SpecialisedWeaponry>(player);
        var targetPlayer = game.TryGetGamePlayerByGameId(playerTech.TargetGamePlayerId);
        // TODO: Include player colour symbol here - probably need to start passing status line through inline icon code
        return await base.GetTechStatusLineAsync(game, player) + (targetPlayer == null ? " [Inactive]" : $" [{await targetPlayer.GetNameAsync(false, false)}]");
    }

    public override int GetDisplayedCombatStrengthBonus(Game game, BoardHex hex, GamePlayer player)
    {
        var playerTech = TryGetThisTech<PlayerTech_SpecialisedWeaponry>(player);
        if (playerTech == null)
        {
            return 0;
        }
        
        var targetPlayer = game.TryGetGamePlayerByGameId(playerTech.TargetGamePlayerId);

        // If the player owning the hex has this tech and the player taking their turn is the target player, show a +1
        return game.CurrentTurnPlayer == targetPlayer ? 1 : 0;
    }

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var interactionIds = serviceProvider.AddInteractionsToSetUp(game.Players.Except(player).Select(x =>
            new UseSpecialisedWeaponryInteraction
            {
                TargetGamePlayerId = x.GamePlayerId,
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId
            }));

        var cancelId = serviceProvider.AddInteractionToSetUp(new RepromptInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId
        });

        builder.AppendContentNewline(
            $"{await player.GetNameAsync(true)}, choose a player to specialise your weaponry against:");
        
        return await builder.AppendPlayerButtonsAsync(game.Players.Except(player), interactionIds, cancelId);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, UseSpecialisedWeaponryInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        var targetPlayer = game.GetGamePlayerByGameId(interactionData.TargetGamePlayerId);
        var playerTech = GetThisTech<PlayerTech_SpecialisedWeaponry>(player);
        playerTech.TargetGamePlayerId = targetPlayer.GamePlayerId;
        playerTech.IsExhausted = true;

        builder?.AppendContentNewline(
            $"{await player.GetNameAsync(false)} is specialising their weaponry against {await targetPlayer.GetNameAsync(true)}!");
        

        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider, new GameEvent_ActionComplete
        {
            ActionType = SimpleActionType,
        });
        
        return new SpaceWarInteractionOutcome(true);
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_PreMove preMove)
        {
            var playerTech = GetThisTech<PlayerTech_SpecialisedWeaponry>(player);
            var targetPlayer = game.TryGetGamePlayerByGameId(playerTech.TargetGamePlayerId);
            if (targetPlayer == null)
            {
                return [];
            }
            
            var destination = game.GetHexAt(preMove.Destination);
            bool? isAttacker = null;
            if (preMove.MovingPlayerId == player.GamePlayerId && destination.Planet!.OwningPlayerId == targetPlayer.GamePlayerId)
            {
                isAttacker = true;
            }
            else if (destination.Planet!.OwningPlayerId == player.GamePlayerId && preMove.MovingPlayerId == targetPlayer.GamePlayerId)
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
                        ResolveInteractionData = new ApplySpecialisedWeaponryBonusInteraction
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
        else if (gameEvent is GameEvent_TechRefreshed techRefreshed && techRefreshed.TechId == Id &&
                 techRefreshed.PlayerGameId == player.GamePlayerId)
        {
            return
            [
                new TriggeredEffect
                {
                    AlwaysAutoResolve = true,
                    IsMandatory = true,
                    DisplayName = DisplayName,
                    ResolveInteractionData = new ResetSpecialisedWeaponryInteraction
                    {
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                        Event = techRefreshed,
                        EventId = techRefreshed.EventId
                    },
                    TriggerId = GetTriggerId(1)
                }
            ];
        }
        
        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        ApplySpecialisedWeaponryBonusInteraction interactionData, Game game, IServiceProvider serviceProvider)
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

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ResetSpecialisedWeaponryInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        GetThisTech<PlayerTech_SpecialisedWeaponry>(player).TargetGamePlayerId = GamePlayer.GamePlayerIdNone;
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }
}