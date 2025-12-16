using System.Collections.Generic;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Tech;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech.CulturalExchange;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_CulturalExchange : Tech, IInteractionHandler<SelectCulturalExchangeTargetPlayerInteraction>, IInteractionHandler<UseCulturalExchangeInteraction>
{
    public Tech_CulturalExchange() : base("culturalExchange", "Cultural Exchange",
        "Swap this tech for one of another player's techs. The exhaustion status of both techs is preserved. The target player must not own Cultural Exchange already. Does not cycle the tech market.",
        "Why yes, we would love to observe your traditional uranium enrichment ceremony.",
        [TechKeyword.FreeAction, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    private IEnumerable<GamePlayer> GetTargetablePlayers(Game game, GamePlayer user)
        => game.Players.Except(user).Where(x => x.TryGetPlayerTechById(Id) == null && GetTargetableTechs(game, user, x).Any());
    
    private static IEnumerable<PlayerTech> GetTargetableTechs(Game game, GamePlayer user, GamePlayer target)
        => target.Techs.Where(x => user.Techs.All(y => x.TechId != y.TechId));

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player)
        => base.IsSimpleActionAvailable(game, player) && GetTargetablePlayers(game, player).Any();

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targetPlayers = GetTargetablePlayers(game, player).ToList();

        if (targetPlayers.Count == 0)
        {
            builder.AppendContentNewline("No suitable targets");
            return builder;
        }

        var interactionIds = serviceProvider.AddInteractionsToSetUp(targetPlayers.Select(x => new SelectCulturalExchangeTargetPlayerInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId,
            TargetPlayerId = x.GamePlayerId
        }));

        var cancelId = serviceProvider.AddInteractionToSetUp(new RepromptInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId
        });

        builder.AppendContentNewline(
            $"{await player.GetNameAsync(true)}, choose a player whose very culturally significant weapons technologies you would like to respectfully learn more about:");
        await builder.AppendPlayerButtonsAsync(targetPlayers, interactionIds, cancelId);
        
        return builder;
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        SelectCulturalExchangeTargetPlayerInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        var targetPlayer = game.GetGamePlayerByGameId(interactionData.TargetPlayerId);
        
        var targetableTechs = GetTargetableTechs(game, game.GetGamePlayerForInteraction(interactionData), targetPlayer)
            .ToList();

        if (targetableTechs.Count == 0)
        {
            builder!.AppendContentNewline($"No targetable techs.");
            return new SpaceWarInteractionOutcome(false);
        }

        var interactionIds = serviceProvider.AddInteractionsToSetUp(targetableTechs.Select(x =>
            new UseCulturalExchangeInteraction
            {
                ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
                TechId = x.TechId,
                TargetGamePlayerId = targetPlayer.GamePlayerId,
                Game = game.DocumentId
            }));

        var cancelId = serviceProvider.AddInteractionToSetUp(new RepromptInteraction
        {
            ForGamePlayerId = game.CurrentTurnPlayer.GamePlayerId,
            Game = game.DocumentId
        });
        
        builder!.AppendContentNewline($"{await player.GetNameAsync(true)}, choose a fascinating and incidentally militarily important facet of {await targetPlayer.GetNameAsync(false)}'s culture to observe:");
        builder.AppendButtonRows(cancelId, interactionIds.Zip(targetableTechs)
            .Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary,
                x.First,
                TechsById[x.Second.TechId].DisplayName + (x.Second.IsExhausted ? " [exhausted]" : ""))));

        return new SpaceWarInteractionOutcome(true);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, UseCulturalExchangeInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        var playerCulturalExchangeTech = GetThisTech(player);
        var targetPlayer = game.GetGamePlayerByGameId(interactionData.TargetGamePlayerId);
        
        var targetPlayerTech = targetPlayer.TryGetPlayerTechById(interactionData.TechId);

        if (targetPlayer.TryGetPlayerTechById(Id) != null
            || player.TryGetPlayerTechById(interactionData.TechId) != null
            || targetPlayerTech == null)
        {
            return new SpaceWarInteractionOutcome(false);
        }
        
        // Don't use the standard gain/lose tech events as we are doing a special swap that preserves exhaustion status
        
        var targetTech = TechsById[interactionData.TechId];
        var gainedPlayerTech = targetTech.CreatePlayerTech(game, player);
        gainedPlayerTech.IsExhausted = targetPlayerTech.IsExhausted;

        player.Techs.Remove(playerCulturalExchangeTech);
        targetPlayer.Techs.Remove(targetPlayerTech);

        var gainedCulturalExchangePlayerTech = CreatePlayerTech(game, player);
        gainedCulturalExchangePlayerTech.IsExhausted = true;
        
        builder?.AppendContentNewline(
            $"{await player.GetNameAsync(false)} acquires {targetTech.DisplayName} from {await targetPlayer.GetNameAsync(true)} as part of a cultural exchange!");

        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_PlayerGainTech
            {
                PlayerGameId = player.GamePlayerId,
                TechId = interactionData.TechId,
                CycleMarket = false,
                PlayerTech = gainedPlayerTech
            },
            new GameEvent_PlayerGainTech
            {
                PlayerGameId = targetPlayer.GamePlayerId,
                TechId = Id,
                CycleMarket = false,
                PlayerTech = gainedCulturalExchangePlayerTech
            });
        
        return new SpaceWarInteractionOutcome(true);
    }
}