using System.ComponentModel;
using System.Globalization;
using LatticeBoltzmann.Core;

namespace LatticeBoltzmann.App;

/// <summary>Main window: the simulation view, the animation loop and the status bar.</summary>
internal sealed partial class MainForm : Form
{
    private const int DefaultGridWidth = 200;
    private const int DefaultGridHeight = 120;
    private const int DefaultGap = 20;
    private const double DefaultDiffusion = 0.2;
    private const int DefaultStepsPerFrame = 10;
    private const int MinStepsPerFrame = 1;
    private const int MaxStepsPerFrame = 50;
    private const int FrameIntervalMs = 16;

    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    private readonly System.Windows.Forms.Timer _timer;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;
    private int _gridWidth = DefaultGridWidth;
    private int _gridHeight = DefaultGridHeight;
    private int _gap = DefaultGap;
    private double _diffusion = DefaultDiffusion;
    private int _stepsPerFrame = DefaultStepsPerFrame;

    public MainForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Lattice Boltzmann — dyfuzja";
        MinimumSize = new Size(960, 600);

        View = new SimulationView { Dock = DockStyle.Fill };
        _statusLabel = new ToolStripStatusLabel();
        _statusStrip = new StatusStrip();
        _statusStrip.Items.Add(_statusLabel);
        Controls.Add(View);
        Controls.Add(_statusStrip);

        _timer = new System.Windows.Forms.Timer { Interval = FrameIntervalMs };
        _timer.Tick += OnTimerTick;

        ResetSimulation();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiffusionSimulation Simulation => View.Simulation!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SimulationView View { get; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsRunning => _timer.Enabled;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string StatusText =>
        $"Krok: {Simulation.StepCount.ToString("N0", Polish)} · {(IsRunning ? "Działa" : "Pauza")}";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int StepsPerFrame
    {
        get => _stepsPerFrame;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinStepsPerFrame);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxStepsPerFrame);
            _stepsPerFrame = value;
        }
    }

    public void StartSimulation()
    {
        _timer.Start();
        UpdateStatus();
    }

    public void StopSimulation()
    {
        _timer.Stop();
        UpdateStatus();
    }

    public void ToggleSimulation()
    {
        if (IsRunning)
        {
            StopSimulation();
        }
        else
        {
            StartSimulation();
        }
    }

    public void StepOnce()
    {
        Simulation.Advance();
        View.RefreshField();
        UpdateStatus();
    }

    public void ResetSimulation()
    {
        View.Simulation = PartitionedBox.Create(_gridWidth, _gridHeight, _gap, _diffusion);
        UpdateStatus();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        base.OnFormClosed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Stop();
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        Simulation.Advance(_stepsPerFrame);
        View.RefreshField();
        UpdateStatus();
    }

    private void UpdateStatus() => _statusLabel.Text = StatusText;
}
