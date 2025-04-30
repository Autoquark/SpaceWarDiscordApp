using Google.Cloud.Firestore;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.Database.Converters;

public class HexCoordinateConverter : IFirestoreConverter<HexCoordinates>
{
    public object ToFirestore(HexCoordinates value)
    {
        return new Dictionary<string, int>
        {
            { "R", value.R },
            { "Q", value.Q }
        };
    }

    public HexCoordinates FromFirestore(object value)
    {
        Dictionary<string, object> dictionary = (Dictionary<string, object>)value;
        return new HexCoordinates(Convert.ToInt32(dictionary["Q"]), Convert.ToInt32(dictionary["R"]));
    }
}