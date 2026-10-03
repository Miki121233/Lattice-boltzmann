using LatticeBoltzmann.Core.Visualization;

namespace LatticeBoltzmann.Core.Tests;

public class FieldRendererTests
{
    [Fact]
    public void Walls_use_wall_color_and_fluid_uses_colormap()
    {
        var sim = new DiffusionSimulation(3, 3, 0.2);
        sim.SetConcentration(0, 0, 1.0);
        sim.SetWall(1, 0, true);
        var argb = new int[9];
        FieldRenderer.Render(sim, argb);
        Assert.Equal(Colormap.ToArgb(1.0), argb[0]);
        Assert.Equal(Colormap.WallArgb, argb[1]);
        Assert.All(argb[2..], v => Assert.Equal(Colormap.ToArgb(0.0), v));
    }

    [Fact]
    public void Wrong_buffer_length_is_rejected() =>
        Assert.Throws<ArgumentException>(() => FieldRenderer.Render(new DiffusionSimulation(3, 3, 0.2), new int[8]));
}
