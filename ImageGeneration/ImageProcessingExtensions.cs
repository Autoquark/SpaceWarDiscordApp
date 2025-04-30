using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace SpaceWarDiscordApp.ImageGeneration;

public static class ImageProcessingExtensions
{
    public static IImageProcessingContext DrawImageCentred(this IImageProcessingContext imageProcessingContext,
        Image image, PointF location, float opacity = 1.0f)
        => imageProcessingContext.DrawImage(image, (Point)location - image.Size / 2, 1.0f);
}