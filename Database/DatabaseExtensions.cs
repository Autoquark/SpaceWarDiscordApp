using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

public static class DatabaseExtensions
{
    public static CollectionReference Games(this FirestoreDb db) => db.Collection("Games");
    public static CollectionReference PlayerTechs(this FirestoreDb db) => db.Collection("PlayerTechs");
    public static CollectionReference ActionRecords(this FirestoreDb db) => db.Collection("ActionRecords");
    public static CollectionReference InteractionData(this FirestoreDb db) => db.Collection("InteractionData");
}