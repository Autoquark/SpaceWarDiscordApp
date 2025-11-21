using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.Converters;

public class TypeDiscriminator : IFirestoreTypeDiscriminator<IPolymorphicFirestoreData>, IFirestoreTypeDiscriminator<PlayerTech>,
    IFirestoreTypeDiscriminator<GameEvent>, IFirestoreTypeDiscriminator<EventRecord>, IFirestoreTypeDiscriminator<InteractionData.InteractionData>
    
{
    public Type GetConcreteType(IDictionary<string, Value> map)
        => Type.GetType(map[nameof(IPolymorphicFirestoreData.SubtypeName)].StringValue)!;
}