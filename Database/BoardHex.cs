using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database;

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

    public bool IsNeutral => Planet == null || Planet.IsNeutral;
    
    public bool AnyForcesPresent => ForcesPresent > 0;
    public int ForcesPresent => Planet?.ForcesPresent ?? 0;
    
    /// <summary>
    /// Gets the die emoji that represents the amount and player affiliation of any forces present, or null if no forces
    /// are present.
    /// </summary>
    public DiscordEmoji? GetDieEmoji(Game game) => ForcesPresent > 0
        ? game.GetGamePlayerByGameId(Planet!.OwningPlayerId).PlayerColourInfo.GetDieEmoji(ForcesPresent)
        : null;
    
    /// <summary>
    /// Returns a string with this planet's coordinates plus an emoji representing the amount and player affiliation of
    /// any forces present. NB This will not work as a button label!
    /// </summary>
    public string ToHexNumberWithDieEmoji(Game game)
    {
        var result = Coordinates.ToHexNumberString();
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