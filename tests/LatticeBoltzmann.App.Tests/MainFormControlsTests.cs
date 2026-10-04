using System.Windows.Forms;
using LatticeBoltzmann.Core;

namespace LatticeBoltzmann.App.Tests;

public class MainFormControlsTests
{
    private static readonly string[] ExpectedResolutions = ["120 × 72", "200 × 120", "300 × 180", "400 × 240"];
    private static readonly int[] ResolutionSequence = [3, 0, 2, 1, 0, 3];

    [Fact]
    public void Controls_show_default_values() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        Assert.Equal("Start", form.StartPauseButton.Text);
        Assert.Equal(20, form.DiffusionTrackBar.Value);
        Assert.Equal("D = 0,20 (τ = 1,10)", form.DiffusionLabel.Text);
        Assert.Equal(10, (int)form.StepsPerFrameInput.Value);
        Assert.Equal((20, 60), (form.GapTrackBar.Value, form.GapTrackBar.Maximum));
        Assert.Equal("Szerokość otworu: 20", form.GapLabel.Text);
        Assert.Equal(ExpectedResolutions, form.ResolutionComboBox.Items.Cast<string>().ToArray());
        Assert.Equal(1, form.ResolutionComboBox.SelectedIndex);
        Assert.Equal(2, form.BrushTrackBar.Value);
        Assert.Equal("Rozmiar pędzla: 2", form.BrushLabel.Text);
    });

    [Fact]
    public void Diffusion_slider_updates_simulation_live() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.StepOnce();
        form.DiffusionTrackBar.Value = 50;
        Assert.Equal(0.5, form.Simulation.DiffusionCoefficient, 12);
        Assert.Equal("D = 0,50 (τ = 2,00)", form.DiffusionLabel.Text);
        Assert.Equal(1, form.Simulation.StepCount);
    });

    [Fact]
    public void Gap_slider_rebuilds_partition_live() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.GapTrackBar.Value = 0;
        var column = PartitionedBox.PartitionColumn(form.Simulation.Width);
        for (var y = 0; y < form.Simulation.Height; y++) Assert.True(form.Simulation.IsWall(column, y));
    });

    [Fact]
    public void Steps_and_brush_inputs_apply() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.StepsPerFrameInput.Value = 50;
        form.BrushTrackBar.Value = 5;
        Assert.Equal(50, form.StepsPerFrame);
        Assert.Equal(5, form.View.BrushSize);
    });

    [Fact]
    public void Resolution_change_resets_and_scales_gap() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.StepOnce();
        form.ResolutionComboBox.SelectedIndex = 0;
        Assert.Equal((120, 72, 0L), (form.Simulation.Width, form.Simulation.Height, form.Simulation.StepCount));
        Assert.Equal((12, 36), (form.GapTrackBar.Value, form.GapTrackBar.Maximum));
    });

    [Fact]
    public void Gap_at_maximum_survives_every_resolution() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        foreach (var index in ResolutionSequence)
        {
            form.GapTrackBar.Value = form.GapTrackBar.Maximum;
            form.ResolutionComboBox.SelectedIndex = index;
            Assert.InRange(form.GapTrackBar.Value, 0, form.GapTrackBar.Maximum);
            Assert.Equal(form.Simulation.Height / 2, form.GapTrackBar.Maximum);
        }
    });

    [Fact]
    public void Shortcuts_control_the_simulation() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        Assert.True(form.HandleShortcut(Keys.Space));
        Assert.True(form.IsRunning);
        Assert.Equal("Pauza", form.StartPauseButton.Text);
        Assert.True(form.HandleShortcut(Keys.Space));
        Assert.False(form.IsRunning);
        Assert.True(form.HandleShortcut(Keys.N));
        Assert.Equal(1, form.Simulation.StepCount);
        Assert.True(form.HandleShortcut(Keys.R));
        Assert.Equal(0, form.Simulation.StepCount);
        Assert.False(form.HandleShortcut(Keys.A));
    });
}
