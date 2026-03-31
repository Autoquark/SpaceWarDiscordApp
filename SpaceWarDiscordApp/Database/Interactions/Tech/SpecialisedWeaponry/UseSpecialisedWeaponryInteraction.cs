using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.Interactions.Tech.SpecialisedWeaponry;

[FirestoreData]
public class UseSpecialisedWeaponryInteraction : InteractionData
{
    [FirestoreProperty]
    public required int TargetGamePlayerId { get; set; }
}