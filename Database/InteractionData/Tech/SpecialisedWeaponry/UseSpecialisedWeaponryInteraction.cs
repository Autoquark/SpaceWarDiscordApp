using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.SpecialisedWeaponry;

[FirestoreData]
public class UseSpecialisedWeaponryInteraction : InteractionData
{
    [FirestoreProperty]
    public required int TargetGamePlayerId { get; set; }
}