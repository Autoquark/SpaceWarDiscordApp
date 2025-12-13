using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.GameLogic;

public static class GameModelExtensions
{
    public static (BoardHex hex, GamePlayer player) GetProducingHexAndPlayer(this GameEvent_BeginProduce produceEvent,
        Game game)
    {
        var hex = game.GetHexAt(produceEvent.Location);
        return produceEvent.OverrideProducingPlayerId.HasValue
            ? (hex, game.GetGamePlayerByGameId(produceEvent.OverrideProducingPlayerId.Value))
            : (hex, game.GetGamePlayerByGameId(hex.Planet!.OwningPlayerId));
    }
}