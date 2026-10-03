using System.Buffers;

namespace LatticeBoltzmann.Core.Visualization;

/// <summary>Renders the concentration field of a simulation into an ARGB pixel buffer, one pixel per cell.</summary>
public static class FieldRenderer
{
    /// <summary>
    /// Fills <paramref name="argb"/> in row-major order (index y * Width + x): walls get
    /// <see cref="Colormap.WallArgb"/>, fluid cells get <see cref="Colormap.ToArgb(double)"/> of their concentration.
    /// The only temporary storage is a pooled buffer, so the call does not allocate per frame.
    /// </summary>
    /// <param name="simulation">Simulation to render.</param>
    /// <param name="argb">Buffer of exactly Width * Height elements.</param>
    /// <exception cref="ArgumentNullException"><paramref name="simulation"/> is null.</exception>
    /// <exception cref="ArgumentException">The buffer has the wrong length.</exception>
    public static void Render(DiffusionSimulation simulation, Span<int> argb)
    {
        ArgumentNullException.ThrowIfNull(simulation);

        var width = simulation.Width;
        var cellCount = width * simulation.Height;
        if (argb.Length != cellCount)
        {
            throw new ArgumentException("Bufor musi mieć dokładnie Width * Height elementów.", nameof(argb));
        }

        var rented = ArrayPool<double>.Shared.Rent(cellCount);
        try
        {
            var concentration = rented.AsSpan(0, cellCount);
            simulation.CopyConcentration(concentration);
            for (var cell = 0; cell < cellCount; cell++)
            {
                argb[cell] = simulation.IsWall(cell % width, cell / width)
                    ? Colormap.WallArgb
                    : Colormap.ToArgb(concentration[cell]);
            }
        }
        finally
        {
            ArrayPool<double>.Shared.Return(rented);
        }
    }
}
