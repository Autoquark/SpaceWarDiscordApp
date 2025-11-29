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

    public static bool TryFromHexNumber(int hexNumber, out HexCoordinates result)
    {
        if (hexNumber == 0)
        {
            result = new HexCoordinates(0, 0);
            return true;
        }
        
        var radius = hexNumber / 100;
        var index = hexNumber % 100;
        var sectionIndex = index / radius;
        if (sectionIndex > 5)
        {
            result = default;
            return false;
        }
        var distanceIntoSection = index % radius;
        result = new HexCoordinates(0, 0)
                     // Go out from the centre to the start of the appropriate section of the ring
                     + Enum.GetValues<HexDirection>()[sectionIndex].ToHexOffset() * radius
                     // Go into the section (around the ring) by the excess distance
                     + Enum.GetValues<HexDirection>()[(sectionIndex + 2) % 6].ToHexOffset() * distanceIntoSection;
        
        return true;
    }

    public int ToHexNumber()
    {
        var radius = new[] {Q, R, S}.Select(Math.Abs).Max();
        var index = 0;
        
        // Top to top right section
        if (R == -radius)
        {
            index = Q;
        }
        // Top right to bottom right section
        else if (Q == radius)
        {
            index = 2 * radius + R;
        }
        // Bottom right to bottom section
        else if (S == -radius)
        {
            index = 3 * radius - Q;
        }
        // Bottom to bottom left section
        else if (R == radius)
        {
            index = 4 * radius + S;
        }
        // Bottom left to top left section
        else if (Q == -radius)
        {
            index = 5 * radius - R;
        }
        // Top left to top section
        else if (S == radius)
        {
            index = 6 * radius + Q;
        }
        else
        {
            throw new Exception();
        }

        return radius * 100 + index;
    }

    public string ToHexNumberString() => ToHexNumber().ToString("000");

    public override string ToString() => ToHexNumberString();
    public string ToCoordsString() => $"({Q}, {R})";

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
    
    public static HexCoordinates operator *(int value, HexCoordinates coordinates)
        => new(coordinates.Q * value, coordinates.R * value);
    
    public static HexCoordinates operator *(HexCoordinates coordinates, int value)
        => value * coordinates;

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