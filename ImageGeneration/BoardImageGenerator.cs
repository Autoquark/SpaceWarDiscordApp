using System.Diagnostics.Contracts;
using System.Net.WebSockets;
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
    
    // Additional Margin around edge of map image and between sections
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
    private static readonly Image PlayerAreaBlankDieIcon;
    private static readonly Image CurrentTurnPlayerIcon;
    private static readonly Image ScienceCostIcon;

    private static readonly IDictionary<string, Image> IconSubstitutions;
    
    private static readonly int InlineIconSize = 42;
    private static readonly Size InlineIconOffset = new(-4, 0);

    // Fonts
    private static readonly FontCollection FontCollection = new();
    private static readonly Font ProductionNumberFont;
    private static readonly Font CoordinatesFont;
    private static readonly Font InfoTableFont;
    private static readonly Font InfoTableFontBold;
    private static readonly Font InfoTableFontItalic;
    private static readonly Font InfoTableFontBoldItalic;
    private static readonly Font PlayerAreaNameFont;
    private static readonly Font SectionHeaderFont;
    private static readonly Font TechCostFont;
    
    // Recap graphics
    private static readonly float PreviousMoveArrowOffset = HexInnerDiameter * 0.2f; 
    private static readonly float PreviousMoveArrowControlPointOffset = HexInnerDiameter * 0.1f;
    private static readonly float PreviousMoveArrowTechIconOffset = HexInnerDiameter * 0.17f;
    private const float RefreshedRecapIconAngle = 30;
    private const float ProduceRecapIconAngle = 45;
    private const float TechRecapIconAngle = 60;
    private const float TechRecapIconSeparation = 25;
    private const float RecapAlpha = 0.8f;
    private static readonly Dictionary<PlayerColour, Image> RefreshRecapIcons = new();
    private static readonly Dictionary<PlayerColour, Image> ProduceRecapIcons = new();
    private static readonly Dictionary<PlayerColour, Image> TechRecapIcons = new();
    private const int PlanetRecapIconSize = 48;
    private static readonly Pen SolidLinePen = new SolidPen(Color.Black, PreviousMoveThickness);
    private static readonly Pen DottedLinePen = new PatternPen(Color.Black, PreviousMoveThickness, [2.0f, 2.0f]);
    
    // Info tables (general)
    private const int InfoTableRowHeight = 76;
    private const int InfoTableSingleIconColumnWidth = 76;
    private const float InfoTableLineThickness = 2;
    private static readonly Brush InfoTextBrush = new SolidBrush(Color.Black);
    private static readonly Pen InfoTextOutlinePen = Pens.Solid(Color.Grey);
    private static readonly Pen InfoTextStrikethroughPen = new SolidPen(Color.Black);
    private const int InfoTableIconHeight = 32;
    private const int InfoTableCellDrawingMargin = 16;

    private static readonly int SectionHeaderHeight;
    private const int ScienceCostIconSize = 80;
    private const int SpacingBelowSectionHeader = 50;
    
    // Player tech table
    private static readonly int PlayerAreaTitleHeight;
    private const int PlayerTechTableNameColumnWidth = 600;
    private const int PlayerTechTableStateColumnWidth = 600;
    private static readonly int PlayerAreaWidth = PlayerTechTableNameColumnWidth + PlayerTechTableNameColumnWidth + Margin / 2;
    
    // Purchaseable techs
    private const int TechBoxWidth = 700;
    private static readonly int TechBoxMinHeight;
    private const int TechBoxToCostSpacing = 8;

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
            SectionHeaderFont = family.CreateFont(72, FontStyle.Bold);
            TechCostFont = SectionHeaderFont;

            const string alphabet = "abcdefghijklmnopqrstuvwxyz";
            
            SectionHeaderHeight = (int)TextMeasurer.MeasureSize(alphabet, new TextOptions(SectionHeaderFont)).Height;

            var playerAreaTitleSpacer = 12;
            PlayerAreaTitleHeight = (int)TextMeasurer.MeasureBounds(alphabet, new TextOptions(PlayerAreaNameFont)
            {
                
            }).Height + playerAreaTitleSpacer;
            
            TechBoxMinHeight = (int)TextMeasurer.MeasureBounds(alphabet, new TextOptions(InfoTableFont)).Height * 4;
            
            PlayerAreaBlankDieIcon = BlankDieIconFullSize.Clone(x => x.Resize(0, PlayerAreaTitleHeight - playerAreaTitleSpacer));
        
            ScienceCostIcon = ScienceIcon.Clone(x => x.Resize(ScienceCostIconSize, 0));
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
            
            using var techIcon = Image.Load("Icons/noun-exclamation-7818018.png");
            techIcon.Mutate(x => x.Resize(PlanetRecapIconSize, 0));
            
            foreach (var colour in Enum.GetValues<PlayerColour>())
            {
                var recolorBrush = new RecolorBrush(Color.White, PlayerColourInfo.Get(colour).ImageSharpColor.WithAlpha(RecapAlpha), 0.5f);
                RefreshRecapIcons.Add(colour, refreshIcon.Clone(x => x.Fill(recolorBrush)));
                ProduceRecapIcons.Add(colour, produceIcon.Clone(x => x.Fill(recolorBrush)));
                TechRecapIcons.Add(colour, techIcon.Clone(x => x.Fill(recolorBrush)));
            }

            IconSubstitutions = new Dictionary<string, Image>
            {
                { "star", StarIcon.Clone(x => x.Resize(InlineIconSize, 0)) },
                { "science", ScienceIcon.Clone(x => x.Resize(InlineIconSize, 0)) }
            };
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
        
        // === Calculate required image dimensions ===
        
        // Board size
        var boardSize = PrecalculateBoardSize(game);
        var imageSize = boardSize;
        imageSize.Height += Margin;
        
        // Universal techs
        imageSize.Height += SectionHeaderHeight + SpacingBelowSectionHeader;
        
        var maxUniversalTechDescriptionHeight = 0;
        var universalTechSectionHeight = 0;
        foreach (var tech in game.UniversalTechs.ToTechsById())
        {
            var descriptionHeight = 0;
            var table = LayoutPurchaseableTech(tech, ref descriptionHeight);
            maxUniversalTechDescriptionHeight = Math.Max(maxUniversalTechDescriptionHeight, descriptionHeight);
            universalTechSectionHeight = Math.Max(universalTechSectionHeight, table.GetRect().Height);;
        }

        universalTechSectionHeight += TechBoxToCostSpacing + ScienceCostIcon.Height;
        imageSize.Height += universalTechSectionHeight + Margin;
        
        // Tech Market
        imageSize.Height += SectionHeaderHeight + SpacingBelowSectionHeader;
        
        var maxMarketTechDescriptionHeight = 0;
        var marketTechSectionHeight = 0;
        foreach (var tech in game.TechMarket.ToTechsByIdNullable())
        {
            var descriptionHeight = 0;
            var table = LayoutPurchaseableTech(tech, ref descriptionHeight);
            maxMarketTechDescriptionHeight = Math.Max(maxMarketTechDescriptionHeight, descriptionHeight);
            marketTechSectionHeight = Math.Max(marketTechSectionHeight, table.GetRect().Height);
        }
        marketTechSectionHeight += TechBoxToCostSpacing + ScienceCostIcon.Height;
        imageSize.Height += marketTechSectionHeight + Margin;
        
        // Summary table
        imageSize.Height += SectionHeaderHeight + SpacingBelowSectionHeader;
        
        var summaryTable = new Table();
        summaryTable.LinePen = new SolidPen(Color.Black, InfoTableLineThickness);
        summaryTable.RowInternalHeights = Enumerable.Repeat(InfoTableRowHeight, game.Players.Count + 1).ToList();
        summaryTable.CellDrawingMargin = InfoTableCellDrawingMargin;
        foreach (var column in Enum.GetValues<SummaryTableColumn>())
        {
            summaryTable.ColumnInternalWidths.Add(SummaryTableColumnWidths[column]);
        }
        
        var summaryRect = summaryTable.GetRect();
        imageSize.Width = Math.Max(imageSize.Width, summaryRect.Width);
        imageSize.Height += summaryRect.Height + Margin;
        
        // Player areas. We display 2 side by side with a margin between
        imageSize.Height += SectionHeaderHeight + SpacingBelowSectionHeader;
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
        
        
        // === Draw image ===
        var verticalMargin = new Size(0, Margin);
        
        // Draw board
        var sectionTopLeft = new Point(Margin, Margin);
        sectionTopLeft = DrawBoard(game, image, sectionTopLeft);
        sectionTopLeft += verticalMargin;

        // Universal Techs
        sectionTopLeft = DrawSectionHeader("Universal Techs", image, sectionTopLeft);
        
        sectionTopLeft = DrawUniversalTechs(game, image, sectionTopLeft, maxUniversalTechDescriptionHeight);
        sectionTopLeft += verticalMargin;
        
        // Tech Market
        sectionTopLeft = DrawSectionHeader("Tech Market", image, sectionTopLeft);
        
        sectionTopLeft = DrawMarketTechs(game, image, sectionTopLeft, maxMarketTechDescriptionHeight);
        sectionTopLeft += verticalMargin;
        
        // Draw summary table
        sectionTopLeft = DrawSectionHeader("Summary", image, sectionTopLeft);
        
        var spareHorizontalSpace = image.Width - Margin * 2 - summaryRect.Width;
        summaryTable.TopLeft = sectionTopLeft + new Size(spareHorizontalSpace / 2, 0);
        summaryRect = summaryTable.GetRect(); // Recalculate with final top left
        image.Mutate(x => summaryTable.Draw(x));

        var textOptions = new RichTextOptions(InfoTableFont)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Font = InfoTableFont,
            TextRuns = [] // Workaround for bug in imagesharp, RichTextOptions leaves this as a collection of non-rich text run
        };
        image.Mutate(context =>
        {
            // Scoring token column
            //infoTable.DrawTextInCell(context, 0, 0, textOptions, "SCORING", InfoTextBrush);

            if (game.Players.Count != 2)
            {
                var iconCentre = summaryTable
                    .GetCellInternalRect((int)SummaryTableColumn.ScoringToken, game.ScoringTokenPlayerIndex + 1)
                    .GetCentre();
                context.DrawImageCentred(ScoringTokenIcon, iconCentre);
            }

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
                
                summaryTable.DrawTextInCell(context, (int)SummaryTableColumn.PlayerName, row, textOptions, playerNames[player], brush: brush, offset: offset, outlinePen: InfoTextOutlinePen);
                
                textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                textOptions.Font = InfoTableFont;
                
                // Science
                summaryTable.DrawTextInCell(context, (int)SummaryTableColumn.Science, row, textOptions, player.Science.ToString(), brush, InfoTextOutlinePen);
                
                // VP
                summaryTable.DrawTextInCell(context, (int)SummaryTableColumn.Vp, row, textOptions, player.VictoryPoints.ToString(), brush, InfoTextOutlinePen);
                
                // Stars
                textOptions.HorizontalAlignment = HorizontalAlignment.Center;
                
                var stars = playerStars.First(x => x.player == player).stars;
                summaryTable.DrawTextInCell(context, (int)SummaryTableColumn.Stars, row, textOptions, stars.ToString(), brush, InfoTextOutlinePen, 0.333f);
                if (stars == playerStars[0].stars)
                {
                    summaryTable.DrawImageInCell(context, (int)SummaryTableColumn.Stars, row,
                        starsTied ? TopStarsTiedIcon : TopStarsIcon, HorizontalAlignment.Center, 1, 0.666f);
                }

                // If player is eliminated, strikethrough entire row in their colour
                if (player.IsEliminated)
                {
                    brush = new SolidBrush(player.PlayerColourInfo.ImageSharpColor.WithAlpha(0.5f));
                    var y = summaryTable.GetCellInternalTopLeft(0, row).Y + InfoTableRowHeight / 2;
                    context.DrawLine(brush, 4.0f,
                        new PointF(summaryTable.GetCellInternalLeft(0) + summaryTable.CellDrawingMargin, y),
                        new PointF(summaryTable.GetCellInternalRight(summaryTable.ColumnInternalWidths.Count - 1) - summaryTable.CellDrawingMargin, y));
                }
            }
        });
        
        sectionTopLeft = new Point(Margin, summaryRect.Bottom + Margin);
        
        // Player areas
        sectionTopLeft = DrawSectionHeader("Players", image, sectionTopLeft);
        
        var location = sectionTopLeft;

        var playerNameTextOptions = new RichTextOptions(PlayerAreaNameFont)
        {
            TextRuns = [] // Workaround for bug in imagesharp, RichTextOptions leaves this as a collection of non-rich text run
        };

        var techTableTextOptions = new RichTextOptions(InfoTableFont)
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextRuns = [] // Workaround for bug in imagesharp, RichTextOptions leaves this as a collection of non-rich text run
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
                    
                    foreach (var (playerTech, index) in player.Techs
                                 .OrderBy(x => 0 + (x.UsedThisTurn ? 1 : 0) + (x.IsExhausted ? 2 : 0))
                                 .ZipWithIndices())
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

    private static Point DrawSectionHeader(string text, Image<Rgba32> image, Point topLeft)
    {
        var textOptions = new RichTextOptions(SectionHeaderFont)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Origin = new Point(image.Width / 2, topLeft.Y),
            TextRuns = [] // Workaround for bug in imagesharp, RichTextOptions leaves this as a collection of non-rich text run
        };
        image.Mutate(x => x.DrawText(textOptions, text, Color.Black));
        return topLeft + new Size(0, SectionHeaderHeight + SpacingBelowSectionHeader);
    }

    private static Point DrawUniversalTechs(Game game, Image<Rgba32> image, Point topLeft,
        int maxDescriptionHeight)
    {
        // Draw universal techs (assume 3 for now)
        var spareHorizontalSpace = image.Width - Margin * 4 - TechBoxWidth * 3;

        var height = 0;
        image.Mutate(context =>
        {
            var xLocation = topLeft.X + spareHorizontalSpace / 2;
            foreach (var tech in game.UniversalTechs.Select(x => Tech.TechsById[x]))
            {
                var size = DrawPurchaseableTech(context, new Point(xLocation, topLeft.Y), tech, GameConstants.UniversalTechCost, maxDescriptionHeight);
                xLocation += TechBoxWidth + Margin;
                height = Math.Max(height, size.Height);
            }
        });

        return topLeft + new Size(0, height);
    }
    
    private static Point DrawMarketTechs(Game game, Image<Rgba32> image, Point topLeft, int maxDescriptionHeight)
    {
        var spareHorizontalSpace = image.Width - Margin * 4 - TechBoxWidth * 3;

        var height = 0;
        image.Mutate(context =>
        {
            var xLocation = topLeft.X + spareHorizontalSpace / 2;
            foreach (var (tech, i) in game.TechMarket.Select(x => x == null ? null : Tech.TechsById[x]).ZipWithIndices())
            {
                var size = DrawPurchaseableTech(context, new Point(xLocation, topLeft.Y), tech, TechOperations.GetMarketSlotCost(i), maxDescriptionHeight);
                xLocation += TechBoxWidth + Margin;
                height = Math.Max(height, size.Height);
            }
        });

        return topLeft + new Size(0, height);
    }

    private static (string text, List<RichTextRun> runs) FormatTechDescription(Tech tech)
    {
        var keywords = tech.DescriptionKeywords.Any() ? string.Join(", ", tech.DescriptionKeywords) + ": " : "";
        var text = keywords + tech.Description;
        var runs = new List<RichTextRun>();
        if (tech.DescriptionKeywords.Any())
        {
            runs.Add(new RichTextRun
            {
                Start = 0,
                End = keywords.Length,
                Font = InfoTableFontBold
            });
        }

        runs.Add(new RichTextRun
        {
            Start = keywords.Length,
            End = text.Length,
            Font = InfoTableFont
        });
        
        return (text, runs);
    }

    private static Table LayoutPurchaseableTech(Tech? tech, ref int descriptionHeight)
    {
        if (descriptionHeight == 0)
        {
            if (tech == null)
            {
                descriptionHeight = TechBoxMinHeight;
            }
            else
            {
                var (description, runs) = FormatTechDescription(tech);
                var descriptionBounds = TextMeasurer.MeasureSize(description, new TextOptions(InfoTableFont)
                {
                    WrappingLength = TechBoxWidth - 2 * InfoTableCellDrawingMargin,
                    TextRuns = runs
                });

                descriptionHeight = Math.Max(TechBoxMinHeight, (int)(descriptionBounds.Height + 2 * InfoTableCellDrawingMargin));
            }
        }

        return new Table
        {
            RowInternalHeights = [InfoTableRowHeight, descriptionHeight],
            ColumnInternalWidths = [TechBoxWidth],
            CellDrawingMargin = InfoTableCellDrawingMargin,
            LinePen = new SolidPen(Color.Black, InfoTableLineThickness)
        };
    }

    private static Size DrawPurchaseableTech(IImageProcessingContext context, Point topLeft, Tech? tech, int cost, int heightOverride)
    {
        var table = LayoutPurchaseableTech(tech, ref heightOverride);
        table.TopLeft = topLeft;
        
        table.Draw(context);
        
        var textOptions = new RichTextOptions(InfoTableFontBold)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextRuns = [] // Workaround for bug in imagesharp, RichTextOptions leaves this as a collection of non-rich text run
        };
        
        if (tech != null)
        {
            table.DrawTextInCell(context, 0, 0, textOptions, tech.DisplayName, InfoTextBrush);

            var (text, runs) = FormatTechDescription(tech);
            textOptions.Font = InfoTableFont;
            textOptions.HorizontalAlignment = HorizontalAlignment.Left;
            textOptions.VerticalAlignment = VerticalAlignment.Top;
            textOptions.TextRuns = runs;

            table.DrawTextInCell(context, 0, 1, textOptions, text,
                InfoTextBrush, iconSubstitutions: IconSubstitutions, iconOffset: InlineIconOffset);
        }

        // Draw cost beneath table
        var rect = table.GetRect();
        var costPosition = topLeft + new Size(rect.Width / 2, rect.Height + TechBoxToCostSpacing);

        textOptions.TextRuns = [];
        textOptions.Font = TechCostFont;
        textOptions.VerticalAlignment = VerticalAlignment.Center;
        textOptions.HorizontalAlignment = HorizontalAlignment.Left;
        var digitSize = TextMeasurer.MeasureSize(cost.ToString(), textOptions);
        
        
        textOptions.Origin = costPosition + new Size(-(int)digitSize.Width - 4, ScienceCostIcon.Height / 2);
        
        context.DrawText(textOptions, cost.ToString(), Color.Black);
        context.DrawImage(ScienceCostIcon, costPosition + new Size(4, 0), 1.0f);
        
        return rect.Size + new Size(0, TechBoxToCostSpacing + ScienceCostIcon.Height);
    }
    
    private static Point DrawBoard(Game game, Image<Rgba32> image, Point topLeft)
    {
        var minBoardX = (int)game.Hexes.Min(x => HexToPixelOffset(x.Coordinates).X);
        var minBoardY = (int)game.Hexes.Min(x => HexToPixelOffset(x.Coordinates).Y);
        // Used to convert a pixel offset into absolute pixel coordinates
        //var offset = new PointF(minBoardX - HexOuterDiameter / 2.0f - Margin, minBoardY - HexInnerDiameter / 2.0f - Margin);
        var boardOffset = (PointF)topLeft + new SizeF(HexOuterDiameter / 2.0f - minBoardX, HexInnerDiameter / 2.0f - minBoardY);
        
        // Draw hexes
        foreach(var hex in game.Hexes)
        {
            var hexOffset = HexToPixelOffset(hex.Coordinates);
            var hexCentre = hexOffset + boardOffset;
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
                            Origin = circleCentre,
                            TextRuns = [] // Workaround for bug in imagesharp, RichTextOptions leaves this as a collection of non-rich text run
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
                    Origin = textOrigin,
                    TextRuns = [] // Workaround for bug in imagesharp, RichTextOptions leaves this as a collection of non-rich text run
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
                        
                        var destinationHexCentre = HexToPixelOffset(movement.Destination) + boardOffset;
                        foreach (var sourceAndAmount in movement.Sources)
                        {
                            var sourceHexCentre = HexToPixelOffset(sourceAndAmount.Source) + boardOffset;
                            var controlPoint = (sourceHexCentre + destinationHexCentre) / 2.0f;
                            var techIconLocation = controlPoint;

                            // Movement with no horizontal component, move the control point to the right so
                            // we still get a curve
                            if (sourceAndAmount.Source.Q == movement.Destination.Q)
                            {
                                controlPoint.X += PreviousMoveArrowControlPointOffset;
                                techIconLocation.X += PreviousMoveArrowTechIconOffset;
                            }
                            else
                            {
                                controlPoint.Y -= PreviousMoveArrowControlPointOffset;
                                techIconLocation.Y -= PreviousMoveArrowTechIconOffset;
                            }
                            
                            
                            var start = sourceHexCentre.LerpTowardsDistance(destinationHexCentre,
                                PreviousMoveArrowOffset);
                            var end = destinationHexCentre.LerpTowardsDistance(sourceHexCentre,
                                PreviousMoveArrowOffset);

                            var bezier = new Path(new CubicBezierLineSegment(start, controlPoint, controlPoint, end));

                            var arrowhead = new Path(new LinearLineSegment(end + (end - controlPoint).Normalised().Rotate(135) * 30.0f,
                                    end,
                                    end + (end - controlPoint).Normalised().Rotate(-135) * 30.0f))
                                .GenerateOutline(PreviousMoveThickness);
                            
                            image.Mutate(x =>
                            {
                                Pen pen = movement.IsTechMove
                                    ? Pens.Dash(player.PlayerColourInfo.ImageSharpColor, PreviousMoveThickness)
                                    : new SolidPen(player.PlayerColourInfo.ImageSharpColor, PreviousMoveThickness);
                                x.Draw(pen, bezier).Fill(colour, arrowhead);
                            });
                        }
                        break;

                    case RefreshPlanetEventRecord refresh:
                    {
                        var hexCentre = HexToPixelOffset(refresh.Coordinates) + boardOffset;
                        var iconLocation = hexCentre + GetPointPolar(PlanetIconDistance, RefreshedRecapIconAngle);

                        image.Mutate(x => x.DrawImageCentred(RefreshRecapIcons[player.PlayerColour], iconLocation));
                    }
                        break;

                    case ProduceEventRecord produce:
                    {
                        var hexCentre = HexToPixelOffset(produce.Coordinates) + boardOffset;
                        var iconLocation = hexCentre + GetPointPolar(PlanetIconDistance, ProduceRecapIconAngle);
                        
                        image.Mutate(x => x.DrawImageCentred(ProduceRecapIcons[player.PlayerColour], iconLocation));
                    }
                        break;
                }
            }
        }
        
        // Draw planet targeted tech icons. We need to group them by planet first as there could be multiple on one planet
        foreach (var grouping in game.Players.SelectMany(x => x.LastTurnEvents.OfType<PlanetTargetedTechEventRecord>(),
                         (player, record) => (player, record))
                     .GroupBy(x => x.record.Coordinates))
        {
            var iconLocation = boardOffset + HexToPixelOffset(grouping.Key) + GetPointPolar(PlanetIconDistance, TechRecapIconAngle);
            var count = grouping.Count();
            iconLocation.X -= (TechRecapIconSeparation * (count - 1)) / 2;

            foreach (var (player, _) in grouping)
            {
                image.Mutate(context => context.DrawImageCentred(TechRecapIcons[player.PlayerColour], iconLocation));
                iconLocation.X += TechRecapIconSeparation;
            }
        }
        
        var maxBoardY = (int)game.Hexes.Max(x => HexToPixelOffset(x.Coordinates).Y);
        return new Point(topLeft.X, (int)boardOffset.Y + maxBoardY + (int)(HexInnerDiameter / 2));
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
    private static (string, List<RichTextRun>) FormatText(IReadOnlyCollection<FormattedTextRun> textRuns)
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
        
        return (text.ToString(), richTextRuns.ToList());
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