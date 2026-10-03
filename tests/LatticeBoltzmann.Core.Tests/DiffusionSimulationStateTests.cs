namespace LatticeBoltzmann.Core.Tests;

public class DiffusionSimulationStateTests
{
    [Fact]
    public void New_simulation_is_all_fluid_and_empty()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        Assert.Equal(5, sim.Width); Assert.Equal(4, sim.Height); Assert.Equal(0, sim.StepCount);
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 5; x++)
            {
                Assert.False(sim.IsWall(x, y));
                Assert.Equal(0.0, sim.GetConcentration(x, y));
            }
        Assert.Equal(0.0, sim.TotalMass);
    }

    [Fact]
    public void RelaxationTime_follows_diffusion_coefficient()
    {
        var sim = new DiffusionSimulation(5, 5, 0.2);
        Assert.Equal(1.1, sim.RelaxationTime, 12);
        sim.DiffusionCoefficient = 0.5;
        Assert.Equal(2.0, sim.RelaxationTime, 12);
    }

    [Theory]
    [InlineData(2, 5, 0.2)]
    [InlineData(5, 2, 0.2)]
    [InlineData(5, 5, 0.049)]
    [InlineData(5, 5, 0.51)]
    [InlineData(5, 5, double.NaN)]
    public void Constructor_rejects_invalid_arguments(int w, int h, double d) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiffusionSimulation(w, h, d));

    [Theory]
    [InlineData(0.04)]
    [InlineData(0.6)]
    [InlineData(double.NaN)]
    public void DiffusionCoefficient_setter_rejects_out_of_range(double d)
    {
        var sim = new DiffusionSimulation(5, 5, 0.2);
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.DiffusionCoefficient = d);
        Assert.Equal(0.2, sim.DiffusionCoefficient);
    }

    [Fact]
    public void SetConcentration_stores_value_and_adds_to_mass()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        sim.SetConcentration(2, 1, 0.7);
        Assert.Equal(0.7, sim.GetConcentration(2, 1), 15);
        Assert.Equal(0.7, sim.TotalMass, 15);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void SetConcentration_rejects_negative_or_non_finite(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiffusionSimulation(5, 4, 0.2).SetConcentration(1, 1, value));

    [Fact]
    public void SetConcentration_on_wall_throws()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        sim.SetWall(1, 1, true);
        Assert.Throws<InvalidOperationException>(() => sim.SetConcentration(1, 1, 0.5));
    }

    [Fact]
    public void Coordinates_outside_grid_throw()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.GetConcentration(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.GetConcentration(5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.GetConcentration(0, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.IsWall(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.SetWall(5, 0, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => sim.SetConcentration(0, 4, 1));
    }

    [Fact]
    public void Painting_wall_on_fluid_removes_its_content()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        sim.SetConcentration(1, 1, 0.5);
        sim.SetConcentration(2, 2, 0.3);
        sim.SetWall(1, 1, true);
        Assert.True(sim.IsWall(1, 1));
        Assert.Equal(0.0, sim.GetConcentration(1, 1));
        Assert.Equal(0.3, sim.TotalMass, 15);
    }

    [Fact]
    public void Erased_wall_becomes_empty_fluid()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        sim.SetWall(1, 1, true);
        sim.SetWall(1, 1, false);
        Assert.False(sim.IsWall(1, 1));
        Assert.Equal(0.0, sim.GetConcentration(1, 1));
        sim.SetConcentration(1, 1, 0.4);
        Assert.Equal(0.4, sim.GetConcentration(1, 1), 15);
    }

    [Fact]
    public void Erasing_fluid_cell_keeps_its_concentration()
    {
        var sim = new DiffusionSimulation(5, 4, 0.2);
        sim.SetConcentration(1, 1, 0.5);
        sim.SetWall(1, 1, false);
        Assert.Equal(0.5, sim.GetConcentration(1, 1), 15);
    }

    [Fact]
    public void CopyConcentration_is_row_major_with_zero_walls()
    {
        var sim = new DiffusionSimulation(3, 3, 0.2);
        sim.SetConcentration(2, 0, 0.4);
        sim.SetConcentration(0, 1, 0.9);
        sim.SetWall(1, 1, true);
        var buffer = new double[9];
        sim.CopyConcentration(buffer);
        Assert.Equal(new[] { 0, 0, 0.4, 0.9, 0, 0, 0, 0, 0 }, buffer.Select(v => Math.Round(v, 12)).ToArray());
    }

    [Fact]
    public void CopyConcentration_rejects_wrong_length() =>
        Assert.Throws<ArgumentException>(() => new DiffusionSimulation(3, 3, 0.2).CopyConcentration(new double[8]));
}
