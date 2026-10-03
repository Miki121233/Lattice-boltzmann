using System.Diagnostics.CodeAnalysis;

namespace LatticeBoltzmann.Core;

/// <summary>
/// Constants of the D2Q9 lattice: nine velocity directions in a fixed order
/// (rest, E, S, W, N, SE, SW, NW, NE with the y axis pointing down).
/// </summary>
public static class D2Q9
{
    /// <summary>Number of lattice directions.</summary>
    public const int Q = 9;

    /// <summary>Squared lattice speed of sound, c_s^2 = 1/3.</summary>
    public const double SoundSpeedSquared = 1.0 / 3.0;

    /// <summary>X components of the direction vectors.</summary>
    [SuppressMessage(
        "Naming",
        "CA1711:Identifiers should not have incorrect suffix",
        Justification = "Ex/Ey are the conventional LBM names of the lattice velocity components, not an 'extended' API.")]
    public static ReadOnlySpan<int> Ex => [0, 1, 0, -1, 0, 1, -1, -1, 1];

    /// <summary>Y components of the direction vectors.</summary>
    public static ReadOnlySpan<int> Ey => [0, 0, 1, 0, -1, 1, 1, -1, -1];

    /// <summary>Equilibrium weights of the directions.</summary>
    public static ReadOnlySpan<double> Weights =>
    [
        4.0 / 9,
        1.0 / 9, 1.0 / 9, 1.0 / 9, 1.0 / 9,
        1.0 / 36, 1.0 / 36, 1.0 / 36, 1.0 / 36,
    ];

    /// <summary>Index of the direction opposite to each direction.</summary>
    public static ReadOnlySpan<int> Opposite => [0, 3, 4, 1, 2, 7, 8, 5, 6];
}
