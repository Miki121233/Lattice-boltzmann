namespace LatticeBoltzmann.Core.Visualization;

/// <summary>
/// Sequential single-hue (blue) colour map for concentration: light for 0, dark for 1.
/// Colours are 32-bit ARGB integers (0xAARRGGBB) and always opaque.
/// </summary>
public static class Colormap
{
    /// <summary>Opaque colour #1a1a19 used for wall cells; it is clearly separated from every step of the ramp.</summary>
    public const int WallArgb = unchecked((int)0xFF1A1A19);

    private const int TableSize = 256;
    private const int OpaqueAlpha = unchecked((int)0xFF000000);

    // Control points are spread evenly over [0, 1]: background followed by the 100-700 ramp. Declared before
    // Table because static initializers run in textual order.
    private static readonly int[] ControlPoints =
    [
        0xFCFCFB, 0xCDE2FB, 0xB7D3F6, 0x9EC5F4, 0x86B6EF, 0x6DA7EC, 0x5598E7,
        0x3987E5, 0x2A78D6, 0x256ABF, 0x1C5CAB, 0x184F95, 0x104281, 0x0D366B,
    ];

    private static readonly int[] Table = BuildTable();

    /// <summary>Maps a concentration to its colour. Values below 0 and NaN map to 0, values above 1 map to 1.</summary>
    /// <param name="value">Concentration, normally within [0, 1].</param>
    /// <returns>Opaque colour as 0xAARRGGBB.</returns>
    public static int ToArgb(double value)
    {
        if (double.IsNaN(value))
        {
            return Table[0];
        }

        return Table[(int)Math.Round(Math.Clamp(value, 0.0, 1.0) * (TableSize - 1))];
    }

    private static int[] BuildTable()
    {
        // Exact integer arithmetic: entry k sits at k / 255 of the ramp, i.e. at k * segments / 255 control-point
        // units. 255 is odd, so a weighted average of two integers is never an exact half and rounding is unambiguous.
        var last = TableSize - 1;
        var segments = ControlPoints.Length - 1;
        var table = new int[TableSize];
        for (var k = 0; k < TableSize; k++)
        {
            var scaled = k * segments;
            var segment = Math.Min(scaled / last, segments - 1);
            var weight = scaled - (segment * last);
            table[k] = OpaqueAlpha
                | (Blend(ControlPoints[segment], ControlPoints[segment + 1], weight, 16) << 16)
                | (Blend(ControlPoints[segment], ControlPoints[segment + 1], weight, 8) << 8)
                | Blend(ControlPoints[segment], ControlPoints[segment + 1], weight, 0);
        }

        return table;
    }

    private static int Blend(int from, int to, int weight, int shift)
    {
        var a = (from >> shift) & 0xFF;
        var b = (to >> shift) & 0xFF;
        var last = TableSize - 1;
        return ((a * (last - weight)) + (b * weight) + (last / 2)) / last;
    }
}
