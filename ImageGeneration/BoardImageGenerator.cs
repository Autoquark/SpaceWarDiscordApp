using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SpaceWarDiscordApp.DatabaseModels;
using SpaceWarDiscordApp.GameLogic;

namespace SpaceWarDiscordApp.ImageGeneration;

public static class BoardImageGenerator
{
    private static readonly double Root3 = Math.Sqrt(3);
    private static readonly double InnerToOuterRatio = 1.0 / (Root3 / 2.0);
    
    // Sizes
    private static readonly int HexOuterDiameter = 420;
    private static readonly double HexInnerDiameter = HexOuterDiameter / InnerToOuterRatio;
    private const int Margin = 10;
    private const float PlanetCircleRadius = 100;
    private const float ProductionCircleRadius = 25;
    private const float PlanetIconSpacingDegrees = 16;
    private const int PlanetIconSize = 50;
    private const int DieIconSize = 80;
    
    // Icons
    private static readonly Image ScienceIcon = Image.Load("Icons/materials-science.png");
    private static readonly Image StarIcon = Image.Load("Icons/staryu.png");
    private static readonly List<Image> DieIcons = [
        Image.Load("Icons/dice-six-faces-one.png"),
        Image.Load("Icons/dice-six-faces-two.png"),
        Image.Load("Icons/dice-six-faces-three.png"),
        Image.Load("Icons/dice-six-faces-four.png"),
        Image.Load("Icons/dice-six-faces-five.png"),
        Image.Load("Icons/dice-six-faces-six.png")
    ];
    
    private static readonly Font Font = SystemFonts.CreateFont("Arial", 22);

    static BoardImageGenerator()
    {
        ScienceIcon.Mutate(x => x.Resize(PlanetIconSize, 0));
        StarIcon.Mutate(x => x.Resize(PlanetIconSize, 0));
        
        foreach (var dieIcon in DieIcons)
        {
            dieIcon.Mutate(x => x.Resize(DieIconSize, 0));
        }
    }
    
    public static Image GenerateBoardImage(Game game)
    {
        var boardWidthHexes = game.Hexes.Max(x => x.Coordinates.Q) - game.Hexes.Min(x => x.Coordinates.Q) + 1;
        var boardHeightHexes = (game.Hexes.Max(x => x.Coordinates.S - x.Coordinates.R) - game.Hexes.Min(x => x.Coordinates.S - x.Coordinates.R)) / 2 + 1;
        
        var image = new Image<Rgba32>(boardWidthHexes * HexOuterDiameter + Margin, (int)(boardHeightHexes * HexInnerDiameter + Margin));
        var imageCentre = new PointF(image.Width / 2, image.Height / 2);
        
        foreach(var hex in game.Hexes)
        {
            var hexCentre = imageCentre + GetHexCentrePixelCoordinatesOffset(hex.Coordinates);
            var hexPolygon = new RegularPolygon(hexCentre, 6,
                HexOuterDiameter / 2, GeometryUtilities.DegreeToRadian(30)).GenerateOutline(2.0f);
            
            image.Mutate(x => x.Fill(Color.White));
            image.Mutate(x => x.Fill(Color.Black, hexPolygon));

            if (hex.Planet != null)
            {
                // Draw planet circle
                var planetCircle = new EllipsePolygon(hexCentre, PlanetCircleRadius)
                    .GenerateOutline(2.0f);
                image.Mutate(x => x.Fill(Color.Black, planetCircle));

                // Draw science icons
                var angle = 30 - (PlanetIconSpacingDegrees/2 * (hex.Planet.Science - 1));
                for (int i = 0; i < hex.Planet.Science; i++)
                {
                    var point = GetPointPolar((PlanetCircleRadius + 30), -angle);
                    image.Mutate(x => x.DrawImageCentred(ScienceIcon, (Point)(hexCentre + point)));
                    angle += PlanetIconSpacingDegrees;
                }
                
                // Draw star icons
                angle = 90 - (PlanetIconSpacingDegrees/2 * (hex.Planet.Stars - 1));
                for (int i = 0; i < hex.Planet.Stars; i++)
                {
                    var point = GetPointPolar((PlanetCircleRadius + 30), -angle);
                    image.Mutate(x => x.DrawImageCentred(StarIcon, (Point)(hexCentre + point)));
                    angle += PlanetIconSpacingDegrees;
                }

                // Draw production
                var circleCentre = hexCentre + GetPointPolar(PlanetCircleRadius + ProductionCircleRadius + 10, 135);
                var productionCircle = new EllipsePolygon(circleCentre, ProductionCircleRadius)
                    .GenerateOutline(2.0f);
                
                image.Mutate(x => x.Fill(Color.Black, productionCircle)
                    .DrawText(new RichTextOptions(Font){ TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Origin = circleCentre },
                        hex.Planet.Production.ToString(),
                        Color.Black));
                
                // Draw forces
                if (hex.Planet.ForcesPresent > 0)
                {
                    var recolorBrush = new RecolorBrush(Color.White, game.GetGamePlayerByGameId(hex.Planet.OwningPlayerId).PlayerColor, 0.5f);
                    using var dieImage = DieIcons[hex.Planet.ForcesPresent - 1].Clone(x => x.Fill(recolorBrush));
                    image.Mutate(x => x.DrawImageCentred(dieImage, hexCentre));
                }
            }
        }
        
        return image;
    }
    
    private static PointF GetPointPolar(float distance, float angleDegrees) => new PointF(0, -distance).Rotate(angleDegrees);

    private static PointF GetHexCentrePixelCoordinatesOffset(HexCoordinates hexCoordinates) =>
        new((float)(hexCoordinates.Q * 3.0/4.0 * HexOuterDiameter), (float)(HexOuterDiameter * ((Root3 / 2.0) * hexCoordinates.Q + Root3 * hexCoordinates.R)));
}