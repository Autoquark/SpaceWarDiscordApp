using DSharpPlus.Entities;
using SixLabors.ImageSharp;

namespace SpaceWarDiscordApp.GameLogic;

public enum PlayerColour
{
    Red,
    Green,
    Blue,
    Orange,
    Yellow,
    Cyan,
    Purple
}

public class PlayerColourInfo
{
    public static PlayerColourInfo Get(PlayerColour colour) => Colors[colour];

    private static readonly IReadOnlyDictionary<PlayerColour, PlayerColourInfo> Colors =
        new Dictionary<PlayerColour, PlayerColourInfo>
        {
            {
                PlayerColour.Red,
                new PlayerColourInfo
                {
                    Name = "red",
                    PlayerColour = PlayerColour.Red,
                    ImageSharpColor = Color.Red
                }
            },
            {
                PlayerColour.Green,
                new PlayerColourInfo
                {
                    Name = "green",
                    PlayerColour = PlayerColour.Green,
                    ImageSharpColor = Color.Green
                }
            },
            {
                PlayerColour.Blue,
                new PlayerColourInfo
                {
                    Name = "blue",
                    PlayerColour = PlayerColour.Blue,
                    ImageSharpColor = Color.Blue
                }
            },
            {
                PlayerColour.Orange,
                new PlayerColourInfo
                {
                    Name = "orange",
                    PlayerColour = PlayerColour.Orange,
                    ImageSharpColor = Color.Orange
                }
            },
            {
                PlayerColour.Yellow,
                new PlayerColourInfo
                {
                    Name = "yellow",
                    PlayerColour = PlayerColour.Yellow,
                    ImageSharpColor = Color.Yellow
                }
            },
            {
                PlayerColour.Cyan,
                new PlayerColourInfo
                {
                    Name = "cyan",
                    PlayerColour = PlayerColour.Cyan,
                    ImageSharpColor = Color.Cyan
                }
            },
            {
                PlayerColour.Purple,
                new PlayerColourInfo
                {
                    Name = "purple",
                    PlayerColour = PlayerColour.Purple,
                    ImageSharpColor = Color.Purple
                }
            }
        };
    
    public required string Name { get; init; }
    public required PlayerColour PlayerColour { get; init; }
    public required Color ImageSharpColor { get; init; }
    public DiscordEmoji GetDieEmoji(int number) => Program.AppEmojisByName[$"{Name}_{number}"];
}