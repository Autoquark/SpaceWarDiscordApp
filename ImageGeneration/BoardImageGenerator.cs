using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.GameLogic;
using Path = SixLabors.ImageSharp.Drawing.Path;

namespace SpaceWarDiscordApp.ImageGeneration;

public static class BoardImageGenerator
{
    private static readonly double Root3 = Math.Sqrt(3);
    private static readonly double InnerToOuterRatio = 1.0 / (Root3 / 2.0);
    
    // *** Image Constants ***
    
    // * Hexes *
    private static readonly int HexOuterDiameter = 420;
    private static readonly float HexInnerDiameter = (float)(HexOuterDiameter / InnerToOuterRatio);
    
    // Additional Margin around edge of map image
    private const int Margin = 100;
    
    // * Planets *
    private const float PlanetCircleRadius = 100;
    private const float HomePlanetInnerCircleRadius = 80;
    private const float ProductionCircleRadius = 25;
    private const float PlanetIconSpacingDegrees = 16;
    private const int PlanetIconSize = 64;
    
    private const int DieIconSize = 80;
    private const float HyperlaneThickness = 20;
    private const float PreviousMoveThickness = 10;
    private const float AsteroidTriangleSideLength = 40;
    private static readonly float PlanetIconDistance = PlanetCircleRadius + 36;
    
    // Icons
    private static readonly Image ScienceIcon;
    private static readonly Image StarIcon;
    public static readonly IReadOnlyList<Image> ColourlessDieIcons;
    public static readonly Image BlankDieIcon;

    private static readonly FontCollection FontCollection = new();
    private static readonly Font ProductionNumberFont; // = SystemFonts.CreateFont("Arial", 22);
    private static readonly Font CoordinatesFont; // = SystemFonts.CreateFont("Arial", 36);
    
    // Recap graphics
    private static readonly float PreviousMoveArrowOffset = HexInnerDiameter * 0.2f; 
    private static readonly float PreviousMoveArrowControlPointOffset = HexInnerDiameter * 0.1f; 
    private static readonly float RefreshedRecapIconAngle = 30; 
    private static readonly float ProduceRecapIconAngle = 45; 
    private static readonly float RecapAlpha = 0.8f; 
    private static readonly Dictionary<PlayerColour, Image> RefreshRecapIcons = new();
    private static readonly Dictionary<PlayerColour, Image> ProduceRecapIcons = new();
    private const int PlanetRecapIconSize = 48;

