using Google.Cloud.Firestore;
using SixLabors.ImageSharp;

namespace SpaceWarDiscordApp.Database.Converters;

public class ImageSharpColorCoordinateConverter : IFirestoreConverter<Color>
{
    public object ToFirestore(Color value)
    {
        return value.ToHex();
    }

    public Color FromFirestore(object value)
    {
        return Color.Parse((string)value);
    }
}