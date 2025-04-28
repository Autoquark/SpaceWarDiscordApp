using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SpaceWarDiscordApp.DatabaseModels;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.ImageGeneration;

public class BoardImageGenerator
{
    private static readonly double Root3 = Math.Sqrt(3);
    private static readonly double InnerToOuterRatio = 1.0 / (Root3 / 2.0);
    private static readonly int HexOuterDiameter = 420;
    private static readonly double HexInnerDiameter = HexOuterDiameter / InnerToOuterRatio;
    private static readonly int Margin = 10;
    
    public static Image GenerateBoardImage(Game game)
    {
        var boardWidthHexes = game.Hexes.Max(x => x.Coordinates.Q) - game.Hexes.Min(x => x.Coordinates.Q) + 1;
        var boardHeightHexes = (game.Hexes.Max(x => x.Coordinates.S - x.Coordinates.R) - game.Hexes.Min(x => x.Coordinates.S - x.Coordinates.R)) / 2 + 1;
        
        var image = new Image<Rgba32>((int)(boardWidthHexes * HexInnerDiameter) + Margin, boardHeightHexes * HexOuterDiameter + Margin);
        var imageCentre = new PointF(image.Width / 2, image.Height / 2);
        
        foreach(var hex in game.Hexes)
        {
            var hexPolygon = new RegularPolygon(imageCentre + GetHexCentrePixelCoordinatesOffset(hex.Coordinates), 6,
                HexOuterDiameter / 2, 0).GenerateOutline(2.0f);
            
            image.Mutate(x => x.Fill(Color.White));
            image.Mutate(x => x.Fill(Color.Black, hexPolygon));
        }
        
        return image;
    }

    private static PointF GetHexCentrePixelCoordinatesOffset(HexCoordinates hexCoordinates) =>
        new((float)(hexCoordinates.Q * 3.0/4.0 * HexOuterDiameter), (float)(HexOuterDiameter * ((Root3 / 2.0) * hexCoordinates.Q + Root3 * hexCoordinates.R)));
}