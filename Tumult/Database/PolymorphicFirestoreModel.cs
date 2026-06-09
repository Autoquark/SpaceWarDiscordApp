using Google.Cloud.Firestore;

namespace Tumult.Database;

/// <summary>
/// Base class for non-root firestore data model classes that can be saved and restored polymorphically
/// i.e. preserving their runtime type.
/// </summary>
[FirestoreData]
public abstract class PolymorphicFirestoreModel : IPolymorphicFirestoreData
{
    protected PolymorphicFirestoreModel()
    {
        SubtypeName = GetType().AssemblyQualifiedName!;
    }

    [FirestoreProperty]
    public string SubtypeName { get; set; }
}
