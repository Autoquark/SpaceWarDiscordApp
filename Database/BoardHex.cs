using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.DatabaseModels;

public enum HexType
{
    Planet,
    Hyperlane,
    Impassible
}

[FirestoreData]
public class BoardHex
{
    public BoardHex() { }
    
    public BoardHex(BoardHex other)
    {
        if (other.Planet != null)
        {
            Planet = new Planet(other.Planet);
        }

        Coordinates = other.Coordinates;
    }
    
    [FirestoreProperty]
    public Planet? Planet { get; set; }
    
    [FirestoreProperty]
    public HexCoordinates Coordinates { get; set; }

    [FirestoreProperty]
    public IList<HyperlaneConnection> HyperlaneConnections { get; set; } = [];

    [FirestoreProperty]
    public bool HasAsteroids { get; set; } = false;

    /// <summary>
    /// Gets the die emoji that represents the amount and player affiliation of any forces present, or null if no forces
    /// are present.
    /// </summary>
    public DiscordEmoji? GetDieEmoji(Game game) => Planet?.ForcesPresent > 0
        ? game.GetGamePlayerByGameId(Planet!.OwningPlayerId).PlayerColourInfo.GetDieEmoji(Planet.ForcesPresent)
        : null;
    
    /// <summary>
    /// Returns a string with this planet's coordinates plus an emoji representing the amount and player affiliation of
    /// any forces present. NB This will not work as a button label!
    /// </summary>
    public string ToCoordsWithDieEmoji(Game game)
    {
        var result = Coordinates.ToString();
        var dieEmoji = GetDieEmoji(game);
        if (dieEmoji! != null!)
        {
            result += " " + dieEmoji;
        }
        return result;
    }
}

[FirestoreData]
public record struct HyperlaneConnection([property: FirestoreProperty] HexDirection First, [property: FirestoreProperty] HexDirection Second)
{
    
}