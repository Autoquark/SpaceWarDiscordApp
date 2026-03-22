using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Movement;
using SpaceWarDiscordApp.Database.Interactions;
using SpaceWarDiscordApp.Database.Interactions.Tech.LiveFireExercise;
using SpaceWarDiscordApp.Database.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_LiveFireExercise : Tech,
    IInteractionHandler<SelectLiveFireExercisePlanetInteraction>,
    IInteractionHandler<ApplyLiveFireExerciseCombatBonusInteraction>
{
    public Tech_LiveFireExercise() : base("live-fire-exercise", "Live Fire Exercise",
        "Remove 1 forces from a planet you control. If you do you have +1 Combat Strength until the start of your next turn.",
        "Congratulations. You obliterated those 'simulated' enemy forces in a very real and expensive manner",
        [TechKeyword.FreeAction, TechKeyword.OncePerTurn])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        SimpleActionIsOncePerTurn = true;
        CheckTriggersWhenExhausted = true;
    }

    public override PlayerTech CreatePlayerTech(Game game, GamePlayer player) => new PlayerTech_LiveFireExercise
    {
        TechId = Id,
    };

    private IEnumerable<BoardHex> GetTargets(Game game, GamePlayer player)
        => game.Hexes.WhereOwnedBy(player).Where(x => x.ForcesPresent > 0);

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player)
        => base.IsSimpleActionAvailable(game, player) && GetTargets(game, player).Any();

    public override async Task<string> GetTechStatusLineAsync(Game game, GamePlayer player)
    {
        var playerTech = GetThisTech<PlayerTech_LiveFireExercise>(player);
        return await base.GetTechStatusLineAsync(game, player) + (playerTech.TurnsActiveRemaining > 0 ? " [Active]" : " [Inactive]");
    }

    public override int GetDisplayedCombatStrengthBonus(Game game, BoardHex hex, GamePlayer player)
        => GetThisTech<PlayerTech_LiveFireExercise>(player).TurnsActiveRemaining > 0 ? 1 : 0;

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = GetTargets(game, player).ToList();

        builder.AppendContentNewline($"{await player.GetNameAsync(true)}, choose a planet to remove 1 forces from:");

        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x =>
            new SelectLiveFireExercisePlanetInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                EditOriginalMessage = true,
                Target = x.Coordinates
            }));

        var cancelId = serviceProvider.AddInteractionToSetUp(new RepromptInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId
        });

        return builder.AppendHexButtons(game, targets, interactionIds)
            .AppendCancelButton(cancelId);
    }

    public async Task<InteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, SelectLiveFireExercisePlanetInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        var hex = game.GetHexAt(interactionData.Target);

        GameFlowOperations.DestroyForces(game, hex, 1, player.GamePlayerId, ForcesDestructionReason.Tech, Id);

        var playerTech = GetThisTech<PlayerTech_LiveFireExercise>(player);
        playerTech.UsedThisTurn = true;
        playerTech.TurnsActiveRemaining = 1;

        builder?.AppendContentNewline(
            $"{await player.GetNameAsync(false)} is conducting a {DisplayName} on {hex.ToHexNumberWithDieEmoji(game)}. 1 forces were lost in an unfortunate accident.");

        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });

        return new InteractionOutcome(true);
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        var playerTech = GetThisTech<PlayerTech_LiveFireExercise>(player);

        if (playerTech.TurnsActiveRemaining <= 0)
        {
            return [];
        }

        if (gameEvent is GameEvent_TurnBegin turnBegin && turnBegin.PlayerGameId == player.GamePlayerId)
        {
            playerTech.TurnsActiveRemaining--;
        }
        else if (gameEvent is GameEvent_PreMove preMove)
        {
            bool? isAttacker = null;
            if (preMove.MovingPlayerId == player.GamePlayerId)
            {
                isAttacker = true;
            }
            else if (game.GetHexAt(preMove.Destination).Planet!.OwningPlayerId == player.GamePlayerId)
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
                        ResolveInteractionData = new ApplyLiveFireExerciseCombatBonusInteraction
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

    public async Task<InteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyLiveFireExerciseCombatBonusInteraction interactionData,
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

        return new InteractionOutcome(true);
    }
}
