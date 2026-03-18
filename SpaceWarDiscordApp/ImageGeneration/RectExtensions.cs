using SixLabors.ImageSharp;

namespace SpaceWarDiscordApp.ImageGeneration;

public static class RectExtensions
{
    public static Point GetCentre(this Rectangle rectangle) => rectangle.Location + rectangle.Size / 2;
    public static Point GetPoint(this Rectangle rectangle, float xProportion, float yProportion) => rectangle.Location + new Size((int)(rectangle.Width * xProportion), (int)(rectangle.Height * yProportion));
}