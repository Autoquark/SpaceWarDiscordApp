using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.GameEvents;
using SpaceWarDiscordApp.Database.InteractionData.Tech;
using SpaceWarDiscordApp.Database.InteractionData.Tech.Biorecyclers;
using SpaceWarDiscordApp.Database.Tech;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.GameEvents;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_Biorecyclers : Tech, IInteractionHandler<PutForcesOnBiorecyclersInteraction>, IInteractionHandler<DeployFromBiorecyclersInteraction>
{
    private const int MaxForces = 6;
    
    public Tech_Biorecyclers() : base("biorecyclers", "Biorecyclers",
        """
        Place all of your forces from this tech onto a planet you control.
        
        Passive: When your forces are destroyed, except due to exceeding planet capacity, place them on this tech, up to a maximum of 6.
        """, "Has anybody seen my legs?",
        [TechKeyword.Action])
    {
        HasSimpleAction = true;
    }

    public override PlayerTech CreatePlayerTech(Game game, GamePlayer player) =>
        new PlayerTech_Biorecyclers
        {
            TechId = Id
        };

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player)
        => base.IsSimpleActionAvailable(game, player) && GetThisTech<PlayerTech_Biorecyclers>(player).Forces > 0;

    public override async Task<string> GetTechStatusLineAsync(Game game, GamePlayer player)
    {
        var tech = GetThisTech<PlayerTech_Biorecyclers>(player);
        return await base.GetTechStatusLineAsync(game, player) + $"{tech.Forces} forces";
    }

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = game.Hexes.WhereOwnedBy(player).ToList();

        if (targets.Count == 0)
        {
            builder.AppendContentNewline("No suitable targets");
            return builder;
        }
        
        builder.AppendContentNewline($"{await player.GetNameAsync(true)}, choose a planet to deploy biorecycled forces:");
        
        var interactionIds = serviceProvider.AddInteractionsToSetUp(targets.Select(x =>
            new DeployFromBiorecyclersInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Location = x.Coordinates
            }));
        
        return builder.AppendHexButtons(game, targets, interactionIds);
    }

    protected override IEnumerable<TriggeredEffect> GetTriggeredEffectsInternal(Game game, GameEvent gameEvent, GamePlayer player)
    {
        var playerTech = GetThisTech<PlayerTech_Biorecyclers>(player);
        
        if (gameEvent is GameEvent_PostForcesDestroyed forcesDestroyed
            && forcesDestroyed.OwningPlayerGameId == player.GamePlayerId
            && forcesDestroyed.Reason != ForcesDestructionReason.ExceedingCapacity
            && playerTech.Forces < MaxForces)
        {
            return
            [
                new TriggeredEffect
                {
                    AlwaysAutoResolve = true,
                    IsMandatory = true,
                    DisplayName = DisplayName,
                    ResolveInteractionData = new PutForcesOnBiorecyclersInteraction
                    {
                        Game = game.DocumentId,
                        ForGamePlayerId = player.GamePlayerId,
                        Event = forcesDestroyed,
                        EventId = forcesDestroyed.EventId
                    },
                    TriggerId = GetTriggerId(0)
                }
            ];
        }

        return [];
    }


    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, PutForcesOnBiorecyclersInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var player = game.GetGamePlayerForInteraction(interactionData);
        var playerTech = GetThisTech<PlayerTech_Biorecyclers>(player);
        
        var toAdd = Math.Min(MaxForces - playerTech.Forces, interactionData.Event.Amount);
        if (toAdd > 0)
        {
            var previous = playerTech.Forces;
            playerTech.Forces += toAdd;
            builder?.AppendContentNewline(
                $"{await player.GetNameAsync(false)} places {toAdd} forces onto {DisplayName} ({previous} -> {playerTech.Forces})");
        }

        await GameFlowOperations.TriggerResolvedAsync(game, builder, serviceProvider, interactionData.InteractionId);
        
        return new SpaceWarInteractionOutcome(true);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, DeployFromBiorecyclersInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        var playerTech = GetThisTech<PlayerTech_Biorecyclers>(game.GetGamePlayerForInteraction(interactionData));
        var hex = game.GetHexAt(interactionData.Location);
        
        hex.Planet!.AddForces(playerTech.Forces);
        
        var player = game.GetGamePlayerByGameId(interactionData.ForGamePlayerId);
        var name = await player.GetNameAsync(false);
        builder?.AppendContentNewline($"{name} added {playerTech.Forces} recycled forces to {hex.Coordinates}");
        
        playerTech.Forces = 0;
        
        ProduceOperations.CheckPlanetCapacity(game, hex);

        await GameFlowOperations.PushGameEventsAndResolveAsync(builder, game, serviceProvider,
            new GameEvent_ActionComplete
            {
                ActionType = SimpleActionType,
            });
        
        return new SpaceWarInteractionOutcome(true);
    }
}