using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData;
using SpaceWarDiscordApp.Database.InteractionData.Tech.VolunteerTesters;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_VolunteerTesters : Tech, IInteractionHandler<SetVolunteerTestersTargetInteraction>,
    IInteractionHandler<UseVolunteerTestersInteraction>
{
    public Tech_VolunteerTesters() : base("volunteerTesters",
        "'Volunteer' Testers",
        "Destroy any number of your forces on a planet, gain 1 $science$ per forces destroyed.",
        "Hold the experimental device such that the aperture is pointed directly towards your forehead and press the red button on the top. Now, anybody who is still alive, please raise a hand. No? Excellent.",
        [TechKeyword.FreeAction, TechKeyword.Exhaust])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
        IncludeInGames = false; // OP, disabling for now. Not sure if I want techs that grant science points in this way at all
    }

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = game.Hexes.WhereOwnedBy(player).ToList();
        var name = await player.GetNameAsync(true);
        
        builder.AppendContentNewline($"{name}, choose a planet to target:");
        
        var interactions = serviceProvider.AddInteractionsToSetUp(targets.Select(x => new SetVolunteerTestersTargetInteraction
        {
            ForGamePlayerId = player.GamePlayerId,
            Game = game.DocumentId,
            Target = x.Coordinates
        }));
        
        return builder.AppendHexButtons(game, targets, interactions);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        SetVolunteerTestersTargetInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var player = game.GetGamePlayerForInteraction(interactionData);
        var hex = game.GetHexAt(interactionData.Target);
        var interactions = serviceProvider.AddInteractionsToSetUp(Enumerable.Range(1, hex.Planet!.ForcesPresent)
            .Select(x => new UseVolunteerTestersInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Target = interactionData.Target,
                Amount = x
            }));

        var name = await player.GetNameAsync(true);
        builder.AppendContentNewline($"{name}, choose how many forces will 'volunteer'")
            .AppendButtonRows(Enumerable.Range(1, hex.Planet.ForcesPresent)
                .Zip(interactions)
                .Select(x => new DiscordButtonComponent(DiscordButtonStyle.Primary, x.Second, x.First.ToString())));

        return new SpaceWarInteractionOutcome(false);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        UseVolunteerTestersInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var hex = game.GetHexAt(interactionData.Target);

        GameFlowOperations.DestroyForces(game, hex, 1, interactionData.ForGamePlayerId, ForcesDestructionReason.Tech);
        var player = game.GetGamePlayerForInteraction(interactionData);
        
        var name = await player.GetNameAsync(false);

        builder.AppendContentNewline(
            $"{interactionData.Amount} of {name}'s forces on {interactionData.Target} have been converted into pure Science");
        
        var tech = player.GetPlayerTechById(Id);
        tech.IsExhausted = true;
        
        player.CurrentTurnEvents.Add(new PlanetTargetedTechEventRecord
        {
            Coordinates = hex.Coordinates
        });
        
        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_PlayerGainScience
            {
                PlayerGameId = player.GamePlayerId,
                Amount = interactionData.Amount,
            },
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType
            });
        
        // I guess you can eliminate yourself with this, if you want to...
        await GameFlowOperations.CheckForPlayerEliminationsAsync(builder, game);
        
        return new SpaceWarInteractionOutcome(true);
    }
}