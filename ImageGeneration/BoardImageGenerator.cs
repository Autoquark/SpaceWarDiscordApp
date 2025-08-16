using System.Diagnostics.Contracts;
using System.Numerics;
using System.Text;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SpaceWarDiscordApp.Database;
using SpaceWarDiscordApp.Database.EventRecords;
using SpaceWarDiscordApp.GameLogic;
using SpaceWarDiscordApp.GameLogic.Operations;
using SpaceWarDiscordApp.GameLogic.Techs;
using Path = SixLabors.ImageSharp.Drawing.Path;

namespace SpaceWarDiscordApp.ImageGeneration;

public static class BoardImageGenerator
{
    private enum SummaryTableColumn
    {
        ScoringToken,
        PlayerName,
        Science,
        Vp,
        Stars,
        //TechStatus
    }

    private static Dictionary<SummaryTableColumn, int> SummaryTableColumnWidths = new()
    {
        {SummaryTableColumn.ScoringToken, InfoTableSingleIconColumnWidth},
        {SummaryTableColumn.PlayerName, 600},
        {SummaryTableColumn.Science, InfoTableSingleIconColumnWidth},
        {SummaryTableColumn.Vp, InfoTableSingleIconColumnWidth},
        {SummaryTableColumn.Stars, InfoTableSingleIconColumnWidth * 2},
        //{InfoTableColumn.TechStatus, 1200},
    };
    
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
    private static readonly Image ScoringTokenIcon;
    private static readonly Image VpIcon;
    private static readonly Image TopStarsTiedIcon;
    private static readonly Image TopStarsIcon;
    public static readonly IReadOnlyList<Image> ColourlessDieIcons;
    public static readonly Image BlankDieIconFullSize;
    public static readonly Image PlayerAreaBlankDieIcon;
    public static readonly Image CurrentTurnPlayerIcon;

    // Fonts
    private static readonly FontCollection FontCollection = new();
    private static readonly Font ProductionNumberFont;
    private static readonly Font CoordinatesFont;
    private static readonly Font InfoTableFont;
    private static readonly Font InfoTableFontBold;
    private static readonly Font InfoTableFontItalic;
    private static readonly Font InfoTableFontBoldItalic;
    private static readonly Font PlayerAreaNameFont;
    
    // Recap graphics
    private static readonly float PreviousMoveArrowOffset = HexInnerDiameter * 0.2f; 
    private static readonly float PreviousMoveArrowControlPointOffset = HexInnerDiameter * 0.1f;
    private const float RefreshedRecapIconAngle = 30;
    private const float ProduceRecapIconAngle = 45;
    private const float RecapAlpha = 0.8f;
    private static readonly Dictionary<PlayerColour, Image> RefreshRecapIcons = new();
    private static readonly Dictionary<PlayerColour, Image> ProduceRecapIcons = new();
    private const int PlanetRecapIconSize = 48;
    
    // Info tables (general)
    private const int InfoTableRowHeight = 76;
    private const int InfoTableSingleIconColumnWidth = 76;
    private const float InfoTableLineThickness = 2;
    private static readonly Brush InfoTextBrush = new SolidBrush(Color.Black);
    private static readonly Pen InfoTextStrikethroughPen = new SolidPen(Color.Black);
    private const int InfoTableIconHeight = 32;
    private const int InfoTableCellDrawingMargin = 12;
    
    // Player tech table
    private static readonly int PlayerAreaTitleHeight;
    private const int PlayerTechTableNameColumnWidth = 600;
    private const int PlayerTechTableStateColumnWidth = 600;
    private static readonly int PlayerAreaWidth = PlayerTechTableNameColumnWidth + PlayerTechTableNameColumnWidth + Margin / 2;

