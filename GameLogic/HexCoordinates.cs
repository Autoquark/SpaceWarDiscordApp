using Google.Cloud.Firestore;
using SpaceWarDiscordApp.Database.Converters;

namespace SpaceWarDiscordApp.GameLogic;

[FirestoreData(ConverterType = typeof(HexCoordinateConverter))]
public readonly struct HexCoordinates
{
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
}