using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

/// <summary>
/// Base class for non-root firestore data model classes that can be saved and restored polymorphically]
/// i.e. preserving their runtime type.
/// </summary>
[FirestoreData]
public abstract class PolymorphicFirestoreModel : IPolymorphicFirestoreData
{
    protected PolymorphicFirestoreModel()
    {
        SubtypeName = GetType().FullName!;
    }
    
    [FirestoreProperty]
    public string SubtypeName { get; set; }
}