    static BoardImageGenerator()
    {
        try
        {
            ScienceIcon = Image.Load("Icons/materials-science.png");
            StarIcon = Image.Load("Icons/staryu.png");
            ScoringTokenIcon = Image.Load("Icons/flying-flag.png");
            VpIcon = Image.Load("Icons/trophy-cup.png");
            TopStarsTiedIcon = Image.Load("Icons/noun-chevron-double-up-line-2648903.png");
            TopStarsIcon = Image.Load("Icons/noun-chevron-double-up-2648915.png");
            CurrentTurnPlayerIcon = Image.Load("Icons/noun-arrow-3134187.png");
            
            ColourlessDieIcons = [
                Image.Load("Icons/dice-six-faces-one.png"),
                Image.Load("Icons/dice-six-faces-two.png"),
                Image.Load("Icons/dice-six-faces-three.png"),
                Image.Load("Icons/dice-six-faces-four.png"),
                Image.Load("Icons/dice-six-faces-five.png"),
                Image.Load("Icons/dice-six-faces-six.png")
            ];
            
            BlankDieIconFullSize = Image.Load("Icons/dice-six-faces-blank.png");
            
            var family = FontCollection.Add("Fonts/Arial/arial.ttf");
            FontCollection.Add("Fonts/Arial/arialbd.ttf");
            FontCollection.Add("Fonts/Arial/arialbi.ttf");
            FontCollection.Add("Fonts/Arial/ariali.ttf");
            FontCollection.Add("Fonts/Arial/ariblk.ttf");
            ProductionNumberFont = family.CreateFont(22);
            CoordinatesFont = family.CreateFont(36);
            InfoTableFont = family.CreateFont(42);
            InfoTableFontBold = family.CreateFont(42, FontStyle.Bold);
            InfoTableFontItalic = family.CreateFont(42, FontStyle.Italic);
            InfoTableFontBoldItalic = family.CreateFont(42, FontStyle.BoldItalic);
            PlayerAreaNameFont = family.CreateFont(48, FontStyle.Bold);

            var playerAreaTitleSpacer = 12;
            PlayerAreaTitleHeight = (int)TextMeasurer.MeasureBounds("abcdefghijklmnopqrstuvwxyz", new TextOptions(PlayerAreaNameFont)
            {
                
            }).Height + playerAreaTitleSpacer;
            
            PlayerAreaBlankDieIcon = BlankDieIconFullSize.Clone(x => x.Resize(0, PlayerAreaTitleHeight - playerAreaTitleSpacer));
        
            ScienceIcon.Mutate(x => x.Resize(PlanetIconSize, 0));
            StarIcon.Mutate(x => x.Resize(PlanetIconSize, 0));
            ScoringTokenIcon.Mutate(x => x.Resize(PlanetIconSize, 0));
            VpIcon.Mutate(x => x.Resize(PlanetIconSize, 0));
            
            TopStarsTiedIcon.Mutate(x => x.Resize(0, InfoTableIconHeight));
            TopStarsIcon.Mutate(x => x.Resize(0, InfoTableIconHeight));
            CurrentTurnPlayerIcon.Mutate(x => x.Resize(0, InfoTableIconHeight));
        
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
        var playerNames = game.Players
            .Select(player => (player, player.GetNameAsync(false, false).GetAwaiter().GetResult()))
            .ToDictionary(x => x.player, x => x.Item2);
        
        var boardSize = PrecalculateBoardSize(game);
        var imageSize = boardSize;
        imageSize.Height += Margin;

        // Take info table dimensions into account
        var summaryTable = new Table();
        summaryTable.LinePen = new SolidPen(Color.Black, InfoTableLineThickness);
        summaryTable.RowInternalHeights = Enumerable.Repeat(InfoTableRowHeight, game.Players.Count + 1).ToList();
        summaryTable.CellDrawingMargin = InfoTableCellDrawingMargin;
        foreach (var column in Enum.GetValues<SummaryTableColumn>())
        {
            summaryTable.ColumnInternalWidths.Add(SummaryTableColumnWidths[column]);
        }

        // Account for summary table
        var summaryRect = summaryTable.GetRect();
        imageSize.Width = Math.Max(imageSize.Width, summaryRect.Width);
        imageSize.Height += summaryRect.Height + Margin;
        
        // Take player areas into account. We display 2 side by side with a margin between
        imageSize.Width = Math.Max(imageSize.Width, PlayerAreaWidth * 2);

        var playerAreaSize = PrecalculatePlayerAreaSize(game);
        imageSize.Width = Math.Max(imageSize.Width, playerAreaSize.Width);
        imageSize.Height += playerAreaSize.Height + Margin;
        
        // Add side margins
        imageSize.Width += Margin * 2;
        // We've been adding bottom margins as we go, so only add top margin now
        imageSize.Height += Margin;
        
        var image = new Image<Rgba32>(imageSize.Width, imageSize.Height);
        image.Mutate(x => x.BackgroundColor(Color.White));
        
        // Draw board
        var boardTopLeft = new Point(Margin, Margin);
        DrawBoard(game, image, boardTopLeft);
        
        // Draw summary table
        summaryTable.TopLeft = boardTopLeft + new Size(0, boardSize.Height + Margin);
        summaryRect = summaryTable.GetRect(); // Recalculate with final top left
        image.Mutate(x => summaryTable.Draw(x));

        var textOptions = new RichTextOptions(InfoTableFont)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Font = InfoTableFont
        };
        image.Mutate(context =>
        {
            // Scoring token column
            //infoTable.DrawTextInCell(context, 0, 0, textOptions, "SCORING", InfoTextBrush);
            
            var iconCentre = summaryTable.GetCellInternalRect((int)SummaryTableColumn.ScoringToken, game.ScoringTokenPlayerIndex + 1).GetCentre();
            context.DrawImageCentred(ScoringTokenIcon, iconCentre);
            
            // Player name column
            summaryTable.DrawTextInCell(context, (int)SummaryTableColumn.PlayerName, 0, textOptions, "PLAYER", InfoTextBrush);
            
            // Science column
            summaryTable.DrawImageInCell(context, (int)SummaryTableColumn.Science, 0, ScienceIcon);
            
            // VP column
            summaryTable.DrawImageInCell(context, (int)SummaryTableColumn.Vp, 0, VpIcon);
            
            // Stars column
            summaryTable.DrawImageInCell(context, (int)SummaryTableColumn.Stars, 0, StarIcon);
            
            // Fill in rows
            var playerStars = game.Players.Select(player => (player, stars: GameStateOperations.GetPlayerStars(game, player)))
                .OrderByDescending(x => x.stars)
                .ToList();
            var starsTied = playerStars.Count > 1 && playerStars[0].stars == playerStars[1].stars;
            
            for (var row = 1; row <= game.Players.Count; row++)
            {
                var player = game.Players[row - 1];
                
                // Player name
                textOptions.HorizontalAlignment = HorizontalAlignment.Left;
                
                var brush = new SolidBrush(player.PlayerColourInfo.ImageSharpColor);
                var offset = Size.Empty;
                if (game.CurrentTurnPlayer == player)
                {
                    textOptions.Font = InfoTableFontBold;
                    summaryTable.DrawImageInCell(context, (int)SummaryTableColumn.PlayerName, row, CurrentTurnPlayerIcon, HorizontalAlignment.Left);
                    textOptions.Font = InfoTableFont;
                    offset = new Size(CurrentTurnPlayerIcon.Width + 12, 0);
                }
                
                summaryTable.DrawTextInCell(context, (int)SummaryTableColumn.PlayerName, row, textOptions, playerNames[player], brush, offset);
                
                textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                textOptions.Font = InfoTableFont;
                
                // Science
                summaryTable.DrawTextInCell(context, (int)SummaryTableColumn.Science, row, textOptions, player.Science.ToString(), brush);
                
                // VP
                summaryTable.DrawTextInCell(context, (int)SummaryTableColumn.Vp, row, textOptions, player.VictoryPoints.ToString(), brush);
                
                // Stars
                textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                
                var stars = playerStars.First(x => x.player == player).stars;
                summaryTable.DrawTextInCell(context, (int)SummaryTableColumn.Stars, row, textOptions, stars.ToString(), brush, 0.333f);
                if (stars == playerStars[0].stars)
                {
                    summaryTable.DrawImageInCell(context, (int)SummaryTableColumn.Stars, row,
                        starsTied ? TopStarsTiedIcon : TopStarsIcon, HorizontalAlignment.Center, 1, 0.666f);
                }
            }
        });
        
        var location = new Point(Margin, summaryRect.Bottom + Margin);

        var playerNameTextOptions = new RichTextOptions(PlayerAreaNameFont)
        {
        };

        var techTableTextOptions = new RichTextOptions(InfoTableFont)
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        // Draw player tech tables
        foreach (var pairing in game.Players.ZipWithIndices()
                     .GroupBy(x => x.index / 2, x => x.item))
        {
            location.X = Margin;

            var nextY = 0;
            foreach (var player in pairing)
            {
                var tempLocation = location;
                
                var evenMoreTempLocation = tempLocation + new Size(12, 0);;
                var recolorBrush = new RecolorBrush(Color.White, player.PlayerColourInfo.ImageSharpColor, 0.5f);
                using var dieImage = PlayerAreaBlankDieIcon.Clone(x => x.Fill(recolorBrush));
                image.Mutate(x => x.DrawImage(dieImage, evenMoreTempLocation, 1.0f));
                
                playerNameTextOptions.Origin = evenMoreTempLocation + new Size(PlayerAreaBlankDieIcon.Width + 12, 0);
                image.Mutate(x => x.DrawText(playerNameTextOptions, playerNames[player], player.PlayerColourInfo.ImageSharpColor));
                tempLocation.Y += PlayerAreaTitleHeight;
                
                var playerTechTable = new Table();
                playerTechTable.TopLeft = tempLocation;
                playerTechTable.RowInternalHeights = Enumerable.Repeat(InfoTableRowHeight, player.Techs.Count() + 1).ToList();
                playerTechTable.ColumnInternalWidths = [PlayerTechTableNameColumnWidth, PlayerTechTableStateColumnWidth];
                playerTechTable.LinePen = new SolidPen(Color.Black, InfoTableLineThickness);
                playerTechTable.CellDrawingMargin = InfoTableCellDrawingMargin;
                
                image.Mutate(context =>
                {
                    playerTechTable.Draw(context);
                    playerTechTable.DrawTextInCell(context, 0, 0, techTableTextOptions, "TECH", InfoTextBrush);
                    playerTechTable.DrawTextInCell(context, 1, 0, techTableTextOptions, "STATUS", InfoTextBrush);
                    
                    foreach (var (playerTech, index) in player.Techs.ZipWithIndices())
                    {
                        playerTechTable.DrawTextInCell(context, 0, index + 1, techTableTextOptions,
                            playerTech.GetTech().DisplayName, InfoTextBrush);
                        
                        playerTechTable.DrawTextInCell(context, 1, index + 1, techTableTextOptions,
                            playerTech switch
                            {
                                { IsExhausted: true } => "Exhausted",
                                { UsedThisTurn: true } => "Used",
                                _ => "Ready"
                            }, InfoTextBrush);
                    }
                });
                
                location.X += PlayerAreaWidth + Margin;
                nextY = Math.Max(nextY, playerTechTable.GetRect().Bottom);
            }

            location.Y = nextY + Margin;
        }
        
        return image;
    }
    
