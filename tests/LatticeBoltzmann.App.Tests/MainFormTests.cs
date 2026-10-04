using System.Drawing;
using System.Windows.Forms;
using LatticeBoltzmann.Core;

namespace LatticeBoltzmann.App.Tests;

public class MainFormTests
{
    [Fact]
    public void Form_starts_paused_with_default_scenario() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        Assert.False(form.IsRunning);
        Assert.Equal((200, 120, 0L), (form.Simulation.Width, form.Simulation.Height, form.Simulation.StepCount));
        Assert.Equal(0.2, form.Simulation.DiffusionCoefficient);
        Assert.Equal(10, form.StepsPerFrame);
        Assert.Equal("Krok: 0 · Pauza", form.StatusText);
    });

    [Fact]
    public void StepOnce_advances_one_step_and_updates_status() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.StepOnce();
        Assert.Equal(1, form.Simulation.StepCount);
        Assert.Equal("Krok: 1 · Pauza", form.StatusText);
    });

    [Fact]
    public void Start_and_stop_toggle_running_state() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.StartSimulation();
        Assert.True(form.IsRunning);
        Assert.Equal("Krok: 0 · Działa", form.StatusText);
        form.ToggleSimulation();
        Assert.False(form.IsRunning);
    });

    [Fact]
    public void Reset_restores_initial_state() => StaThread.Run(() =>
    {
        using var form = new MainForm();
        form.StepOnce();
        form.View.BeginStroke(new PointF(1, 1), MouseButtons.Left);
        form.ResetSimulation();
        Assert.Equal(0, form.Simulation.StepCount);
        Assert.Equal(PartitionedBox.Create(200, 120, 20, 0.2).TotalMass, form.Simulation.TotalMass, 9);
    });
}
