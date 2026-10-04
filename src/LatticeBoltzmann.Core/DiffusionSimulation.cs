namespace LatticeBoltzmann.Core;

/// <summary>
/// Diffusion of a scalar concentration on a rectangular grid, solved with the D2Q9 lattice Boltzmann method.
/// Every cell is either fluid (nine populations f_i) or a wall (all populations zero).
/// </summary>
public sealed class DiffusionSimulation
{
    /// <summary>Smallest accepted diffusion coefficient.</summary>
    public const double MinDiffusionCoefficient = 0.05;

    /// <summary>Largest accepted diffusion coefficient.</summary>
    public const double MaxDiffusionCoefficient = 0.5;

    private const int MinGridSize = 3;

    // Populations of cell (x, y) direction i live at index (y * Width + x) * D2Q9.Q + i.
    // Wall cells hold zeros in both buffers.
    // The two buffers swap roles after every step, so they cannot be readonly.
    private double[] _f;
    private double[] _next;
    private readonly bool[] _wall;
    private double _diffusionCoefficient;

    /// <summary>Creates a grid made entirely of empty fluid cells.</summary>
    /// <param name="width">Number of columns, at least 3.</param>
    /// <param name="height">Number of rows, at least 3.</param>
    /// <param name="diffusionCoefficient">Diffusion coefficient D in lattice units, within the allowed range.</param>
    /// <exception cref="ArgumentOutOfRangeException">An argument is outside its allowed range or not finite.</exception>
    public DiffusionSimulation(int width, int height, double diffusionCoefficient)
    {
        if (width < MinGridSize)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Szerokość siatki musi wynosić co najmniej 3.");
        }