    private static void DrawBoard(Game game, Image<Rgba32> image, Point topLeft)
    {
        var minBoardX = (int)game.Hexes.Min(x => HexToPixelOffset(x.Coordinates).X);
        var minBoardY = (int)game.Hexes.Min(x => HexToPixelOffset(x.Coordinates).Y);
        // Used to convert a pixel offset into absolute pixel coordinates
        //var offset = new PointF(minBoardX - HexOuterDiameter / 2.0f - Margin, minBoardY - HexInnerDiameter / 2.0f - Margin);
        var offset = (PointF)topLeft + new SizeF(HexOuterDiameter / 2.0f - minBoardX, HexInnerDiameter / 2.0f - minBoardY);
        
        // Draw hexes
        foreach(var hex in game.Hexes)
        {
            var hexOffset = HexToPixelOffset(hex.Coordinates);
            var hexCentre = hexOffset + offset;
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
                    //TODO: Cache these
                    var colourInfo = PlayerColourInfo.Get(game.GetGamePlayerByGameId(hex.Planet.OwningPlayerId).PlayerColour);
                    var recolorBrush = new RecolorBrush(Color.White, colourInfo.ImageSharpColor, 0.5f);
                    using var dieImage = ColourlessDieIcons[hex.ForcesPresent - 1].Clone(x => x.Fill(recolorBrush));
                    image.Mutate(x => x.DrawImageCentred(dieImage, hexCentre));
                }
            }