    static BoardImageGenerator()
    {
        try
        {
            ScienceIcon = Image.Load("Icons/materials-science.png");
            StarIcon = Image.Load("Icons/staryu.png");
            
            ColourlessDieIcons = [
                Image.Load("Icons/dice-six-faces-one.png"),
                Image.Load("Icons/dice-six-faces-two.png"),
                Image.Load("Icons/dice-six-faces-three.png"),
                Image.Load("Icons/dice-six-faces-four.png"),
                Image.Load("Icons/dice-six-faces-five.png"),
                Image.Load("Icons/dice-six-faces-six.png")
            ];
            
            BlankDieIcon = Image.Load("Icons/dice-six-faces-blank.png");
            
            var family = FontCollection.Add("Fonts/Arial/ARIAL.TTF");
            ProductionNumberFont = family.CreateFont(22);
            CoordinatesFont = family.CreateFont(36);
        
            ScienceIcon.Mutate(x => x.Resize(PlanetIconSize, 0));
            StarIcon.Mutate(x => x.Resize(PlanetIconSize, 0));
        
            foreach (var dieIcon in ColourlessDieIcons)
            {
                dieIcon.Mutate(x => x.Resize(DieIconSize, 0));
            }

            using var refreshIcon = Image.Load("Icons/anticlockwise-rotation.png");
            refreshIcon.Mutate(x => x.Resize(PlanetRecapIconSize, 0));
            
            using var produceIcon = Image.Load("Icons/trample.png");
            produceIcon.Mutate(x => x.Resize(PlanetRecapIconSize, 0));
            
            foreach (var colour in Enum.GetValues<PlayerColour>())
            {
                var recolorBrush = new RecolorBrush(Color.White, PlayerColourInfo.Get(colour).ImageSharpColor, 0.5f);
                RefreshRecapIcons.Add(colour, refreshIcon.Clone(x => x.Fill(recolorBrush)));
                ProduceRecapIcons.Add(colour, produceIcon.Clone(x => x.Fill(recolorBrush)));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    public static Image GenerateBoardImage(Game game)
    {
        var minX = game.Hexes.Min(x => HexToPixel(x.Coordinates).X);
        var minY = game.Hexes.Min(x => HexToPixel(x.Coordinates).Y);
        var maxX = game.Hexes.Max(x => HexToPixel(x.Coordinates).X);
        var maxY = game.Hexes.Max(x => HexToPixel(x.Coordinates).Y);
        
        var image = new Image<Rgba32>((int)(maxX - minX + HexOuterDiameter + Margin * 2), (int)(maxY - minY + HexInnerDiameter + Margin * 2));
        var offset = new PointF(minX - HexOuterDiameter / 2.0f - Margin, minY - HexInnerDiameter / 2.0f - Margin);
        
        image.Mutate(x => x.BackgroundColor(Color.White));
        
        foreach(var hex in game.Hexes)
        {
            var hexOffset = HexToPixel(hex.Coordinates);
            var hexCentre = hexOffset - offset;
            var hexPolygon = new RegularPolygon(hexCentre, 6,
                HexOuterDiameter / 2.0f, GeometryUtilities.DegreeToRadian(30)).GenerateOutline(2.0f);
            
            image.Mutate(x => x.Fill(Color.Black, hexPolygon));

            if (hex.Planet != null)
            {
                // Draw planet circle
                var planetCircle = new EllipsePolygon(hexCentre, PlanetCircleRadius)
                    .GenerateOutline(2.0f);
                image.Mutate(x => x.Fill(Color.Black, planetCircle));
                
                // Draw inner circle for home planet
                if (hex.Planet.IsHomeSystem)
                {
                    var innerCircle = new EllipsePolygon(hexCentre, HomePlanetInnerCircleRadius)
                        .GenerateOutline(2.0f);
                    image.Mutate(x => x.Fill(Color.Black, innerCircle));
                }

                if (hex.Planet.IsExhausted)
                {
                    image.Mutate(x => x.DrawLine(Color.Black,
                        2.0f,
                        hexCentre + GetPointPolar(PlanetCircleRadius, 45),
                        hexCentre + GetPointPolar(PlanetCircleRadius, 225)));
                    image.Mutate(x => x.DrawLine(Color.Black,
                        2.0f,
                        hexCentre + GetPointPolar(PlanetCircleRadius, -45),
                        hexCentre + GetPointPolar(PlanetCircleRadius, 135)));
                }

                // Draw science icons
                var angle = 30 - (PlanetIconSpacingDegrees/2 * (hex.Planet.Science - 1));
                for (var i = 0; i < hex.Planet.Science; i++)
                {
                    var point = GetPointPolar(PlanetIconDistance, -angle);
                    image.Mutate(x => x.DrawImageCentred(ScienceIcon, (Point)(hexCentre + point)));
                    angle += PlanetIconSpacingDegrees;
                }
                
                // Draw star icons
                angle = 90 - (PlanetIconSpacingDegrees/2 * (hex.Planet.Stars - 1));
                for (var i = 0; i < hex.Planet.Stars; i++)
                {
                    var point = GetPointPolar(PlanetIconDistance, -angle);
                    image.Mutate(x => x.DrawImageCentred(StarIcon, (Point)(hexCentre + point)));
                    angle += PlanetIconSpacingDegrees;
                }

                // Draw production
                var circleCentre = hexCentre + GetPointPolar(PlanetCircleRadius + ProductionCircleRadius + 10, 135);
                var productionCircle = new EllipsePolygon(circleCentre, ProductionCircleRadius)
                    .GenerateOutline(2.0f);
                
                image.Mutate(x => x.Fill(Color.Black, productionCircle)
                    .DrawText(new RichTextOptions(ProductionNumberFont)
                        {
                            TextAlignment = TextAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Origin = circleCentre
                        },
                        hex.Planet.Production.ToString(),
                        Color.Black));
                
                // Draw forces
                if (hex.ForcesPresent > 0)
                {
                    var colourInfo = PlayerColourInfo.Get(game.GetGamePlayerByGameId(hex.Planet.OwningPlayerId).PlayerColour);
                    var recolorBrush = new RecolorBrush(Color.White, colourInfo.ImageSharpColor, 0.5f);
                    using var dieImage = ColourlessDieIcons[hex.ForcesPresent - 1].Clone(x => x.Fill(recolorBrush));
                    image.Mutate(x => x.DrawImageCentred(dieImage, hexCentre));
                }
            }

            foreach (var connection in hex.HyperlaneConnections)
            {
                var end1 = hexCentre + HexToPixel(connection.First.ToHexOffset()) * 0.5f;
                var end2 = hexCentre + HexToPixel(connection.Second.ToHexOffset()) * 0.5f;
                var bezier = new Path(new CubicBezierLineSegment(end1, hexCentre, hexCentre, end2))
                    .GenerateOutline(HyperlaneThickness);
                
                image.Mutate(x => x.Fill(Color.Black, bezier));
            }

            if (hex.HasAsteroids)
            {
                var centres = GetPolygonVertices(hexCentre, AsteroidTriangleSideLength * 2, 3);

                foreach (var centre in centres)
                {
                    var triangle = new RegularPolygon(centre, 3,
                        AsteroidTriangleSideLength, GeometryUtilities.DegreeToRadian(180));
                
                    image.Mutate(x => x.Fill(Color.Black, triangle));
                }
            }

            var textOrigin = hexCentre + new PointF(0, HexInnerDiameter * 0.4f);
            SizeF textAreaSize = new SizeF(100, 50);
            image.Mutate(x => x.Fill(new DrawingOptions() { GraphicsOptions = new GraphicsOptions() { BlendPercentage = 0.5f } }, Color.White , new RectangleF(textOrigin - textAreaSize/2, textAreaSize)));
            
            image.Mutate(x => x.DrawText(new RichTextOptions(CoordinatesFont)
                {
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Origin = textOrigin
                },
                hex.Coordinates.ToString(),
                Color.Black));
        }
        
        // Draw previous turn actions for each player
        foreach (var player in game.Players)
        {
            foreach (var action in player.LastTurnEvents)
            {
                var colour = player.PlayerColourInfo.ImageSharpColor.WithAlpha(RecapAlpha);
                switch (action)
                {
                    case MovementEventRecord movement:
                        
                        var destinationHexCentre = HexToPixel(movement.Destination) - offset;
                        foreach (var sourceAndAmount in movement.Sources)
                        {
                            var sourceHexCentre = HexToPixel(sourceAndAmount.Source) - offset;
                            var controlPoint = (sourceHexCentre + destinationHexCentre) / 2.0f;

                            // Movement with no horizontal component, move the control point to the right so
                            // we still get a curve
                            if (sourceAndAmount.Source.Q == movement.Destination.Q)
                            {
                                controlPoint.X += PreviousMoveArrowControlPointOffset;
                            }
                            else
                            {
                                controlPoint.Y -= PreviousMoveArrowControlPointOffset;
                            }
                            
                            
                            var start = sourceHexCentre.LerpTowardsDistance(destinationHexCentre,
                                PreviousMoveArrowOffset);
                            var end = destinationHexCentre.LerpTowardsDistance(sourceHexCentre,
                                PreviousMoveArrowOffset);
                            
                            var bezier = new Path(new CubicBezierLineSegment(start, controlPoint, controlPoint, end))
                                .GenerateOutline(PreviousMoveThickness);

                            var arrowhead = new Path(new LinearLineSegment(end + (end - controlPoint).Normalised().Rotate(135) * 30.0f,
                                    end,
                                    end + (end - controlPoint).Normalised().Rotate(-135) * 30.0f))
                                .GenerateOutline(PreviousMoveThickness);

                            
                            image.Mutate(x => x.Fill(colour, bezier)
                                .Fill(colour, arrowhead));
                        }
                        break;

                    case RefreshPlanetEventRecord refresh:
                    {
                        var hexCentre = HexToPixel(refresh.Coordinates) - offset;
                        var iconLocation = hexCentre + GetPointPolar(PlanetIconDistance, RefreshedRecapIconAngle);

                        image.Mutate(x => x.DrawImageCentred(RefreshRecapIcons[player.PlayerColour], iconLocation));
                    }
                        break;

                    case ProduceEventRecord produce:
                    {
                        var hexCentre = HexToPixel(produce.Coordinates) - offset;
                        var iconLocation = hexCentre + GetPointPolar(PlanetIconDistance, ProduceRecapIconAngle);
                        
                        image.Mutate(x => x.DrawImageCentred(ProduceRecapIcons[player.PlayerColour], iconLocation));
                    }
                        break;
                }
            }
        }
        
        return image;
    }

    private static PointF[] GetPolygonVertices(PointF location, float radius, int vertices, float angle = 0.0f)
    {
        var distanceVector = new PointF(0, radius);

        float anglePerSegments = (float)(2 * Math.PI / vertices);
        float current = angle;
        var points = new PointF[vertices];
        for (int i = 0; i < vertices; i++)
        {
            var rotated = PointF.Transform(distanceVector, Matrix3x2.CreateRotation(current));

            points[i] = rotated + location;

            current += anglePerSegments;
        }
        
        return points;
    }
    
    private static PointF GetPointPolar(float distance, float angleDegrees) => new PointF(0, -distance).Rotate(angleDegrees);

    /// <summary>
    /// Converts a 2d vector in hexes into pixels
    /// </summary>
    private static PointF HexToPixel(in HexCoordinates hexCoordinates) =>
        new((float)(hexCoordinates.Q * (3.0/4.0) * HexOuterDiameter), (float)(HexOuterDiameter/2.0f * ((Root3 / 2.0) * hexCoordinates.Q + Root3 * hexCoordinates.R)));
}