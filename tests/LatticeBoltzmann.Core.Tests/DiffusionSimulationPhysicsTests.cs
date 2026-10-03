namespace LatticeBoltzmann.Core.Tests;

public class DiffusionSimulationPhysicsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Advance_rejects_non_positive_steps(int steps) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiffusionSimulation(5, 5, 0.2).Advance(steps));

    [Fact]
    public void Advance_counts_steps()
    {
        var sim = new DiffusionSimulation(5, 5, 0.2);
        sim.Advance(3);
        sim.Advance();
        Assert.Equal(4, sim.StepCount);
    }

    [Fact]
    public void Uniform_equilibrium_stays_uniform()
    {
        var sim = new DiffusionSimulation(20, 10, 0.2);
        Fill(sim, (_, _) => 0.7);
        sim.Advance(100);
        ForEachFluid(sim, (x, y) => Assert.Equal(0.7, sim.GetConcentration(x, y), 12));
    }

    [Fact]
    public void Mass_is_conserved_with_walls()
    {
        var sim = new DiffusionSimulation(60, 40, 0.3);
        for (var y = 0; y < 40; y++) if (y < 15 || y > 24) sim.SetWall(20, y, true);
        for (var x = 40; x <= 44; x++) for (var y = 5; y <= 9; y++) sim.SetWall(x, y, true);
        Fill(sim, (x, _) => x < 20 ? 1.0 : 0.0);
        var m0 = sim.TotalMass;
        sim.Advance(2000);
        Assert.True(Math.Abs(sim.TotalMass - m0) / m0 < 1e-12);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.0 / 6.0)]
    [InlineData(0.3)]
    [InlineData(0.5)]
    public void Variance_grows_at_rate_two_D(double d)
    {
        var sim = new DiffusionSimulation(121, 121, d);
        sim.SetConcentration(60, 60, 1.0);
        sim.Advance(50);
        var v1 = VarianceX(sim);
        sim.Advance(100);
        var v2 = VarianceX(sim);
        var slope = (v2 - v1) / (2.0 * 100);
        Assert.InRange(slope, d * 0.99, d * 1.01);
    }

    [Theory]
    [InlineData(1.0 / 6.0)]
    [InlineData(0.3)]
    public void Concentration_stays_in_unit_range_when_tau_at_least_one(double d)
    {
        var sim = new DiffusionSimulation(60, 40, d);
        Fill(sim, (x, _) => x < 20 ? 1.0 : 0.0);
        sim.Advance(1000);
        ForEachFluid(sim, (x, y) => Assert.InRange(sim.GetConcentration(x, y), 0.0, 1.0 + 1e-12));
    }

    [Fact]
    public void Full_height_wall_is_impermeable()
    {
        var sim = new DiffusionSimulation(30, 10, 0.3);
        for (var y = 0; y < 10; y++) sim.SetWall(10, y, true);
        Fill(sim, (x, _) => x < 10 ? 1.0 : 0.0);
        sim.Advance(500);
        for (var x = 11; x < 30; x++) for (var y = 0; y < 10; y++)
            Assert.Equal(0.0, sim.GetConcentration(x, y));
    }

    [Fact]
    public void All_wall_grid_stays_empty()
    {
        var sim = new DiffusionSimulation(4, 4, 0.2);
        for (var x = 0; x < 4; x++) for (var y = 0; y < 4; y++) sim.SetWall(x, y, true);
        sim.Advance(10);
        Assert.Equal(0.0, sim.TotalMass);
    }

    [Fact]
    public void Wall_painted_mid_run_stays_empty_and_conserves_remaining_mass()
    {
        var sim = new DiffusionSimulation(30, 10, 0.3);
        Fill(sim, (x, _) => x < 10 ? 1.0 : 0.0);
        sim.Advance(5);
        var massBefore = sim.TotalMass;
        var removed = sim.GetConcentration(5, 5);

        sim.SetWall(5, 5, true);

        var massAfterPaint = sim.TotalMass;
        Assert.InRange(massAfterPaint, massBefore - removed - 1e-12, massBefore - removed + 1e-12);

        // An odd number of steps swaps the buffers an odd number of times, so a wall that was
        // cleared in only one of them would show stale populations here.
        sim.Advance(7);

        Assert.Equal(0.0, sim.GetConcentration(5, 5));
        Assert.True(Math.Abs(sim.TotalMass - massAfterPaint) / massAfterPaint < 1e-12);
    }

    internal static void Fill(DiffusionSimulation sim, Func<int, int, double> c) =>
        ForEachFluid(sim, (x, y) => sim.SetConcentration(x, y, c(x, y)));

    internal static void ForEachFluid(DiffusionSimulation sim, Action<int, int> action)
    {
        for (var y = 0; y < sim.Height; y++)
            for (var x = 0; x < sim.Width; x++)
                if (!sim.IsWall(x, y)) action(x, y);
    }

    private static double VarianceX(DiffusionSimulation sim)
    {
        double m = 0, mx = 0, mxx = 0;
        ForEachFluid(sim, (x, y) => { var c = sim.GetConcentration(x, y); m += c; mx += c * x; mxx += c * x * x; });
        var mean = mx / m;
        return mxx / m - mean * mean;
    }
}
