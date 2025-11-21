using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech.MegaLaser;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_MegaLaser : Tech, IInteractionHandler<FireMegaLaserInteraction>
{
    public Tech_MegaLaser() : base("megaLaser",
        "Mega Laser",
        "Remove all Forces from a planet adjacent to one you control",
        "Building on the shoulders of the Large Space Laser, the Very Large Space Laser and the Truly Humongous Space Laser, we proudly present a bold new answer to the question 'just how large can a space laser be?'",
        ["Action", "Exhaust"])
    {
        HasSimpleAction = true;
    }

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player) =>
        base.IsSimpleActionAvailable(game, player) && game.Hexes
            .WhereOwnedBy(player)
            .SelectMany(x => BoardUtils.GetNeighbouringHexes(game, x))
            .DistinctBy(x => x.Coordinates)
            .Any(x => x.ForcesPresent > 0);

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var planets = game.Hexes.WhereOwnedBy(player)
            .SelectMany(x => BoardUtils.GetNeighbouringHexes(game, x))
            .DistinctBy(x => x.Coordinates)
            .Where(x => x.ForcesPresent > 0)
            .ToList();

        if (planets.Count == 0)
        {
            builder.AppendContentNewline("No suitable targets");
            return builder;
        }
        
        builder.AppendContentNewline("Choose a planet to target:");

        var interactions = serviceProvider.AddInteractionsToSetUp(planets.Select(x => new FireMegaLaserInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId,
            Target = x.Coordinates
        }));
        
        return builder.AppendHexButtons(game, planets, interactions);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        FireMegaLaserInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        player.GetPlayerTechById(Id).IsExhausted = true;
        game.GetHexAt(interactionData.Target).Planet!.SetForces(0);
        player.CurrentTurnEvents.Add(new PlanetTargetedTechEventRecord
        {
            Coordinates = interactionData.Target
        });

        builder?.AppendContentNewline($"All forces on {interactionData.Target} have been destroyed");
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });
        
        return new SpaceWarInteractionOutcome(true);
    }
}