using System.ComponentModel;
using LatticeBoltzmann.Core;

namespace LatticeBoltzmann.App;

internal sealed partial class MainForm
{
    private const int PanelWidth = 280;
    private const int ControlWidth = 210;

    // Every group has this one width: panel minus a 17 px vertical scrollbar and the 3 px margins of the flow panel.
    private const int GroupWidth = PanelWidth - 17 - 6 - 2;

    private static readonly (int Width, int Height)[] Resolutions = [(120, 72), (200, 120), (300, 180), (400, 240)];

    private readonly ToolTip _toolTip = new();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button StartPauseButton { get; private set; } = null!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button StepButton { get; private set; } = null!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Button ResetButton { get; private set; } = null!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TrackBar DiffusionTrackBar { get; private set; } = null!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Label DiffusionLabel { get; private set; } = null!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public NumericUpDown StepsPerFrameInput { get; private set; } = null!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TrackBar GapTrackBar { get; private set; } = null!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Label GapLabel { get; private set; } = null!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ComboBox ResolutionComboBox { get; private set; } = null!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TrackBar BrushTrackBar { get; private set; } = null!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Label BrushLabel { get; private set; } = null!;

    private void BuildLayout()
    {
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, PanelWidth));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        View.Dock = DockStyle.Fill;
        View.Margin = Padding.Empty;
        table.Controls.Add(View, 0, 0);

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
        };
        // Only vertical scrolling may ever appear.
        panel.HorizontalScroll.Enabled = false;
        panel.HorizontalScroll.Visible = false;
        panel.Controls.Add(BuildSimulationGroup());
        panel.Controls.Add(BuildParametersGroup());
        panel.Controls.Add(BuildDrawingGroup());
        panel.Controls.Add(BuildLegendGroup());
        table.Controls.Add(panel, 1, 0);

        _statusStrip.Dock = DockStyle.Bottom;
        Controls.Add(table);
        Controls.Add(_statusStrip);
    }

    private GroupBox BuildSimulationGroup()
    {
        StartPauseButton = CreateButton("Start", "Start / pauza (Spacja)");
        StepButton = CreateButton("Krok", "Jeden krok (N)");
        ResetButton = CreateButton("Reset", "Przywróć stan początkowy (R)");

        var row = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        row.Controls.AddRange([StartPauseButton, StepButton, ResetButton]);
        return CreateGroup("Symulacja", row);
    }

    private GroupBox BuildParametersGroup()
    {
        DiffusionTrackBar = CreateTrackBar(5, 50, (int)Math.Round(DefaultDiffusion * 100));
        DiffusionLabel = CreateLabel(string.Empty);
        StepsPerFrameInput = new NumericUpDown
        {
            Minimum = MinStepsPerFrame,
            Maximum = MaxStepsPerFrame,
            Value = DefaultStepsPerFrame,
            Width = 80,
        };
        GapTrackBar = CreateTrackBar(0, DefaultGridHeight / 2, DefaultGap);
        GapLabel = CreateLabel(string.Empty);
        ResolutionComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = ControlWidth };
        foreach (var (width, height) in Resolutions)
        {
            ResolutionComboBox.Items.Add($"{width} × {height}");
        }

        ResolutionComboBox.SelectedIndex = Array.IndexOf(Resolutions, (DefaultGridWidth, DefaultGridHeight));

        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        flow.Controls.AddRange(
        [
            CreateLabel("Współczynnik dyfuzji"),
            DiffusionLabel,
            DiffusionTrackBar,
            CreateLabel("Kroki na klatkę"),
            StepsPerFrameInput,
            GapLabel,
            GapTrackBar,
            CreateLabel("Rozdzielczość"),
            ResolutionComboBox,
        ]);
        return CreateGroup("Parametry", flow);
    }

    private GroupBox BuildDrawingGroup()
    {
        BrushTrackBar = CreateTrackBar(1, 8, View.BrushSize);
        BrushLabel = CreateLabel(string.Empty);
        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        flow.Controls.AddRange(
        [
            CreateLabel("Lewy przycisk — ściana, prawy — gumka"),
            BrushLabel,
            BrushTrackBar,
        ]);
        return CreateGroup("Rysowanie", flow);
    }

    private static GroupBox BuildLegendGroup()
    {
        var legend = new ColorLegend { Width = ControlWidth };
        return CreateGroup("Legenda", legend);
    }

    private Button CreateButton(string text, string tip)
    {
        var button = new Button { Text = text, Width = 76, Height = 30 };
        _toolTip.SetToolTip(button, tip);
        return button;
    }

    private static TrackBar CreateTrackBar(int minimum, int maximum, int value) => new()
    {
        Minimum = minimum,
        Maximum = maximum,
        Value = value,
        TickStyle = TickStyle.None,
        AutoSize = false,
        Width = ControlWidth,
        Height = 28,
    };

    private static Label CreateLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        MaximumSize = new Size(ControlWidth, 0),
    };

    private static GroupBox CreateGroup(string title, Control content)
    {
        var group = new GroupBox
        {
            Text = title,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(6),
            MinimumSize = new Size(GroupWidth, 0),
            MaximumSize = new Size(GroupWidth, 0),
        };
        content.Dock = DockStyle.Top;
        group.Controls.Add(content);
        return group;
    }
}
