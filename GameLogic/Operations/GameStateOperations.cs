using DSharpPlus.Entities;
using SixLabors.ImageSharp;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Discord;
using SpaceWarDiscordApp.GameLogic.Techs;
using SpaceWarDiscordApp.ImageGeneration;

namespace SpaceWarDiscordApp.GameLogic.Operations;

public static class GameStateOperations
{
    public static int GetPlayerScienceIconsControlled(Game game, GamePlayer player)
        => game.Hexes.WhereOwnedBy(player).Sum(x => x.Planet!.Science);
    
    public static int GetPlayerStars(Game game, GamePlayer player)
        => game.Hexes.WhereOwnedBy(player).Sum(x => x.Planet!.Stars)
           // Hardcoded for now as there's only one tech that does this
           + (player.TryGetPlayerTechById(Tech_GlorificationMatrix.StaticId) == null ? 0 : 1);

    public static async Task<DiscordMultiMessageBuilder> ShowBoardStateMessageAsync(DiscordMultiMessageBuilder builder, Game game, bool oldCoords = false)
    {
        using var image = BoardImageGenerator.GenerateBoardImage(game, oldCoords);
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        var name = await game.CurrentTurnPlayer.GetNameAsync(false);
        builder.NewMessage()
            .AppendContentNewline(
                $"Board state for {Program.TextInfo.ToTitleCase(game.Name)} at turn {game.TurnNumber} ({name}'s turn)")
            .AddFile("board.png", stream)
            .AddMediaGalleryComponent(new DiscordMediaGalleryItem("attachment://board.png"));
        
        return builder;
    }
    
    public static bool GameUsesScoringToken(Game game) => game.Players.Count != 2 && game.Rules.ScoringRule == ScoringRule.MostStars; 
}