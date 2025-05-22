using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

public static class DocumentSnapShotExtensions
{
    /// <summary>
    /// Converts the snapshot dynamically to the class it was originally saved as
    /// </summary>
    /// <typeparam name="T">Type to cast to when returning</typeparam>
    public static T ConvertToPolymorphic<T>(this DocumentSnapshot snapshot) where T : PolymorphicFirestoreModel
    {
        var typeName = snapshot.GetValue<string>(nameof(PolymorphicFirestoreModel.SubtypeName));
        var type = Type.GetType(typeName);
        
        if (type == null)
        {
            throw new Exception($"Type {typeName} not found");
        }
        
        return (T)typeof(DocumentSnapshot).GetMethod(nameof(DocumentSnapshot.ConvertTo))!.MakeGenericMethod(type)
            .Invoke(snapshot, [])!;
    }
}