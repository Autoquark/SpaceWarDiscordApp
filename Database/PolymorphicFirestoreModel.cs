using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

/// <summary>
/// Subclasses of this can be saved and restored polymorphically i.e. preserving their runtime type.
/// This is not possible for nested classes, however. They must be the root documents of a collection.
/// </summary>
[FirestoreData]
public abstract class PolymorphicFirestoreModel : FirestoreModel
{
    protected PolymorphicFirestoreModel()
    {
        SubtypeName = GetType().FullName!;
    }
    
    [FirestoreProperty]
    public string SubtypeName { get; set; }
}