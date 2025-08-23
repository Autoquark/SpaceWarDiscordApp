using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.ImageGeneration;

namespace SpaceWarDiscordApp.Discord;

public class BotManagementOperations
{
    private const string EmojiDirectoryPath = "./Icons/Emoji/";
    private static readonly string DieEmojiDirectoryPath = Path.Combine(EmojiDirectoryPath, "Dice");
    
    public static async Task UpdateEmojiAsync()
    {
        // Regenerate die emoji files
        Directory.CreateDirectory(DieEmojiDirectoryPath);
        foreach (var file in Directory.EnumerateFiles(DieEmojiDirectoryPath, "*.png"))
        {
            File.Delete(file);
        }
        
        foreach (var playerColour in Enum.GetValues<PlayerColour>().Select(PlayerColourInfo.Get))
        {
            var recolorBrush = new RecolorBrush(Color.White, playerColour.ImageSharpColor, 0.5f);
            foreach (var (image, i) in BoardImageGenerator.ColourlessDieIcons.ZipWithIndices())
            {
                using var dieImage = image.Clone(x => x.Fill(recolorBrush));
                await using var fileStream = new FileStream(Path.Combine(DieEmojiDirectoryPath, $"{playerColour.Name}_{i + 1}.png"), FileMode.Create);
                await dieImage.SaveAsPngAsync(fileStream);
            }
            
            using var blankImage = BoardImageGenerator.BlankDieIconFullSize.Clone(x => x.Fill(recolorBrush));
            await using var fileStream2 = new FileStream(Path.Combine(DieEmojiDirectoryPath, $"{playerColour.Name}_blank.png"), FileMode.Create);
            await blankImage.SaveAsPngAsync(fileStream2);
        }
        
        // Reupload bot emoji to discord
        foreach (var emoji in await Program.DiscordClient.GetApplicationEmojisAsync())
        {
            await Program.DiscordClient.DeleteApplicationEmojiAsync(emoji.Id);
        }
        
        var directories = new Queue<string>([EmojiDirectoryPath]);
        while (directories.TryDequeue(out var directoryPath))
        {
            foreach (var filePath in Directory.EnumerateFiles(directoryPath))
            {
                await using var stream = File.OpenRead(filePath);
                await Program.DiscordClient.CreateApplicationEmojiAsync(Path.GetFileNameWithoutExtension(filePath), stream);
            }
            
            foreach (var subDirectory in Directory.EnumerateDirectories(directoryPath))
            {
                directories.Enqueue(subDirectory);
            }
        }

        await Program.RebuildEmojiCache();
    }
}