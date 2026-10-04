using System.Drawing;
using System.Windows.Forms;
using LatticeBoltzmann.Core;
using LatticeBoltzmann.Core.Visualization;

namespace LatticeBoltzmann.App.Tests;

public class SimulationViewTests
{
    private static SimulationView CreateView(int w = 200, int h = 120) => new()
    {
        Size = new Size(w, h),
        Simulation = PartitionedBox.Create(w, h, 20, 0.2),
    };

    [Fact]
    public void RefreshField_renders_simulation_colors() => StaThread.Run(() =>
    {
        using var view = CreateView();
        view.RefreshField();
        Assert.Equal(Colormap.ToArgb(1.0), view.FieldBitmap!.GetPixel(10, 60).ToArgb());
        Assert.Equal(Colormap.ToArgb(0.0), view.FieldBitmap.GetPixel(190, 60).ToArgb());
        Assert.Equal(Colormap.WallArgb, view.FieldBitmap.GetPixel(66, 5).ToArgb());
    });

    [Fact]
    public void View_paints_into_bitmap() => StaThread.Run(() =>
    {
        using var view = CreateView();
        view.RefreshField();
        using var bmp = new Bitmap(200, 120);
        view.DrawToBitmap(bmp, new Rectangle(0, 0, 200, 120));
        Assert.NotEqual(bmp.GetPixel(10, 60).ToArgb(), bmp.GetPixel(190, 60).ToArgb());
    });

    [Fact]
    public void Left_drag_draws_wall_and_right_drag_erases_it() => StaThread.Run(() =>
    {
        using var view = CreateView();
        view.BrushSize = 1;
        view.BeginStroke(new PointF(150.5f, 10.5f), MouseButtons.Left);
        view.ContinueStroke(new PointF(150.5f, 30.5f));
        view.EndStroke();
        for (var y = 10; y <= 30; y++) Assert.True(view.Simulation!.IsWall(150, y));
        view.BeginStroke(new PointF(150.5f, 20.5f), MouseButtons.Right);
        view.EndStroke();
        Assert.False(view.Simulation!.IsWall(150, 20));
    });

    [Fact]
    public void Stroke_restarts_after_leaving_the_image() => StaThread.Run(() =>
    {
        using var view = CreateView(200, 120);
        view.Size = new Size(400, 120);              // image at x 100..300
        view.BrushSize = 1;
        view.BeginStroke(new PointF(120.5f, 10.5f), MouseButtons.Left);   // cell (20, 10)
        view.ContinueStroke(new PointF(50f, 10.5f));                      // outside
        view.ContinueStroke(new PointF(180.5f, 10.5f));                   // cell (80, 10)
        view.EndStroke();
        Assert.True(view.Simulation!.IsWall(20, 10));
        Assert.True(view.Simulation.IsWall(80, 10));
        Assert.False(view.Simulation.IsWall(50, 10));
    });

    [Fact]
    public void Replacing_simulation_during_drag_is_safe() => StaThread.Run(() =>
    {
        using var view = CreateView(400, 240);
        view.BeginStroke(new PointF(390.5f, 230.5f), MouseButtons.Left);
        view.Simulation = PartitionedBox.Create(120, 72, 12, 0.2);
        view.ContinueStroke(new PointF(10.5f, 10.5f));
        view.EndStroke();
        view.RefreshField();
        Assert.Equal(new Size(120, 72), view.FieldBitmap!.Size);
    });

    [Fact]
    public void Zero_sized_view_refreshes_and_paints_without_error() => StaThread.Run(() =>
    {
        using var view = CreateView();
        view.Size = Size.Empty;
        view.RefreshField();
        Assert.Equal(new Size(200, 120), view.FieldBitmap!.Size);
        view.BeginStroke(new PointF(0, 0), MouseButtons.Left);
        view.ContinueStroke(new PointF(0, 0));
        view.EndStroke();
        using var bmp = new Bitmap(1, 1);
        using var g = Graphics.FromImage(bmp);
        view.PaintField(g);
    });
}
