using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database.GameEvents;

[FirestoreData]
public abstract class GameEvent_PlayerChoice : GameEvent { }

/// <summary>
/// Event that requires a player choice to resolve, where their selection is represented as a TInteractionData
/// </summary>
[FirestoreData]
public abstract class GameEvent_PlayerChoice<TInteractionData> : GameEvent_PlayerChoice where TInteractionData : InteractionData.InteractionData
{
    
}