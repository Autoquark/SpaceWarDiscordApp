using System.Drawing;

namespace SpaceWarDiscordApp.ImageGeneration;

public static class SizeFExtensions
{
    extension(SizeF size)
    {
        public SizeF Normalized() => size / ((float)Math.Sqrt(size.Width * size.Width + size.Height * size.Height));
    }
}