namespace LatticeBoltzmann.Core.Visualization;

/// <summary>
/// Maps between screen pixels of a view and cells of the simulation grid. The grid image is drawn with a uniform
/// scale (one cell is a square) and centred in the view, leaving letterbox bars on the sides that do not fit.
/// A view with no usable area (zero, negative or non-finite size) has <see cref="Scale"/> 0 and maps no pixel.
/// </summary>
public readonly record struct ViewMapping
{
    private readonly int _gridWidth;
    private readonly int _gridHeight;

    /// <summary>Creates the mapping of a view onto a grid.</summary>
    /// <param name="viewWidth">View width in pixels; a negative or non-finite value is treated as 0.</param>
    /// <param name="viewHeight">View height in pixels; a negative or non-finite value is treated as 0.</param>
    /// <param name="gridWidth">Grid width in cells, at least 1.</param>
    /// <param name="gridHeight">Grid height in cells, at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">A grid dimension is not positive.</exception>
    public ViewMapping(double viewWidth, double viewHeight, int gridWidth, int gridHeight)
    {
        if (gridWidth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(gridWidth), gridWidth, "Szerokość siatki musi być dodatnia.");
        }

        if (gridHeight < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(gridHeight), gridHeight, "Wysokość siatki musi być dodatnia.");
        }

        _gridWidth = gridWidth;
        _gridHeight = gridHeight;

        var width = Usable(viewWidth);
        var height = Usable(viewHeight);
        Scale = Math.Min(width / gridWidth, height / gridHeight);
        DrawWidth = gridWidth * Scale;
        DrawHeight = gridHeight * Scale;
        OffsetX = (width - DrawWidth) / 2;
        OffsetY = (height - DrawHeight) / 2;
    }

    /// <summary>Pixels per cell edge; 0 when the view has no usable area.</summary>
    public double Scale { get; }

    /// <summary>Horizontal distance in pixels from the view's left edge to the grid image.</summary>
    public double OffsetX { get; }

    /// <summary>Vertical distance in pixels from the view's top edge to the grid image.</summary>
    public double OffsetY { get; }

    /// <summary>Width of the grid image in pixels.</summary>
    public double DrawWidth { get; }

    /// <summary>Height of the grid image in pixels.</summary>
    public double DrawHeight { get; }

    /// <summary>Finds the grid cell under a view pixel.</summary>
    /// <param name="px">Horizontal pixel coordinate within the view.</param>
    /// <param name="py">Vertical pixel coordinate within the view.</param>
    /// <param name="x">Receives the cell column, or 0 when the pixel is outside the image.</param>
    /// <param name="y">Receives the cell row, or 0 when the pixel is outside the image.</param>
    /// <returns><see langword="true"/> when the pixel lies on a cell of the grid image; otherwise <see langword="false"/>.</returns>
    public bool TryGetCell(double px, double py, out int x, out int y)
    {
        x = 0;
        y = 0;
        if (Scale <= 0)
        {
            return false;
        }

        // Compared as doubles before the cast, so NaN and huge coordinates are rejected instead of wrapping.
        var column = Math.Floor((px - OffsetX) / Scale);
        var row = Math.Floor((py - OffsetY) / Scale);
        if (!(column >= 0 && column < _gridWidth && row >= 0 && row < _gridHeight))
        {
            return false;
        }

        x = (int)column;
        y = (int)row;
        return true;
    }

    private static double Usable(double length) => double.IsFinite(length) && length > 0 ? length : 0;
}