            foreach (var connection in hex.HyperlaneConnections)
            {
                var end1 = hexCentre + HexToPixelOffset(connection.First.ToHexOffset()) * 0.5f;
                var end2 = hexCentre + HexToPixelOffset(connection.Second.ToHexOffset()) * 0.5f;
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
                        
                        var destinationHexCentre = HexToPixelOffset(movement.Destination) + offset;
                        foreach (var sourceAndAmount in movement.Sources)
                        {
                            var sourceHexCentre = HexToPixelOffset(sourceAndAmount.Source) + offset;
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
                        var hexCentre = HexToPixelOffset(refresh.Coordinates) + offset;
                        var iconLocation = hexCentre + GetPointPolar(PlanetIconDistance, RefreshedRecapIconAngle);

                        image.Mutate(x => x.DrawImageCentred(RefreshRecapIcons[player.PlayerColour], iconLocation));
                    }
                        break;

                    case ProduceEventRecord produce:
                    {
                        var hexCentre = HexToPixelOffset(produce.Coordinates) + offset;
                        var iconLocation = hexCentre + GetPointPolar(PlanetIconDistance, ProduceRecapIconAngle);
                        
                        image.Mutate(x => x.DrawImageCentred(ProduceRecapIcons[player.PlayerColour], iconLocation));
                    }
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Calculates the size needed to display the player areas, excluding external margins
    /// </summary>
    /// <param name="game"></param>
    /// <returns></returns>
    [Pure]
    private static Size PrecalculatePlayerAreaSize(Game game)
    {
        var size = Size.Empty;
        
        // Player area height varies according to how many techs they have, so need to calculate. Since they are in two
        // columns, only the tallest of each pairing is relevant
        foreach (var pairing in game.Players.ZipWithIndices()
                     .GroupBy(x => x.index / 2, x => x.item))
        {
            size.Height += PlayerAreaTitleHeight;
            size.Height += pairing.Max(x => (x.Techs.Count() + 1) * InfoTableRowHeight);
        }
        
        // Add one additional margin for each player pairing beyond the first, as there's a margin of vertical separation
        // between each pair
        size.Height += (game.Players.Count / 2) * Margin;
        
        return size;
    }

    /// <summary>
    /// Calculates the size needed to display the game board, excluding margins
    /// </summary>
    [Pure]
    private static Size PrecalculateBoardSize(Game game)
    {
        var minBoardX = (int)game.Hexes.Min(x => HexToPixelOffset(x.Coordinates).X);
        var minBoardY = (int)game.Hexes.Min(x => HexToPixelOffset(x.Coordinates).Y);
        var maxBoardX = (int)game.Hexes.Max(x => HexToPixelOffset(x.Coordinates).X);
        var maxBoardY = (int)game.Hexes.Max(x => HexToPixelOffset(x.Coordinates).Y);

        return new Size(maxBoardX - minBoardX + HexOuterDiameter,
            (int)(maxBoardY - minBoardY + HexInnerDiameter));
    }
    
    [Pure]
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

    [Pure]
    private static (string, IEnumerable<RichTextRun>) FormatText(List<FormattedTextRun> textRuns)
    {
        var text = new StringBuilder();
        foreach (var run in textRuns)
        {
            text.Append(run.Text);
        }
        
        var richTextRuns = textRuns.Select(x => new RichTextRun
        {
            Font = x switch
            {
                { IsBold: true, IsItalic: true } => InfoTableFontBoldItalic,
                { IsBold:true } => InfoTableFontBold,
                { IsItalic: true } => InfoTableFontItalic,
                _ => InfoTableFont
            },
            
            StrikeoutPen = x.IsStrikethrough ? InfoTextStrikethroughPen : null,
            Start = text.Length,
            End = text.Length + x.Text.Length
        });
        
        return (text.ToString(), richTextRuns);
    }
    
    [Pure]
    private static PointF GetPointPolar(float distance, float angleDegrees) => new PointF(0, -distance).Rotate(angleDegrees);

    /// <summary>
    /// Converts hex coordinates into a pixel offset from the centre of the board
    /// </summary>
    [Pure]
    private static PointF HexToPixelOffset(in HexCoordinates hexCoordinates) =>
        new((float)(hexCoordinates.Q * (3.0/4.0) * HexOuterDiameter), (float)(HexOuterDiameter/2.0f * ((Root3 / 2.0) * hexCoordinates.Q + Root3 * hexCoordinates.R)));
}