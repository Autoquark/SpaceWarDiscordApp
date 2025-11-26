using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.ImprovisedWeaponry;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_ImprovisedWeaponry : Tech, IInteractionHandler<ImprovisedWeaponryAddForcesInteraction>
{
    public Tech_ImprovisedWeaponry() : base("improvised-weaponry", "Improvised Weaponry",
        "After you capture a planet from another player, add 1 forces to that planet. Don't exhaust it.",
        "This is a traditional weapon used to hunt Necrolisks on Raptulon-5. Careful, it's filled with naturally occuring nitroglycerin.")
    {
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_CapturePlanet capturePlanet)
        {
            var hex = game.GetHexAt(capturePlanet.Location);
            if (capturePlanet.FormerOwnerGameId != GamePlayer.GamePlayerIdNone && hex.Planet != null && hex.Planet.OwningPlayerId == player.GamePlayerId)
            {
                return
                [
                    new TriggeredEffect
                    {
                        AlwaysAutoResolve = true,
                        IsMandatory = true,
                        DisplayName = DisplayName,
                        ResolveInteractionData = new ImprovisedWeaponryAddForcesInteraction
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            Event = capturePlanet,
                            EventId = capturePlanet.EventId
                        },
                        TriggerId = GetTriggerId(0)
                    }
                ];
            }
        }

        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ImprovisedWeaponryAddForcesInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Event.Location);
        
        if (hex.Planet is { OwningPlayerId: not -1 })
        {
            var player = game.GetGamePlayerForInteraction(interactionData);
            hex.Planet.AddForces(1);
            builder?.AppendContentNewline(
                $"{await player.GetNameAsync(false)} adds 1 forces to {hex.ToHexNumberWithDieEmoji(game)} with {DisplayName}");
        }
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        return new SpaceWarInteractionOutcome(true);
    }
}