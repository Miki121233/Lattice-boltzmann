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
    private bool _updatingControls;

    public MainForm()
    {
        // Designer pattern: everything is built in logical (96 DPI) pixels and WinForms scales it once on ResumeLayout.
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Lattice Boltzmann — dyfuzja";

        View = new SimulationView();
        _statusLabel = new ToolStripStatusLabel();
        _statusStrip = new StatusStrip();
        _statusStrip.Items.Add(_statusLabel);
        BuildLayout();
        MinimumSize = new Size(960, 600);
        ClientSize = new Size(1200, 760);
        ResumeLayout(false);
        PerformLayout();

        _timer = new System.Windows.Forms.Timer { Interval = FrameIntervalMs };
        _timer.Tick += OnTimerTick;

        ResetSimulation();
        UpdateDiffusionLabel();
        UpdateGapLabel();
        UpdateBrushLabel();
        ConnectControls();
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
            if (value < MinStepsPerFrame || value > MaxStepsPerFrame)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    $"Liczba kroków na klatkę musi być z przedziału od {MinStepsPerFrame} do {MaxStepsPerFrame}.");
            }

            _stepsPerFrame = value;
        }
    }

    public void StartSimulation()
    {
        _timer.Start();
        StartPauseButton.Text = "Pauza";
        UpdateStatus();
    }

    public void StopSimulation()
    {
        _timer.Stop();
        StartPauseButton.Text = "Start";
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

    internal bool HandleShortcut(Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Space:
                ToggleSimulation();
                return true;
            case Keys.N:
                StepOnce();
                return true;
            case Keys.R:
                ResetSimulation();
                return true;
            default:
                return false;
        }
    }

    // Space/N/R are intercepted form-wide by design (spec 4.1), so Space toggles Start/Pauza
    // even when another button has focus.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) =>
        HandleShortcut(keyData) || base.ProcessCmdKey(ref msg, keyData);

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
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        Simulation.Advance(_stepsPerFrame);
        View.RefreshField();
        UpdateStatus();
    }

    private void ConnectControls()
    {
        StartPauseButton.Click += (_, _) => ToggleSimulation();
        StepButton.Click += (_, _) => StepOnce();
        ResetButton.Click += (_, _) => ResetSimulation();
        DiffusionTrackBar.ValueChanged += OnDiffusionChanged;
        GapTrackBar.ValueChanged += OnGapChanged;
        ResolutionComboBox.SelectedIndexChanged += OnResolutionChanged;
        StepsPerFrameInput.ValueChanged += (_, _) => StepsPerFrame = (int)StepsPerFrameInput.Value;
        BrushTrackBar.ValueChanged += OnBrushChanged;
    }

    private void OnDiffusionChanged(object? sender, EventArgs e)
    {
        _diffusion = DiffusionTrackBar.Value / 100.0;
        Simulation.DiffusionCoefficient = _diffusion;
        UpdateDiffusionLabel();
    }

    private void OnGapChanged(object? sender, EventArgs e)
    {
        if (_updatingControls)
        {
            return;
        }

        _gap = GapTrackBar.Value;
        PartitionedBox.ApplyGap(Simulation, _gap);
        View.RefreshField();
        UpdateGapLabel();
    }

    private void OnResolutionChanged(object? sender, EventArgs e)
    {
        var index = ResolutionComboBox.SelectedIndex;
        if (index < 0)
        {
            return;
        }

        var (newWidth, newHeight) = Resolutions[index];
        var newGap = Math.Clamp((int)Math.Round(_gap * newHeight / (double)_gridHeight), 0, newHeight / 2);
        _gridWidth = newWidth;
        _gridHeight = newHeight;
        _gap = newGap;
        ResetSimulation();

        _updatingControls = true;
        try
        {
            GapTrackBar.Maximum = newHeight / 2;
            GapTrackBar.Value = newGap;
        }
        finally
        {
            _updatingControls = false;
        }

        UpdateGapLabel();
    }

    private void OnBrushChanged(object? sender, EventArgs e)
    {
        View.BrushSize = BrushTrackBar.Value;
        UpdateBrushLabel();
    }

    private void UpdateDiffusionLabel() =>
        DiffusionLabel.Text = $"D = {_diffusion.ToString("0.00", Polish)} (τ = {Simulation.RelaxationTime.ToString("0.00", Polish)})";

    private void UpdateGapLabel() => GapLabel.Text = $"Szerokość otworu: {_gap}";

    private void UpdateBrushLabel() => BrushLabel.Text = $"Rozmiar pędzla: {View.BrushSize}";

    private void UpdateStatus() => _statusLabel.Text = StatusText;
}
