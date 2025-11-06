using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.Tech_MaterialRepurposing;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_MaterialRepurposing : Tech, IInteractionHandler<UseMaterialRepurposingInteraction>
{
    public Tech_MaterialRepurposing() : base("material-repurposing", "Material Repurposing",
        "When you capture a planet, if it is ready, produce there (then exhaust it as normal)",
        "It turns out the main difference between our battle ships and theirs is the paint job.",
        ["Exhaust"])
    {
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_CapturePlanet capturePlanet)
        {
            var hex = game.GetHexAt(capturePlanet.Location);
            if (hex.Planet is { IsExhausted: false } && hex.Planet.OwningPlayerId == player.GamePlayerId)
            {
                return
                [
                    new TriggeredEffect
                    {
                        AlwaysAutoResolve = false,
                        IsMandatory = false,
                        DisplayName = $"{DisplayName}: Produce on {hex.Coordinates}",
                        ResolveInteractionData = new UseMaterialRepurposingInteraction
                        {
                            Game = game.DocumentId,
                            ForGamePlayerId = player.GamePlayerId,
                            Event = capturePlanet,
                            EventDocumentId = capturePlanet.DocumentId!,
                        }
                    }
                ];
            }
        }

        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        UseMaterialRepurposingInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var hex = game.GetHexAt(interactionData.Event.Location);
        if (hex.Planet is { IsExhausted: false, OwningPlayerId: not -1 })
        {
            var player = game.GetGamePlayerForInteraction(interactionData);
            GetThisTech(player).IsExhausted = true;
            builder?.AppendContentNewline($"{await player.GetNameAsync(false)} is using {DisplayName} to produce on {hex.ToCoordsWithDieEmoji(game)}");
            await GameFlowOperations.PushGameEventsAsync(game,
                ProduceOperations.CreateProduceEvent(game, hex.Coordinates));
        }
        
        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true, builder);
    }
}