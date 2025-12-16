using System.Reflection;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.Converters;

public class TypeDiscriminator : IFirestoreTypeDiscriminator<IPolymorphicFirestoreData>,
    IFirestoreTypeDiscriminator<PlayerTech>,
    IFirestoreTypeDiscriminator<GameEvent>, IFirestoreTypeDiscriminator<EventRecord>,
    IFirestoreTypeDiscriminator<InteractionData.InteractionData>
{
    private static readonly Dictionary<string, string> TypeMappings = new()
    {
        {
            "SpaceWarDiscordApp.Database.GameEvents.GameEvent_BeginProduce",
            "SpaceWarDiscordApp.Database.GameEvents.Produce.GameEvent_BeginProduce"
        },
        {
            "SpaceWarDiscordApp.Database.GameEvents.GameEvent_PostProduce",
            "SpaceWarDiscordApp.Database.GameEvents.Produce.GameEvent_PostProduce"
        },
        {
            "SpaceWarDiscordApp.Database.GameEvents.GameEvent_MovementFlowComplete",
            "SpaceWarDiscordApp.Database.GameEvents.Movement.GameEvent_MovementFlowComplete"
        },
        {
            "SpaceWarDiscordApp.Database.GameEvents.GameEvent_PreMove",
            "SpaceWarDiscordApp.Database.GameEvents.Movement.GameEvent_PreMove"
        },
        {
            "SpaceWarDiscordApp.Database.GameEvents.GameEvent_FullRefresh",
            "SpaceWarDiscordApp.Database.GameEvents.Refresh.GameEvent_PreMove"
        },
        {
            "SpaceWarDiscordApp.Database.GameEvents.GameEvent_TechRefreshed",
            "SpaceWarDiscordApp.Database.GameEvents.Refresh.GameEvent_TechRefreshed"
        },
        {
            "SpaceWarDiscordApp.Database.GameEvents.GameEvent_TechPurchaseDecision",
            "SpaceWarDiscordApp.Database.GameEvents.Tech.GameEvent_TechPurchaseDecision"
        },
    };
    
    public Type GetConcreteType(IDictionary<string, Value> map)
    {
        var descriptor = map[nameof(IPolymorphicFirestoreData.SubtypeName)].StringValue;
        var temp = Type.GetType(descriptor);
        if (temp == null)
        {
            var backtickIndex = descriptor.IndexOf('`');
            if (backtickIndex == -1)
            {
                backtickIndex = descriptor.Length;
            }
            
            var beforeBacktick = descriptor[..backtickIndex];
            var afterBacktick = descriptor[backtickIndex..];
            return Type.GetType(TypeMappings[beforeBacktick] + afterBacktick)!;
        }

        return temp;
    }
}