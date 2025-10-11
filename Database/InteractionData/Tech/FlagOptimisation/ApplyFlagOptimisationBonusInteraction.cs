using SpaceWarDiscordApp.Database.GameEvents;

namespace SpaceWarDiscordApp.Database.InteractionData.Tech.FlagOptimisation;

public class ApplyFlagOptimisationBonusInteraction : TriggeredEffectInteractionData
{
    public required GameEvent_PreMove Event { get; set; }
    public required bool IsAttacker { get; set; }
}