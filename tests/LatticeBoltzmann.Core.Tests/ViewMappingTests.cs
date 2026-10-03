using LatticeBoltzmann.Core.Visualization;

namespace LatticeBoltzmann.Core.Tests;

public class ViewMappingTests
{
    [Fact]
    public void Wide_view_is_centered_horizontally()
    {
        var m = new ViewMapping(400, 100, 200, 100);
        Assert.Equal((1.0, 100.0, 0.0, 200.0, 100.0), (m.Scale, m.OffsetX, m.OffsetY, m.DrawWidth, m.DrawHeight));
    }

    [Fact]
    public void Tall_view_is_centered_vertically()
    {
        var m = new ViewMapping(100, 400, 50, 100);
        Assert.Equal((2.0, 0.0, 100.0, 100.0, 200.0), (m.Scale, m.OffsetX, m.OffsetY, m.DrawWidth, m.DrawHeight));
    }

    [Theory]
    [InlineData(100.0, 0.0, 0, 0)]
    [InlineData(299.9, 99.9, 199, 99)]
    [InlineData(150.5, 10.2, 50, 10)]
    public void Points_inside_image_map_to_cells(double px, double py, int ex, int ey)
    {
        Assert.True(new ViewMapping(400, 100, 200, 100).TryGetCell(px, py, out var x, out var y));
        Assert.Equal((ex, ey), (x, y));
    }

    [Theory]
    [InlineData(99.9, 50.0)]
    [InlineData(300.0, 50.0)]
    [InlineData(150.0, -0.1)]
    [InlineData(150.0, 100.0)]
    public void Points_outside_image_map_to_nothing(double px, double py) =>
        Assert.False(new ViewMapping(400, 100, 200, 100).TryGetCell(px, py, out _, out _));

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(-5.0, 10.0)]
    public void Empty_view_maps_nothing(double vw, double vh)
    {
        var m = new ViewMapping(vw, vh, 200, 100);
        Assert.Equal(0.0, m.Scale);
        Assert.False(m.TryGetCell(0, 0, out _, out _));
    }

    [Fact]
    public void Default_mapping_and_non_finite_pixels_map_nothing()
    {
        Assert.False(default(ViewMapping).TryGetCell(0, 0, out _, out _));
        var m = new ViewMapping(400, 100, 200, 100);
        Assert.False(m.TryGetCell(double.NaN, 50, out _, out _));
        Assert.False(m.TryGetCell(150, double.PositiveInfinity, out _, out _));
    }

    [Fact]
    public void Non_positive_grid_is_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ViewMapping(100, 100, 0, 10));
}
