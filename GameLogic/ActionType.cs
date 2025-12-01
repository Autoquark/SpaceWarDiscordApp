using SpaceWarDiscordApp.GameLogic.Techs;

namespace SpaceWarDiscordApp.GameLogic;

public enum ActionType
{
    Main,
    Free
}

public static class ActionTypeExtensions
{
    public static TechKeyword GetCorrespondingKeyword(this ActionType actionType) => actionType switch
    {
        ActionType.Main => TechKeyword.Action,
        ActionType.Free => TechKeyword.FreeAction,
        _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null)
    };
}