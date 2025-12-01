using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.AggressiveWasteDisposal;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_AggressiveWasteDisposal : Tech, IInteractionHandler<UseAggressiveWasteDisposalInteraction>,
    IInteractionHandler<RefreshAggressiveWasteDisposalInteraction>
{
    public Tech_AggressiveWasteDisposal() : base("aggressiveWasteDisposal",
        "Aggressive Waste Disposal",
        "Destroy 1 forces on a planet adjacent to one you control.\nRefresh this whenever you take the produce action",
        "A primitive civilisation like theirs will probably appreciate these thousand ton containers of miscellaneous industrial refuse!",
        [TechKeyword.FreeAction, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        CheckTriggersWhenExhausted = true;
    }

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) => base.IsSimpleActionAvailable(game, player) && GetTargets(game, player).Any();

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = GetTargets(game, player).ToList();

        if (targets.Count == 0)
        {
            builder.AppendContentNewline("No targets available");
        }

        builder.AppendContentNewline("Choose where to aggressively dispose waste:");

        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x =>
            new UseAggressiveWasteDisposalInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                EditOriginalMessage = true,
                Target = x.Coordinates
            }));
        
        return builder.AppendHexButtons(game, targets, interactionIds);
    }

    private static IEnumerable<BoardHex> GetTargets(Game game, GamePlayer player) => game.Hexes.WhereOwnedBy(player)
        .SelectMany(x => BoardUtils.GetNeighbouringHexes(game, x))
        .WhereForcesPresent()
        .DistinctBy(x => x.Coordinates);

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        UseAggressiveWasteDisposalInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Target);
        var player = game.GetGamePlayerForInteraction(interactionData);
        var tech = player.GetPlayerTechById(Id);

        if (!hex.AnyForcesPresent || tech.IsExhausted)
        {
            throw new Exception();
        }
        
        GameFlowOperations.DestroyForces(game, hex, 1, player.GamePlayerId, ForcesDestructionReason.Tech, Id);
        tech.IsExhausted = true;
        
        var name = await player.GetNameAsync(false);
        builder?.AppendContentNewline($"{name} removed 1 forces from {hex.Coordinates} using Aggressive Waste Disposal.");
        
        player.CurrentTurnEvents.Add(new PlanetTargetedTechEventRecord
        {
            Coordinates = hex.Coordinates
        });
        
        await GameFlowOperations.CheckForPlayerEliminationsAsync(builder, game);
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });

        return new SpaceWarInteractionOutcome(true);
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_PostProduce postProduce && postProduce.PlayerGameId == player.GamePlayerId)
        {
            return
            [
                new TriggeredEffect
                {
                    AlwaysAutoResolve = true,
                    IsMandatory = true,
                    DisplayName = DisplayName,
                    ResolveInteractionData = new RefreshAggressiveWasteDisposalInteraction()
                    {
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                    },
                    TriggerId = GetTriggerId(0)
                }
            ];
        }
        
        return [];
    }
    
    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        RefreshAggressiveWasteDisposalInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        game.GetGamePlayerForInteraction(interactionData).GetPlayerTechById(Id).IsExhausted = false;
        builder?.AppendContentNewline($"{DisplayName} has been refreshed!");
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        return new SpaceWarInteractionOutcome(true);
    }
}