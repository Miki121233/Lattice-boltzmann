using LatticeBoltzmann.Core.Visualization;

namespace LatticeBoltzmann.Core.Tests;

public class BrushStrokeTests
{
    [Fact]
    public void Single_point_with_size_one_is_one_cell() =>
        Assert.Equal(new[] { (2, 2) }, BrushStroke.Cells(2, 2, 2, 2, 1, 10, 10));

    [Fact]
    public void Diagonal_line_steps_along_x_first() =>
        Assert.Equal(new[] { (0, 0), (1, 0), (1, 1), (2, 1), (2, 2) }, BrushStroke.Cells(0, 0, 2, 2, 1, 10, 10));

    [Fact]
    public void Line_is_four_connected()
    {
        var cells = BrushStroke.Cells(0, 0, 7, 3, 1, 10, 10);
        Assert.Equal((0, 0), cells[0]);
        Assert.Equal((7, 3), cells[^1]);
        for (var k = 1; k < cells.Count; k++)
            Assert.Equal(1, Math.Abs(cells[k].X - cells[k - 1].X) + Math.Abs(cells[k].Y - cells[k - 1].Y));
    }

    [Fact]
    public void Line_is_four_connected_between_any_two_cells()
    {
        for (var x0 = 0; x0 < 6; x0++) for (var y0 = 0; y0 < 6; y0++)
        for (var x1 = 0; x1 < 6; x1++) for (var y1 = 0; y1 < 6; y1++)
        {
            var cells = BrushStroke.Cells(x0, y0, x1, y1, 1, 10, 10);
            Assert.Equal((x0, y0), cells[0]);
            Assert.Equal((x1, y1), cells[^1]);
            for (var k = 1; k < cells.Count; k++)
                Assert.Equal(1, Math.Abs(cells[k].X - cells[k - 1].X) + Math.Abs(cells[k].Y - cells[k - 1].Y));
        }
    }

    [Fact]
    public void Stroke_crossing_outside_the_grid_never_returns_outside_cells()
    {
        var cells = BrushStroke.Cells(-5, -5, 15, 12, BrushStroke.MaxSize, 10, 10);
        Assert.NotEmpty(cells);
        Assert.All(cells, c => Assert.True(c.X is >= 0 and < 10 && c.Y is >= 0 and < 10));
    }

    [Fact]
    public void Stroke_far_outside_the_grid_is_empty() =>
        Assert.Empty(BrushStroke.Cells(-100, -100, -50, -90, BrushStroke.MaxSize, 10, 10));

    [Fact]
    public void Size_two_is_plus_shape() =>
        Assert.Equal(new HashSet<(int, int)> { (5, 5), (4, 5), (6, 5), (5, 4), (5, 6) },
                     BrushStroke.Cells(5, 5, 5, 5, 2, 10, 10).ToHashSet());

    [Fact]
    public void Cells_are_clipped_to_grid() =>
        Assert.Equal(new HashSet<(int, int)> { (0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (0, 2) },
                     BrushStroke.Cells(0, 0, 0, 0, 3, 10, 10).ToHashSet());

    [Fact]
    public void Cells_are_unique()
    {
        var cells = BrushStroke.Cells(1, 1, 6, 1, 3, 10, 10);
        Assert.Equal(cells.Count, cells.Distinct().Count());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void Invalid_size_is_rejected(int size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => BrushStroke.Cells(0, 0, 1, 1, size, 10, 10));

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    [InlineData(-1, 10)]
    [InlineData(10, -1)]
    [InlineData(-5, -5)]
    public void Non_positive_grid_is_rejected(int gridWidth, int gridHeight) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => BrushStroke.Cells(0, 0, 3, 3, 1, gridWidth, gridHeight));

    [Fact]
    public void Drawn_diagonal_wall_is_impermeable()
    {
        var sim = new DiffusionSimulation(60, 60, 1.0 / 6.0);
        foreach (var (x, y) in BrushStroke.Cells(0, 0, 59, 59, 1, 60, 60)) sim.SetWall(x, y, true);
        for (var x = 0; x < 60; x++) for (var y = 0; y < x; y++)
            if (!sim.IsWall(x, y)) sim.SetConcentration(x, y, 1.0);
        sim.Advance(200);
        for (var x = 0; x < 60; x++) for (var y = x + 1; y < 60; y++)
            Assert.Equal(0.0, sim.GetConcentration(x, y));
    }
}
