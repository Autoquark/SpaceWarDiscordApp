using Microsoft.Extensions.DependencyInjection;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.InteractionData.Tech.DimensionalOrigami;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.Discord.Commands;
using SpaceWarDiscordApp.GameLogic.Operations;

namespace SpaceWarDiscordApp.GameLogic.Techs;

public class Tech_DimensionalOrigami : Tech, IInteractionHandler<ChooseFirstDimensionalOrigamiSystemInteraction>,
    IInteractionHandler<UseDimensionalOrigamiInteraction>
{
    public Tech_DimensionalOrigami() : base("dimensional-origami",
        "Dimensional Origami",
        "Swap the positions of two systems with planets.",
        "If you could see in four dimensions, it would look a bit like a rabbit",
        ["Free Action", "Single Use"])
    {
        HasSimpleAction = true;
        SimpleActionType = ActionType.Free;
    }

    public override async Task<DiscordMultiMessageBuilder> UseTechActionAsync(DiscordMultiMessageBuilder builder, Game game, GamePlayer player,
        IServiceProvider serviceProvider)
    {
        var targets = game.Hexes.Where(x => x.Planet != null).ToList();

        var interactions = serviceProvider.AddInteractionsToSetUp(targets.Select(x =>
            new ChooseFirstDimensionalOrigamiSystemInteraction
            {
                Game = game.DocumentId,
                ForGamePlayerId = player.GamePlayerId,
                Target = x.Coordinates
            }));
        
        var name = await player.GetNameAsync(true);
        builder.NewMessage();
        builder.AppendContentNewline($"{DisplayName}: {name}, choose the first system to swap:");
        builder.AppendHexButtons(game, targets, interactions);
        
        return builder;
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder,
        ChooseFirstDimensionalOrigamiSystemInteraction interactionData, Game game, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        var targets = game.Hexes.Where(x => x.Planet != null && x.Coordinates != interactionData.Target)
            .ToList();
        
        var player = game.GetGamePlayerForInteraction(interactionData);
        
        var interactions = serviceProvider.AddInteractionsToSetUp(targets.Select(x =>
                new UseDimensionalOrigamiInteraction
                {
                    Game = game.DocumentId,
                    ForGamePlayerId = player.GamePlayerId,
                    Target1 = interactionData.Target,
                    Target2 = x.Coordinates,
                    EditOriginalMessage = true
                }));
        
        var name = await player.GetNameAsync(true);
        builder.AppendContentNewline($"{DisplayName}: {name}, choose the second system to swap:");
        builder.AppendHexButtons(game, targets, interactions);
        
        return new SpaceWarInteractionOutcome(false, builder);
    }

    public async Task<SpaceWarInteractionOutcome> HandleInteractionAsync(DiscordMultiMessageBuilder? builder, UseDimensionalOrigamiInteraction interactionData,
        Game game, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        game.GetHexAt(interactionData.Target1).Coordinates = interactionData.Target2;
        game.GetHexAt(interactionData.Target2).Coordinates = interactionData.Target1;

        var player = game.GetGamePlayerForInteraction(interactionData);
        var name = await player.GetNameAsync(false);
        builder.AppendContentNewline($"{name} has swapped {interactionData.Target1} and {interactionData.Target2} using {DisplayName}!");
        
        player.Techs.Remove(GetThisTech(player));
        
        await GameFlowOperations.OnActionCompletedAsync(builder, game, ActionType.Free, serviceProvider);
        
        return new SpaceWarInteractionOutcome(true, builder);
    }
}