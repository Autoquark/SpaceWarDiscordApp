using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;
using SixLabors.Fonts;
using SixLabors.Fonts.Unicode;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace SpaceWarDiscordApp.ImageGeneration;

public static partial class ImageProcessingExtensions
{
    private static readonly Regex iconReplacementRegex = InitializeIconReplacementRegex();

    private const string iconPlaceholderString = "   ";

    public static IImageProcessingContext DrawImageCentred(this IImageProcessingContext imageProcessingContext,
        Image image, PointF location, float opacity = 1.0f)
        => imageProcessingContext.DrawImage(image, (Point)location - image.Size / 2, 1.0f);

    public static IImageProcessingContext DrawTextWithInlineIcons(this IImageProcessingContext imageProcessingContext,
        RichTextOptions textOptions,
        string text,
        Brush? brush,
        Pen? pen,
        IDictionary<string, Image> substitutions,
        Size iconOffset)
    {
        var textOptionsCopy = new RichTextOptions(textOptions)
        {
            TextRuns = textOptions.TextRuns.ToList()
        };

        if (textOptionsCopy.TextRuns.Count == 0)
        {
            textOptionsCopy.TextRuns =
            [
                new RichTextRun
                {
                    Start = 0,
                    End = text.Length,
                    Font = textOptions.Font,
                }
            ];
        }

        var indexAndImage = new List<(int index, Image image)>();
        var charDiff = 0;

        text = iconReplacementRegex.Replace(text, match =>
        {
            indexAndImage.Add((match.Index + charDiff, substitutions[match.Groups[1].Value[1..^1]]));
            charDiff -= match.Value.Length - iconPlaceholderString.Length;

            foreach (var run in textOptionsCopy.TextRuns)
            {
                if (run.Start > match.Index)
                {
                    run.Start -= match.Length;
                    run.Start += iconPlaceholderString.Length;
                }

                if (run.End > match.Index)
                {
                    run.End -= match.Length;
                    run.End += iconPlaceholderString.Length;
                }
            }

            return iconPlaceholderString;
        });

        imageProcessingContext.DrawText(textOptionsCopy, text, brush, pen);

        TextMeasurer.TryMeasureCharacterBounds(text, textOptionsCopy, out var bounds);
        
        // It's possible the returned bounds array omits spaces that ended up at the end of lines and therefore won't be rendered
        // This will throw off our character indexes, so we need to account for them
        var glyphEnumerator = bounds.GetEnumerator();
        using var textEnumerator = text.GetEnumerator();

        var hasMore = glyphEnumerator.MoveNext() && textEnumerator.MoveNext();

        while (hasMore)
        {
            var currentBounds = glyphEnumerator.Current;
            if (currentBounds.Codepoint.Value == textEnumerator.Current)
            {
                hasMore = glyphEnumerator.MoveNext() && textEnumerator.MoveNext();
                continue;
            }
            
            Debug.Assert(char.IsWhiteSpace(textEnumerator.Current));
            
            // Omitted whitespace, fix up image insertion indices
            for (var i = 0; i < indexAndImage.Count; i++)
            {
                if (indexAndImage[i].index >= currentBounds.StringIndex)
                {
                    indexAndImage[i] = indexAndImage[i] with { index = indexAndImage[i].index - 1 };
                }
            }

            hasMore = textEnumerator.MoveNext();
        }
        
        foreach (var (index, image) in indexAndImage)
        {
            var glyphBounds = bounds[index].Bounds;
            imageProcessingContext.DrawImageCentred(image,
                new Point((int)glyphBounds.Location.X, (int)glyphBounds.Location.Y)
                    + new Size((int)((glyphBounds.Width * iconPlaceholderString.Length) / 2), (int)(glyphBounds.Height / 2))
                    + iconOffset);
        }

        return imageProcessingContext;
    }

    [GeneratedRegex(@"(\$[^$]+\$)")]
    private static partial Regex InitializeIconReplacementRegex();
}