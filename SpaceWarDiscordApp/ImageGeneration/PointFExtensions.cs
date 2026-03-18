using SixLabors.ImageSharp;

namespace SpaceWarDiscordApp.ImageGeneration;

public static class PointFExtensions
{
    public static PointF Rotate(this PointF point, float degrees) =>
        PointF.Transform(point, Matrix3x2Extensions.CreateRotationDegrees(degrees));
    
    public static float Length(this PointF point) => MathF.Sqrt(point.X * point.X + point.Y * point.Y);
    public static PointF Normalised(this PointF point) => point / point.Length();
    
    public static PointF LerpTowardsAlpha(this PointF point, PointF target, float alpha) => point + (target - point) * alpha;
    public static PointF LerpTowardsDistance(this PointF point, PointF target, float distance) => point + (target - point).Normalised() * distance;
}