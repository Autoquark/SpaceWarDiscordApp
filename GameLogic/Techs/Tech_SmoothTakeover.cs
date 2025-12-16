using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.SmoothTakeover;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_SmoothTakeover : Tech, IInteractionHandler<SmoothTakeoverRefreshInteraction>
{
    public Tech_SmoothTakeover() : base(
        "smooth-takeover", "Smooth Takeover", "When you capture an exhausted planet, ready it.",
        "It seems the previous reigime were in the habit of using your people for cheap unskilled labour. Well, good news! We also have a need for that!")
    {
        
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_CapturePlanet capturePlanet)
        {
            var hex = game.GetHexAt(capturePlanet.Location);
            if (hex.Planet is { IsExhausted: true } planet && planet.OwningPlayerId == player.GamePlayerId)
            {
                return
                [
                    new TriggeredEffect
                    {
                        AlwaysAutoResolve = true,
                        IsMandatory = true,
                        DisplayName = DisplayName,
                        ResolveInteractionData = new SmoothTakeoverRefreshInteraction
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

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, SmoothTakeoverRefreshInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Event.Location);
        if (hex.Planet is { IsExhausted: true, OwningPlayerId: not -1})
        {
            var newOwner = game.GetGamePlayerByGameId(hex.Planet.OwningPlayerId);
            builder?.AppendContentNewline($"{await newOwner.GetNameAsync(false)} performs a smooth takeover and refreshes {hex.ToHexNumberWithDieEmoji(game)}");

            hex.Planet.IsExhausted = false;
        }

        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        return new SpaceWarInteractionOutcome(true);
    }
}