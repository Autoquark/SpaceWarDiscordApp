using Tumult.Database;
using Tumult.Database.GameEvents;

namespace TinyRtsDiscordApp.Database;

public class Game : BaseGame
{
    public override List<GameEvent> EventStack { get; set; } = [];
    public override IReadOnlyList<BaseGamePlayer> GamePlayers { get; } = [];
}