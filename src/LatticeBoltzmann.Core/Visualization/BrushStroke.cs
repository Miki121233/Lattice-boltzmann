namespace LatticeBoltzmann.Core.Visualization;

/// <summary>
/// Computes the grid cells painted by a mouse drag. The path between two cells is 4-connected (every step moves
/// along exactly one axis), because in D2Q9 the diagonal populations pass between two wall cells that touch only
/// at a corner, so an 8-connected wall would leak.
/// </summary>
public static class BrushStroke
{
    /// <summary>Smallest brush size: a single cell.</summary>
    public const int MinSize = 1;

    /// <summary>Largest brush size.</summary>
    public const int MaxSize = 8;

    // Offsets of the brush footprint per size (index size - 1): every (dx, dy) with dx² + dy² <= (size - 1)².
    private static readonly (int Dx, int Dy)[][] Footprints = BuildFootprints();

    /// <summary>
    /// Lists the cells covered by a brush dragged from one cell to another, clipped to the grid. The endpoints may
    /// lie outside the grid; only cells inside it are returned. Cells are unique and in order of first occurrence
    /// along the path. Cost is proportional to path length times brush area.
    /// </summary>
    /// <param name="x0">Column of the start cell.</param>
    /// <param name="y0">Row of the start cell.</param>
    /// <param name="x1">Column of the end cell.</param>
    /// <param name="y1">Row of the end cell.</param>
    /// <param name="size">Brush size from <see cref="MinSize"/> to <see cref="MaxSize"/>; 1 is one cell, 2 a plus shape.</param>
    /// <param name="gridWidth">Grid width in cells.</param>
    /// <param name="gridHeight">Grid height in cells.</param>
    /// <returns>The painted cells; empty when the stroke misses the grid.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is outside its allowed range.</exception>
    public static IReadOnlyList<(int X, int Y)> Cells(int x0, int y0, int x1, int y1, int size, int gridWidth, int gridHeight)
    {
        if (size < MinSize || size > MaxSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                $"Rozmiar pędzla musi mieścić się w przedziale od {MinSize} do {MaxSize}.");
        }

        var footprint = Footprints[size - MinSize];
        var cells = new List<(int X, int Y)>();
        var seen = new HashSet<(int X, int Y)>();

        void Stamp(int cx, int cy)
        {
            foreach (var (dx, dy) in footprint)
            {
                var x = cx + dx;
                var y = cy + dy;

                // Unsigned comparison also rejects negative coordinates, and an empty grid rejects everything.
                if ((uint)x < (uint)gridWidth && (uint)y < (uint)gridHeight && seen.Add((x, y)))
                {
                    cells.Add((x, y));
                }
            }
        }

        // Bresenham in 64-bit so that extreme coordinates cannot overflow. Both steps of an iteration are decided
        // from the same error term but emitted one after the other (x first), which keeps the path 4-connected.
        long deltaX = Math.Abs((long)x1 - x0);
        long deltaY = Math.Abs((long)y1 - y0);
        var stepX = Math.Sign(x1 - (long)x0);
        var stepY = Math.Sign(y1 - (long)y0);
        var error = deltaX - deltaY;
        var px = x0;
        var py = y0;
        Stamp(px, py);
        while (px != x1 || py != y1)
        {
            var doubled = 2 * error;
            var moveX = doubled > -deltaY;
            var moveY = doubled < deltaX;
            if (moveX)
            {
                error -= deltaY;
                px += stepX;
                Stamp(px, py);
            }

            if (moveY)
            {
                error += deltaX;
                py += stepY;
                Stamp(px, py);
            }
        }

        return cells;
    }

    private static (int Dx, int Dy)[][] BuildFootprints()
    {
        var footprints = new (int Dx, int Dy)[MaxSize - MinSize + 1][];
        for (var size = MinSize; size <= MaxSize; size++)
        {
            var radius = size - 1;
            var offsets = new List<(int Dx, int Dy)>();
            for (var dy = -radius; dy <= radius; dy++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    if ((dx * dx) + (dy * dy) <= radius * radius)
                    {
                        offsets.Add((dx, dy));
                    }
                }
            }

            footprints[size - MinSize] = [.. offsets];
        }

        return footprints;
    }
}
