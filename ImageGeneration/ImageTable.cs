using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace SpaceWarDiscordApp.ImageGeneration;

public class Table
{
    /// <summary>
    /// Position of the top left of the table
    /// </summary>
    public Point TopLeft { get; set; }
    
    public List<int> ColumnInternalWidths { get; set; } = [];
    
    public List<int> RowInternalHeights { get; set; } = [];
    
    public float LineThickness => LinePen.StrokeWidth;

    public Pen LinePen { get; set; } = new SolidPen(Color.Black);

    public int CellDrawingMargin { get; set; } = 6;

    public Rectangle GetCellInternalRect(int column, int row)
    {
        if (column >= ColumnInternalWidths.Count || row >= RowInternalHeights.Count)
        {
            throw new ArgumentOutOfRangeException();
        }

        return new Rectangle(GetCellInternalLeft(column), GetCellInternalTop(row), ColumnInternalWidths[column], RowInternalHeights[row]);
    }

    public Rectangle GetRect()
        => Rectangle.FromLTRB(TopLeft.X,
            TopLeft.Y,
            GetCellExternalRight(ColumnInternalWidths.Count - 1),
            GetCellExternalBottom(RowInternalHeights.Count - 1));

    public int GetCellExternalRight(int column)
    {
        if (column >= ColumnInternalWidths.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }
        
        // Left of table, plus internal width of columns to the right and self, plus number of column borders to the right, excluding our own border
        return (int)(TopLeft.X + ColumnInternalWidths.Take(column + 1).Sum() + (column + 1) * LineThickness);
    }

    public int GetCellExternalLeft(int column)
    {
        if (column >= ColumnInternalWidths.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }
        
        // Left of table, plus internal width of columns to the left, plus number of column borders to the left, excluding our own border
        return (int)(TopLeft.X + ColumnInternalWidths.Take(column).Sum() + column * LineThickness);
    }

    public int GetCellInternalLeft(int column) => (int)(GetCellExternalLeft(column) + LineThickness);

    public int GetCellExternalTop(int row)
    {
        if (row >= RowInternalHeights.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }
        
        // Top of table, plus internal height of rows above, plus number of row borders above, excluding our own border
        return (int)(TopLeft.Y + RowInternalHeights.Take(row).Sum() + row * LineThickness);
    }

    public int GetCellExternalBottom(int row)
    {
        if (row >= RowInternalHeights.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }
        
        // Top of table, plus internal height of rows above, plus number of row borders above, plus our own bottom border
        return (int)(TopLeft.Y + RowInternalHeights.Take(row + 1).Sum() + (row + 1) * LineThickness);
    }
    
    public int GetCellInternalTop(int row) => (int)(GetCellExternalTop(row) + LineThickness);
    
    public Point GetCellExternalTopLeft(int column, int row) => new Point(GetCellExternalLeft(column), GetCellExternalTop(row));
    
    public Point GetCellInternalTopLeft(int column, int row) => new Point(GetCellInternalLeft(column), GetCellInternalTop(row));

    public IImageProcessingContext Draw(IImageProcessingContext imageProcessingContext)
    {
        var tableRect = GetRect();
        for(var column = 0; column < ColumnInternalWidths.Count; column++)
        {
            var columnX = GetCellExternalLeft(column) + LineThickness / 2;
            imageProcessingContext.DrawLine(LinePen, new PointF(columnX, tableRect.Top), new PointF(columnX, tableRect.Bottom));
        }
        
        imageProcessingContext.DrawLine(LinePen, new PointF(tableRect.Right, tableRect.Top), new PointF(tableRect.Right, tableRect.Bottom));
        
        for(var row = 0; row < RowInternalHeights.Count; row++)
        {
            var rowY = GetCellExternalTop(row) + LineThickness / 2;
            imageProcessingContext.DrawLine(LinePen, new PointF(tableRect.Left, rowY), new PointF(tableRect.Right, rowY));
        }
        
        imageProcessingContext.DrawLine(LinePen, new PointF(tableRect.Left, tableRect.Bottom), new PointF(tableRect.Right, tableRect.Bottom));

        return imageProcessingContext;
    }
    
