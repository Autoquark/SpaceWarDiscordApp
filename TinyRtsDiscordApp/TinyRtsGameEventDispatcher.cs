using Google.Cloud.Firestore;
using TinyRtsDiscordApp.Database;
using Tumult.Database;
using Tumult.Database.GameEvents;
using Tumult.Discord;
using Tumult.GameLogic;

namespace TinyRtsDiscordApp;

public class TinyRtsGameEventDispatcher : GameEventDispatcher<Game>
{
    public TinyRtsGameEventDispatcher(FirestoreDb firestoreDb) : base(firestoreDb)
    {
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsForPlayer(Game game, GameEvent gameEvent, BaseGamePlayer player)
    {
        return [];
    }

    protected override IEnumerable<int> GetPlayerIdsToResolveTriggersFor(Game game)
    {
        return [];
    }

    protected override Task<DiscordMultiMessageBuilder?> OnEventStackEmptyAsync(DiscordMultiMessageBuilder? builder, Game game, IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }

    protected override Task ShowTriggeredEffectsChoiceAsync(DiscordMultiMessageBuilder? builder, GameEvent resolvingEvent, Game game,
        IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }
}