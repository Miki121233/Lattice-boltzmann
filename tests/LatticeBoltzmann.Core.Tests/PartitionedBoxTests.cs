namespace LatticeBoltzmann.Core.Tests;

public class PartitionedBoxTests
{
    [Theory]
    [InlineData(200, 66)]
    [InlineData(90, 30)]
    public void PartitionColumn_is_one_third_of_width(int width, int expected) =>
        Assert.Equal(expected, PartitionedBox.PartitionColumn(width));

    [Fact]
    public void Create_builds_partition_with_centered_gap()
    {
        var sim = PartitionedBox.Create(90, 40, 10, 0.2);
        for (var y = 0; y < 40; y++)
            Assert.Equal(y < 15 || y > 24, sim.IsWall(30, y));
        for (var x = 0; x < 90; x++) for (var y = 0; y < 40; y++)
            if (x != 30) Assert.False(sim.IsWall(x, y));
    }

    [Fact]
    public void Create_fills_left_chamber_only()
    {
        var sim = PartitionedBox.Create(90, 40, 10, 0.2);
        for (var x = 0; x < 90; x++) for (var y = 0; y < 40; y++)
            if (!sim.IsWall(x, y)) Assert.Equal(x < 30 ? 1.0 : 0.0, sim.GetConcentration(x, y), 15);
        Assert.Equal(1200.0, sim.TotalMass, 9);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(41)]
    public void Gap_outside_range_is_rejected(int gap)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PartitionedBox.Create(90, 40, gap, 0.2));
        var sim = PartitionedBox.Create(90, 40, 10, 0.2);
        Assert.Throws<ArgumentOutOfRangeException>(() => PartitionedBox.ApplyGap(sim, gap));
    }

    [Fact]
    public void ApplyGap_rebuilds_only_partition_column()
    {
        var sim = PartitionedBox.Create(90, 40, 10, 0.2);
        sim.SetWall(60, 5, true);
        PartitionedBox.ApplyGap(sim, 20);
        for (var y = 0; y < 40; y++)
            Assert.Equal(y < 10 || y > 29, sim.IsWall(30, y));
        Assert.True(sim.IsWall(60, 5));
    }

    [Fact]
    public void Closed_gap_keeps_right_chamber_empty()
    {
        var sim = PartitionedBox.Create(90, 40, 0, 0.3);
        sim.Advance(1000);
        for (var x = 31; x < 90; x++) for (var y = 0; y < 40; y++)
            Assert.Equal(0.0, sim.GetConcentration(x, y));
    }

    [Fact]
    public void Field_stays_symmetric_about_gap_axis()
    {
        var sim = PartitionedBox.Create(90, 40, 10, 0.2);
        sim.Advance(500);
        for (var x = 0; x < 90; x++) for (var y = 0; y < 20; y++)
            Assert.Equal(sim.GetConcentration(x, y), sim.GetConcentration(x, 39 - y), 12);
    }

    [Fact]
    public void ApplyGap_leaves_concentration_outside_partition_untouched()
    {
        var sim = PartitionedBox.Create(90, 40, 10, 0.2);
        sim.Advance(50);
        var before = new double[90 * 40];
        sim.CopyConcentration(before);

        PartitionedBox.ApplyGap(sim, 30);

        var after = new double[90 * 40];
        sim.CopyConcentration(after);
        for (var x = 0; x < 90; x++) for (var y = 0; y < 40; y++)
            if (x != 30) Assert.Equal(before[(y * 90) + x], after[(y * 90) + x]);
    }

    [Fact]
    public void Rejected_gap_leaves_partition_unchanged()
    {
        var sim = PartitionedBox.Create(90, 40, 10, 0.2);
        Assert.Throws<ArgumentOutOfRangeException>(() => PartitionedBox.ApplyGap(sim, 41));
        for (var y = 0; y < 40; y++)
            Assert.Equal(y < 15 || y > 24, sim.IsWall(30, y));
    }

    [Fact]
    public void ApplyGap_rejects_null_simulation() =>
        Assert.Throws<ArgumentNullException>(() => PartitionedBox.ApplyGap(null!, 10));

    [Theory]
    [InlineData(0)]
    [InlineData(40)]
    public void Gap_range_limits_are_accepted(int gap)
    {
        var sim = PartitionedBox.Create(90, 40, gap, 0.2);
        for (var y = 0; y < 40; y++)
            Assert.Equal(gap == 0, sim.IsWall(30, y));
    }
}
