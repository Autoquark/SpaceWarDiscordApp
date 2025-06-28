using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData.Tech.EnervatorBeam;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_EnervatorBeam : Tech, IInteractionHandler<UseEnervatorBeamInteraction>
{
    public Tech_EnervatorBeam() : base("enervatorBeam",
        "Enervator Beam",
        "Free Action, Exhaust: Exhaust any planet",
        "Does anybody else feel tired today?")
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    protected override bool IsSimpleActionAvailable(Game game, GamePlayer player)
        => base.IsSimpleActionAvailable(game, player) && game.Hexes.Any(x => x.Planet?.IsExhausted == false);

    public override async Task<TBuilder> UseTechActionAsync<TBuilder>(TBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = game.Hexes.Where(x => x.Planet?.IsExhausted == false)
            .ToList();

        var interactionIds = await InteractionsHelper.SetUpInteractionsAsync(targets.Select(x =>
            new UseEnervatorBeamInteraction
            {
                ForGamePlayerId = player.GamePlayerId,
                Game = game.DocumentId,
                Target = x.Coordinates,
                EditOriginalMessage = true
            }), serviceProvider.GetRequiredService<SpaceWarCommandContextData>().GlobalData.InteractionGroupId);

        builder.AppendContentNewline("Enervator Beam: Choose a planet to exhaust:");
        
        return builder.AppendHexButtons(game, targets, interactionIds);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync<TBuilder>(TBuilder builder,
        UseEnervatorBeamInteraction interactionData, Game game, IServiceProvider serviceProvider) where TBuilder : BaseDiscordMessageBuilder<TBuilder>
    {
        game.GetHexAt(interactionData.Target).Planet!.IsExhausted = true;
        var player = game.GetGamePlayerForInteraction(interactionData);
        var name = await player.GetNameAsync(false);
        
        builder.AppendContentNewline($"{name} has exhausted {interactionData.Target} using Enervator Beam");
        
        player.GetPlayerTechById(Id).IsExhausted = true;
        
        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Free, serviceProvider);
        
        return new SpaceWarInteractionOutcome(true, builder);
    }
}