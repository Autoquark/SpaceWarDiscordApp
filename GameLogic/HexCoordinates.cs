using System.Text.RegularExpressions;
using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.Converters;

namespace SpaceWarDiscordApp.GameLogic;

[FirestoreData(ConverterType = typeof(HexCoordinateConverter))]
public readonly partial record struct HexCoordinates
{
    public static readonly Regex HexCoordsRegex = MyRegex();

    public static bool TryParse(string input, out HexCoordinates coordinates)
    {
        input = new string(input.Where(x => !char.IsWhiteSpace(x)).ToArray());
        var matches = HexCoordsRegex.Match(input);
        if (!matches.Success
            || !int.TryParse(matches.Groups[1].Value, out var q)
            || !int.TryParse(matches.Groups[2].Value, out var r))
        {
            coordinates = default;
            return false;
        }

        coordinates = new HexCoordinates(q, r);
        return true;
    }
    public static HexCoordinates Parse(string coordinates)
    {
        if (TryParse(coordinates, out var result))
        {
            return result;
        }
        
        throw new ArgumentException($"Invalid hex coordinates string: {coordinates}");
    }
    
    public HexCoordinates(int q, int r)
    {
        this.Q = q;
        this.R = r;
    }

    /// <summary>
    /// Offset from the centre in the Q axis. +Q is to the right
    /// </summary>
    public int Q { get; } = 0;
    
    /// <summary>
    /// Offset from the centre in the R axis. +R is down and left
    /// </summary>
    public int R { get; } = 0;

    /// <summary>
    /// Offset from the centre in the S axis. +S is up and left
    /// </summary>
    public int S => -Q - R;

    public IEnumerable<HexCoordinates> GetNeighbors()
    {
        yield return new HexCoordinates(Q + 1, R);
        yield return new HexCoordinates(Q + 1, R - 1);
        yield return new HexCoordinates(Q, R - 1);
        yield return new HexCoordinates(Q - 1, R);
        yield return new HexCoordinates(Q - 1, R + 1);
        yield return new HexCoordinates(Q, R + 1);
    }

    public override string ToString()
    {
        return $"({Q}, {R})";
    }
    
    /// <summary>
    /// Returns this position rotated the given number of 60 degree increments clockwise around 0, 0
    /// </summary>
    /// <param name="sixths"></param>
    /// <returns></returns>
    public HexCoordinates RotateClockwise(int sixths)
    {
        var q = Q;
        var r = R;
        for (var i = 0; i < sixths; i++)
        {
            var s = -q - r;
            q = -r;
            r = -s;
        }
        
        return new HexCoordinates(q, r);
    }

    public static HexCoordinates operator +(HexCoordinates coordinates, HexDirection direction)
        => coordinates + direction.ToHexOffset();

    public static HexCoordinates operator +(HexCoordinates coordinates, HexCoordinates coordinates2)
        => new(coordinates.Q + coordinates2.Q, coordinates.R + coordinates2.R);

    [GeneratedRegex(@"\(?(-?[0-9]+),(-?[0-9]+)\)?")]
    private static partial Regex MyRegex();
}

public enum HexDirection
{
    North,
    NorthEast,
    SouthEast,
    South,
    SouthWest,
    NorthWest
}