        if (height < MinGridSize)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Wysokość siatki musi wynosić co najmniej 3.");
        }

        ValidateDiffusionCoefficient(diffusionCoefficient, nameof(diffusionCoefficient));

        Width = width;
        Height = height;
        _diffusionCoefficient = diffusionCoefficient;
        _f = new double[width * height * D2Q9.Q];
        _next = new double[_f.Length];
        _wall = new bool[width * height];
    }

    /// <summary>Number of columns.</summary>
    public int Width { get; }

    /// <summary>Number of rows.</summary>
    public int Height { get; }

    /// <summary>Number of completed simulation steps.</summary>
    public long StepCount { get; private set; }

    /// <summary>
    /// Diffusion coefficient D. Must lie within <see cref="MinDiffusionCoefficient"/> and
    /// <see cref="MaxDiffusionCoefficient"/>; a rejected value leaves the current one unchanged.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the allowed range or not finite.</exception>
    public double DiffusionCoefficient
    {
        get => _diffusionCoefficient;
        set
        {
            ValidateDiffusionCoefficient(value, nameof(value));
            _diffusionCoefficient = value;
        }
    }

    /// <summary>BGK relaxation time, tau = 3 D + 1/2.</summary>
    public double RelaxationTime => (3.0 * _diffusionCoefficient) + 0.5;

    /// <summary>Sum of the concentration over all fluid cells.</summary>
    public double TotalMass
    {
        get
        {
            double mass = 0;
            for (var cell = 0; cell < _wall.Length; cell++)
            {
                mass += ConcentrationAt(cell);
            }

            return mass;
        }
    }

    /// <summary>Tells whether the cell is a wall.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The coordinates lie outside the grid.</exception>
    public bool IsWall(int x, int y) => _wall[CellIndex(x, y)];

    /// <summary>
    /// Turns a cell into a wall or back into fluid. Painting a wall on fluid removes its content;
    /// erasing a wall leaves empty fluid; a call that does not change the state does nothing.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The coordinates lie outside the grid.</exception>
    public void SetWall(int x, int y, bool isWall)
    {
        var cell = CellIndex(x, y);
        if (_wall[cell] == isWall)
        {
            return;
        }

        _wall[cell] = isWall;
        if (isWall)
        {
            // Both buffers are cleared so a wall never shows stale populations after the buffers swap.
            Array.Clear(_f, cell * D2Q9.Q, D2Q9.Q);
            Array.Clear(_next, cell * D2Q9.Q, D2Q9.Q);
        }
    }

    /// <summary>Returns the concentration of the cell; zero for a wall.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The coordinates lie outside the grid.</exception>
    public double GetConcentration(int x, int y) => ConcentrationAt(CellIndex(x, y));

    /// <summary>Sets the concentration of a fluid cell by putting its populations in equilibrium, f_i = w_i * value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The coordinates lie outside the grid, or the value is negative or not finite.</exception>
    /// <exception cref="InvalidOperationException">The cell is a wall.</exception>
    public void SetConcentration(int x, int y, double value)
    {
        var cell = CellIndex(x, y);
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Stężenie musi być liczbą skończoną i nieujemną.");
        }

        if (_wall[cell])
        {
            throw new InvalidOperationException("Nie można ustawić stężenia w komórce będącej ścianą.");
        }

        var weights = D2Q9.Weights;
        var offset = cell * D2Q9.Q;
        for (var i = 0; i < D2Q9.Q; i++)
        {
            _f[offset + i] = weights[i] * value;
        }
    }

    /// <summary>Copies the concentration of every cell in row-major order (index y * Width + x); walls are zero.</summary>
    /// <param name="destination">Buffer of exactly Width * Height elements.</param>
    /// <exception cref="ArgumentException">The buffer has the wrong length.</exception>
    public void CopyConcentration(Span<double> destination)
    {
        if (destination.Length != _wall.Length)
        {
            throw new ArgumentException("Bufor musi mieć dokładnie Width * Height elementów.", nameof(destination));
        }

        for (var cell = 0; cell < destination.Length; cell++)
        {
            destination[cell] = ConcentrationAt(cell);
        }
    }

    /// <summary>
    /// Advances the simulation by the given number of steps. Each step relaxes the populations of every fluid cell
    /// towards equilibrium (BGK collision) and streams them to the neighbouring cells; populations that would enter
    /// a wall or leave the grid are bounced back, so the walls and the grid border are impermeable.
    /// </summary>
    /// <param name="steps">Number of steps, at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="steps"/> is less than 1.</exception>
    public void Advance(int steps = 1)
    {
        if (steps < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(steps), steps, "Liczba kroków musi wynosić co najmniej 1.");
        }

        for (var step = 0; step < steps; step++)
        {
            Step();
        }
    }

    private static void ValidateDiffusionCoefficient(double value, string paramName)
    {
        if (!double.IsFinite(value) || value < MinDiffusionCoefficient || value > MaxDiffusionCoefficient)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                "Współczynnik dyfuzji musi być liczbą z przedziału od 0,05 do 0,5.");
        }
    }

    private void Step()
    {
        var f = _f;
        var next = _next;
        var wall = _wall;
        var width = Width;
        var height = Height;
        var omega = 1.0 / RelaxationTime;
        var ex = D2Q9.Ex;
        var ey = D2Q9.Ey;
        var weights = D2Q9.Weights;
        var opposite = D2Q9.Opposite;

        // No clearing of the target buffer: every fluid slot is written exactly once below, and wall slots
        // are zero in both buffers (SetWall clears both, the constructor starts zeroed) and never written.

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var cell = (y * width) + x;
                if (wall[cell])
                {
                    continue;
                }

                var offset = cell * D2Q9.Q;
                double concentration = 0;
                for (var i = 0; i < D2Q9.Q; i++)
                {
                    concentration += f[offset + i];
                }

                for (var i = 0; i < D2Q9.Q; i++)
                {
                    var fi = f[offset + i];
                    var post = fi - ((fi - (weights[i] * concentration)) * omega);
                    var tx = x + ex[i];
                    var ty = y + ey[i];
                    if ((uint)tx < (uint)width && (uint)ty < (uint)height && !wall[(ty * width) + tx])
                    {
                        next[(((ty * width) + tx) * D2Q9.Q) + i] = post;
                    }
                    else
                    {
                        // Bounce-back: the population returns into its own cell with reversed direction.
                        next[offset + opposite[i]] = post;
                    }
                }
            }
        }

        (_f, _next) = (next, f);
        StepCount++;
    }

    private int CellIndex(int x, int y)
    {
        if ((uint)x >= (uint)Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "Współrzędna x leży poza siatką.");
        }

        if ((uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "Współrzędna y leży poza siatką.");
        }

        return (y * Width) + x;
    }

    private double ConcentrationAt(int cell)
    {
        double sum = 0;
        var offset = cell * D2Q9.Q;
        for (var i = 0; i < D2Q9.Q; i++)
        {
            sum += _f[offset + i];
        }

        return sum;
    }
}
