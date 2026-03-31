using SpaceWarDiscordApp.Database;

namespace SpaceWarDiscordApp.GameLogic;

public interface IPlayerChoiceEventHandler<TEvent, TInteractionData>
    : IPlayerChoiceEventHandler<TEvent, TInteractionData, Game>
    where TEvent : GameEvent_PlayerChoice<TInteractionData>
    where TInteractionData : InteractionData
{
}
