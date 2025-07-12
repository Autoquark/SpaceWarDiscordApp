using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

public static class DatabaseExtensions
{
    public static CollectionReference Games(this FirestoreDb db) => db.Collection("Games");
    public static CollectionReference PlayerTechs(this FirestoreDb db) => db.Collection("PlayerTechs");
    public static CollectionReference GameEvents(this FirestoreDb db) => db.Collection("GameEvents");
    public static CollectionReference EventRecords(this FirestoreDb db) => db.Collection("EventRecords");
    public static CollectionReference InteractionData(this FirestoreDb db) => db.Collection("InteractionData");
    public static DocumentReference GlobalData(this FirestoreDb db) => db.Collection("GlobalData").Document("GlobalData");
}