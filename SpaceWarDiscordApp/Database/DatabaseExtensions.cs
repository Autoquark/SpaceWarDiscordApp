using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

public static class DatabaseExtensions
{
    extension(FirestoreDb db)
    {
        public CollectionReference Games() => db.Collection("Games");
        public CollectionReference GameBackups() => db.Collection("GameBackups");
        public CollectionReference PlayerTechs() => db.Collection("PlayerTechs");
        public CollectionReference GameEvents() => db.Collection("GameEvents");
        public CollectionReference EventRecords() => db.Collection("EventRecords");
        public CollectionReference GuildData() => db.Collection("GuildData");
        public DocumentReference GlobalData() => db.Collection("GlobalData").Document("GlobalData");
    }
}