    public IImageProcessingContext DrawTextInCell(IImageProcessingContext imageProcessingContext, int column, int row,
        RichTextOptions options, string text, Brush brush, float? xPositionOverride = null,
        float? yPositionOverride = null)
        => DrawTextInCell(imageProcessingContext, column, row, options, text, brush, Size.Empty, xPositionOverride, yPositionOverride);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="imageProcessingContext"></param>
    /// <param name="column"></param>
    /// <param name="row"></param>
    /// <param name="options">The horizontal and vertical alignment determine positioning within the cell.</param>
    /// <param name="text"></param>
    /// <param name="brush"></param>
    /// <param name="offset">Flat offset to the text's positioning, applied after all other factors</param>
    /// <param name="xAlignmentOverride">A 0..1 value which if set overrides text x positioning, with 0 being at the left
    /// of the cell and 1 at the right. options.HorizontalAlignment still controls what pivot point of the text
    /// (left, center or right) is being positioned</param>
    /// <param name="yAlignmentOverride">A 0..1 value which if set overrides text y positioning, with 0 being at the top
    /// of the cell and 1 at the bottom. options.VerticalAlignment still controls what pivot point of the text
    /// (top, center or bottom) is being positioned</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public IImageProcessingContext DrawTextInCell(IImageProcessingContext imageProcessingContext, int column, int row,
        RichTextOptions options, string text, Brush brush, Size offset, float? xAlignmentOverride = null,
        float? yAlignmentOverride = null)
    {
        var optionsCopy = new RichTextOptions(options)
        {
            WrappingLength = GetCellInternalRect(column, row).Width - 2 * CellDrawingMargin
        };

        var left = GetCellInternalLeft(column);
        var x = left + (xAlignmentOverride.HasValue ? (int)(ColumnInternalWidths[column] * xAlignmentOverride) : optionsCopy.HorizontalAlignment switch
        {
            HorizontalAlignment.Left => CellDrawingMargin,
            HorizontalAlignment.Center => ColumnInternalWidths[column] / 2,
            HorizontalAlignment.Right => ColumnInternalWidths[column] - CellDrawingMargin,
            _ => throw new ArgumentOutOfRangeException(nameof(options), optionsCopy.HorizontalAlignment, null)
        });

        var top = GetCellInternalTop(row);
        var y = top + (yAlignmentOverride.HasValue ? (int)(RowInternalHeights[row] * yAlignmentOverride) : optionsCopy.VerticalAlignment switch
        {
            VerticalAlignment.Top => CellDrawingMargin,
            VerticalAlignment.Center => RowInternalHeights[row] / 2,
            VerticalAlignment.Bottom => RowInternalHeights[row] - CellDrawingMargin,
            _ => throw new ArgumentOutOfRangeException(nameof(options), optionsCopy.VerticalAlignment, null)
        });

        optionsCopy.Origin = new Point(x, y) + offset;
        imageProcessingContext.DrawText(optionsCopy, text, brush);
        
        return imageProcessingContext;
    }

    public IImageProcessingContext DrawImageInCell(IImageProcessingContext imageProcessingContext, int column, int row,
        Image image, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center, float opacity = 1.0f,
        float? xPositionOverride = null, float? yPositionOverride = null)
    {
        var rect = GetCellInternalRect(column, row);
        
        var left = GetCellInternalLeft(column);
        var x = left + (xPositionOverride.HasValue
            ? (int)(ColumnInternalWidths[column] * xPositionOverride)
            : horizontalAlignment switch
            {
                HorizontalAlignment.Left => CellDrawingMargin,
                HorizontalAlignment.Center => ColumnInternalWidths[column] / 2,
                HorizontalAlignment.Right => ColumnInternalWidths[column] - CellDrawingMargin,
                _ => throw new ArgumentOutOfRangeException(nameof(horizontalAlignment), horizontalAlignment, null)
            });

        // Regardless of if positioning is overridden, horizontalAlignment controls where the image pivot is
        x += horizontalAlignment switch
        {
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Center => -image.Width / 2,
            HorizontalAlignment.Right => -image.Width,
            _ => throw new ArgumentOutOfRangeException(nameof(horizontalAlignment), horizontalAlignment, null)
        };
        
        var top = GetCellInternalTop(row);
        var y = top + (yPositionOverride.HasValue
            ? (int)(RowInternalHeights[row] * yPositionOverride)
            : RowInternalHeights[row] / 2);
        
        y -= image.Height / 2;
        
        imageProcessingContext.DrawImage(image, new Point(x, y), opacity);
        return imageProcessingContext;
    }
}
