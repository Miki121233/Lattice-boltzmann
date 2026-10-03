using LatticeBoltzmann.Core.Visualization;

namespace LatticeBoltzmann.Core.Tests;

public class ColormapTests
{
    [Fact]
    public void Endpoints_match_spec_colors()
    {
        Assert.Equal(unchecked((int)0xFFFCFCFB), Colormap.ToArgb(0.0));
        Assert.Equal(unchecked((int)0xFF0D366B), Colormap.ToArgb(1.0));
        Assert.Equal(unchecked((int)0xFF1A1A19), Colormap.WallArgb);
    }

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(double.NaN, 0.0)]
    [InlineData(2.0, 1.0)]
    public void Values_outside_unit_range_are_clamped(double value, double clamped) =>
        Assert.Equal(Colormap.ToArgb(clamped), Colormap.ToArgb(value));

    [Fact]
    public void Colors_are_opaque_and_get_darker()
    {
        var previous = double.MaxValue;
        for (var k = 0; k <= 255; k++)
        {
            var argb = Colormap.ToArgb(k / 255.0);
            Assert.Equal(255, (argb >> 24) & 0xFF);
            var luminance = RelativeLuminance(argb);
            Assert.True(luminance <= previous + 1e-12, $"luminance rose at {k}");
            previous = luminance;
        }
    }

    private static double RelativeLuminance(int argb)
    {
        static double Lin(int c) { var s = c / 255.0; return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
        return 0.2126 * Lin((argb >> 16) & 0xFF) + 0.7152 * Lin((argb >> 8) & 0xFF) + 0.0722 * Lin(argb & 0xFF);
    }
}
