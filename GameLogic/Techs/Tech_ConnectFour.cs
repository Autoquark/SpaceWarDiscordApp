using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_ConnectFour : Tech
{
    public Tech_ConnectFour() : base("connect_four", "Connect Four",
        "If you control 4 planets in a straight line (not using any hyperlane connections), gain 1VP",
        "Sir, are you taking this entirely seriously?",
        [TechKeyword.FreeAction, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) {
        if (!base.IsSimpleActionAvailable(game, player))
        {
            return false;
        }

        var ownedHexes = game.Hexes.WhereOwnedBy(player).ToList();

        // For each owned hex, pick a direction and move in it until we get to 4 or don't
        foreach (var startingHex in ownedHexes)
        {
            foreach (var hexDirection in Enum.GetValues<HexDirection>())
            {
                var coordinates = startingHex.Coordinates;
                for (int step = 2; step <= 4; step++)
                {
                    coordinates += hexDirection;
                    var neighbour = game.TryGetHexAt(coordinates);
                    if (neighbour != null && neighbour.Planet?.OwningPlayerId == player.GamePlayerId)
                    {
                        if (step == 4) { 
                            return true;
                        }
                    } else
                    {
                        break;
                    }
                }
            }
        }
        return false;
    }

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} gains 1 VP via Connect Four!");

        player.VictoryPoints++;
        player.GetPlayerTechById(Id).IsExhausted = true;
        await GameFlowOperations.CheckForVictoryAsync(builder, game);
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });

        return builder;
    }
}