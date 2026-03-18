using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.EventRecords;

[FirestoreData]
public abstract class EventRecord : PolymorphicFirestoreModel
{
    
}