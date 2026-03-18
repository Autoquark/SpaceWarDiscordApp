using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Tech;

[FirestoreData]
public class PlayerTech_DisposableTroops : PlayerTech
{
    /// <summary>
    /// Hexes that we have produced on this turn that we are waiting to destroy forces on.
    /// Main purpose of this mechanism is to allow us to avoid destroying forces when the player only gained disposable troops off
    /// this produce (and therefore didn't create extra troops), but theoretically this safeguards against any scenario where we somehow
    /// manage to bypass applying the disposable troops bonus
    /// </summary>
    [FirestoreProperty]
    public List<HexCoordinates> PendingDestroy { get; set; } = [];
}