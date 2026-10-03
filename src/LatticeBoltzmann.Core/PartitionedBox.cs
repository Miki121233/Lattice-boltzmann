namespace LatticeBoltzmann.Core;

/// <summary>
/// Scenario builder: a box split by a vertical partition wall with a gap of adjustable width.
/// The left chamber starts filled with concentration 1, the right chamber starts empty.
/// </summary>
public static class PartitionedBox
{
    private const double InitialConcentration = 1.0;

    /// <summary>Index of the column that holds the partition wall: one third of the grid width, rounded down.</summary>
    /// <param name="width">Number of columns of the grid.</param>
    public static int PartitionColumn(int width) => width / 3;

    /// <summary>
    /// Creates a simulation with the partition wall, a gap centred vertically in it,
    /// and the left chamber filled with concentration 1.
    /// </summary>
    /// <param name="width">Number of columns, at least 3.</param>
    /// <param name="height">Number of rows, at least 3.</param>
    /// <param name="gapWidth">Gap height in cells, from 0 (closed partition) to <paramref name="height"/> (no wall).</param>
    /// <param name="diffusionCoefficient">Diffusion coefficient D in lattice units, within the allowed range.</param>
    /// <exception cref="ArgumentOutOfRangeException">An argument is outside its allowed range.</exception>
    public static DiffusionSimulation Create(int width, int height, int gapWidth, double diffusionCoefficient)
    {
        var simulation = new DiffusionSimulation(width, height, diffusionCoefficient);
        ApplyGap(simulation, gapWidth);

        var partitionColumn = PartitionColumn(width);
        for (var y = 0; y < height; y++)
        {
            // Every cell left of the partition column is fluid; only the partition column itself holds walls.
            for (var x = 0; x < partitionColumn; x++)
            {
                simulation.SetConcentration(x, y, InitialConcentration);
            }
        }

        return simulation;
    }

    /// <summary>
    /// Rebuilds the partition column of an existing simulation so that it has a centred gap of the given width.
    /// Only that column is touched: other walls and all concentrations outside it stay as they are;
    /// cells that turn into wall lose their content and cells that open start empty.
    /// </summary>
    /// <param name="simulation">The simulation to modify.</param>
    /// <param name="gapWidth">Gap height in cells, from 0 (closed partition) to the grid height (no wall).</param>
    /// <exception cref="ArgumentNullException"><paramref name="simulation"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="gapWidth"/> is outside the allowed range.</exception>
    public static void ApplyGap(DiffusionSimulation simulation, int gapWidth)
    {
        ArgumentNullException.ThrowIfNull(simulation);

        var height = simulation.Height;
        if (gapWidth < 0 || gapWidth > height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gapWidth),
                gapWidth,
                $"Szerokość otworu musi mieścić się w przedziale od 0 do wysokości siatki ({height}).");
        }

        var column = PartitionColumn(simulation.Width);
        var gapStart = (height - gapWidth) / 2;
        var gapEnd = gapStart + gapWidth;
        for (var y = 0; y < height; y++)
        {
            simulation.SetWall(column, y, y < gapStart || y >= gapEnd);
        }
    }
}
