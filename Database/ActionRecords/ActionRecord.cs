using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.ActionRecords;

[FirestoreData]
public abstract class ActionRecord : PolymorphicFirestoreModel
{
    
}