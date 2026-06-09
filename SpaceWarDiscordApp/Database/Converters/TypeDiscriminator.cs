using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.EventRecords;
using Tumult.Database.Converters;

namespace SpaceWarDiscordApp.Database.Converters;

public class TypeDiscriminator : TypeDiscriminatorBase,
    IFirestoreTypeDiscriminator<IPolymorphicFirestoreData>,
    IFirestoreTypeDiscriminator<PlayerTech>,
    IFirestoreTypeDiscriminator<GameEvent>, IFirestoreTypeDiscriminator<EventRecord>,
    IFirestoreTypeDiscriminator<InteractionData>
{
    protected override IReadOnlyDictionary<string, string> TypeMappings { get; } = new Dictionary<string, string>
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
}
