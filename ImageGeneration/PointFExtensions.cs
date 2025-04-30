using SixLabors.ImageSharp;

namespace SpaceWarDiscordApp.ImageGeneration;

public static class PointFExtensions
{
    public static PointF Rotate(this PointF point, float degrees) =>
        PointF.Transform(point, Matrix3x2Extensions.CreateRotationDegrees(degrees));
}