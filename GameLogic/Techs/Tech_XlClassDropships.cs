using DSharpPlus.Entities;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.MassMigration;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_XlClassDropships : Tech, IInteractionHandler<ApplyMassMigrationBonusInteraction>
{
    public Tech_XlClassDropships() : base("xl-class-dropships", "XL Class Dropships",
        "When attacking, if you moved all of your forces that were present on at least one planet, +1 Combat Strength.",
        "Personnel of rank sergeant and below should seat themselves in the economy section.")
    {
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        if (gameEvent is GameEvent_PreMove preMove
            && preMove.MovingPlayerId == player.GamePlayerId
            && preMove.Sources.Any(x => x.Amount == game.GetHexAt(x.Source).ForcesPresent))
        {
            return
            [
                new TriggeredEffect
                {
                    AlwaysAutoResolve = true,
                    IsMandatory = true,
                    DisplayName = DisplayName,
                    ResolveInteractionData = new ApplyMassMigrationBonusInteraction
                    {
                        Event = preMove,
                        EventId = preMove.EventId,
                        ForGamePlayerId = player.GamePlayerId,
                        Game = game.DocumentId
                    },
                    TriggerId = GetTriggerId(0)
                }
            ];
        }

        return [];
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, ApplyMassMigrationBonusInteraction interactionData, Game game,
        IServiceProvider serviceProvider)
    {
        interactionData.Event.AttackerCombatStrengthSources.Add(new CombatStrengthSource
        {
            DisplayName = DisplayName,
            Amount = 1
        });

        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }
}