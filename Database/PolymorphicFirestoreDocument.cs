using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

/// <summary>
/// Base class for root documents that can be saved and restored polymorphically i.e. preserving their runtime type.
/// </summary>
[FirestoreData]
public abstract class PolymorphicFirestoreDocument : FirestoreDocument, IPolymorphicFirestoreData
{
    protected PolymorphicFirestoreDocument()
    {
        SubtypeName = GetType().FullName!;
    }
    
    [FirestoreProperty]
    public string SubtypeName { get; set; }
}