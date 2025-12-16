using System.Diagnostics.CodeAnalysis;
using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.GameEvents.Refresh;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public class RefreshOperations : IEventResolvedHandler<GameEvent_FullRefresh>, IEventResolvedHandler<GameEvent_TechRefreshed>
{
    public async Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_FullRefresh gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerByGameId(gameEvent.GamePlayerId);
        var name = await player.GetNameAsync(false);
        builder?.AppendContentNewline($"{name} is refreshing");

        var refreshedHexes = new HashSet<BoardHex>();
        foreach (var hex in game.Hexes.Where(x => x.Planet?.OwningPlayerId == player.GamePlayerId))
        {
            if (hex.Planet?.IsExhausted == true)
            {
                hex.Planet.IsExhausted = false;
                refreshedHexes.Add(hex);
            }
        }

        var refreshedTechs = new List<PlayerTech>();
        foreach (var playerTech in player.Techs)
        {
            if (playerTech.IsExhausted)
            {
                playerTech.IsExhausted = false;
                refreshedTechs.Add(playerTech);
                GameFlowOperations.PushGameEvents(game, new GameEvent_TechRefreshed
                {
                    TechId = playerTech.TechId,
                    PlayerGameId = player.GamePlayerId
                });
            }
        }

        if (game.CurrentTurnPlayer == player)
        {
            player.CurrentTurnEvents.AddRange(refreshedHexes.Select(x => new RefreshPlanetEventRecord
            {
                Coordinates = x.Coordinates
            }));
        }

        if (refreshedHexes.Count > 0)
        {
            builder?.AppendContentNewline("Refreshed planets: " + string.Join(", ", refreshedHexes.Select(x => x.Coordinates)));
        }

        if (refreshedTechs.Count > 0)
        {
            builder?.AppendContentNewline("Refreshed techs: " +
                                          string.Join(", ",
                                              refreshedTechs.Select(x => Tech.TechsById[x.TechId].DisplayName)));
        }
        
        if(refreshedTechs.Count + refreshedHexes.Count == 0)
        {
            builder?.AppendContentNewline("Nothing to refresh!");
        }

        return builder;
    }

    public Task<DiscordMultiMessageBuilder?> HandleEventResolvedAsync(DiscordMultiMessageBuilder? builder, GameEvent_TechRefreshed gameEvent, Game game,
        IServiceProvider serviceProvider)
    {
        // Don't need to do anything, this event is just for triggers
        return Task.FromResult(builder);
    }
}