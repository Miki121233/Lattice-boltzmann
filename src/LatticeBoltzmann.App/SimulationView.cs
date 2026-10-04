using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using LatticeBoltzmann.Core;
using LatticeBoltzmann.Core.Visualization;

namespace LatticeBoltzmann.App;

/// <summary>Draws the concentration field of a simulation and lets the user paint walls on it with the mouse.</summary>
internal sealed class SimulationView : Control
{
    private DiffusionSimulation? _simulation;
    private Bitmap? _bitmap;
    private int[] _pixels = [];
    private int _brushSize = 2;
    private (int X, int Y)? _lastCell;
    private MouseButtons _strokeButton = MouseButtons.None;

    public SimulationView()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = SystemColors.Control;
    }

    /// <summary>The simulation shown and edited by this view; replacing it ends any stroke in progress.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiffusionSimulation? Simulation
    {
        get => _simulation;
        set
        {
            _simulation = value;
            _lastCell = null;
            _strokeButton = MouseButtons.None;
            _bitmap?.Dispose();
            _bitmap = null;
            _pixels = [];
            if (value is not null)
            {
                _bitmap = new Bitmap(value.Width, value.Height, PixelFormat.Format32bppArgb);
                _pixels = new int[value.Width * value.Height];
            }

            RefreshField();
        }
    }

    /// <summary>Brush edge length in cells, 1 to 8.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int BrushSize
    {
        get => _brushSize;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, BrushStroke.MinSize);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, BrushStroke.MaxSize);
            _brushSize = value;
        }
    }

    /// <summary>The bitmap with one pixel per grid cell, or <see langword="null"/> without a simulation.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Bitmap? FieldBitmap => _bitmap;

    /// <summary>Renders the current simulation state into <see cref="FieldBitmap"/> and requests a repaint.</summary>
    public void RefreshField()
    {
        if (_simulation is null || _bitmap is null)
        {
            Invalidate();
            return;
        }

        FieldRenderer.Render(_simulation, _pixels);
        var width = _bitmap.Width;
        var height = _bitmap.Height;
        var data = _bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (var row = 0; row < height; row++)
            {
                Marshal.Copy(_pixels, row * width, data.Scan0 + (row * data.Stride), width);
            }
        }
        finally
        {
            _bitmap.UnlockBits(data);
        }

        Invalidate();
    }

    /// <summary>Paints the background and the field image centred with a uniform scale.</summary>
    public void PaintField(Graphics graphics)
    {
        ArgumentNullException.ThrowIfNull(graphics);

        graphics.Clear(BackColor);
        if (_bitmap is null || _simulation is null)
        {
            return;
        }

        var mapping = new ViewMapping(ClientSize.Width, ClientSize.Height, _simulation.Width, _simulation.Height);
        if (mapping.Scale <= 0)
        {
            return;
        }

        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.DrawImage(
            _bitmap,
            new RectangleF((float)mapping.OffsetX, (float)mapping.OffsetY, (float)mapping.DrawWidth, (float)mapping.DrawHeight));
    }

    /// <summary>Starts a stroke: the left button draws walls, the right one erases them; other buttons are ignored.</summary>
    public void BeginStroke(PointF location, MouseButtons button)
    {
        _lastCell = null;
        _strokeButton = MouseButtons.None;
        if (button is not (MouseButtons.Left or MouseButtons.Right))
        {
            return;
        }

        _strokeButton = button;
        ApplyBrush(location);
    }

    /// <summary>Extends the stroke to a new pointer position; leaving the image breaks the stroke.</summary>
    public void ContinueStroke(PointF location)
    {
        if (_strokeButton == MouseButtons.None)
        {
            return;
        }

        ApplyBrush(location);
    }

    /// <summary>Ends the stroke.</summary>
    public void EndStroke()
    {
        _lastCell = null;
        _strokeButton = MouseButtons.None;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        BeginStroke(e.Location, e.Button);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        ContinueStroke(e.Location);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        EndStroke();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        PaintField(e.Graphics);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _bitmap?.Dispose();
            _bitmap = null;
        }

        base.Dispose(disposing);
    }

    private void ApplyBrush(PointF location)
    {
        var simulation = _simulation;
        if (simulation is null)
        {
            return;
        }

        var mapping = new ViewMapping(ClientSize.Width, ClientSize.Height, simulation.Width, simulation.Height);
        if (!mapping.TryGetCell(location.X, location.Y, out var x, out var y))
        {
            _lastCell = null;
            return;
        }

        var from = _lastCell ?? (x, y);
        var wall = _strokeButton == MouseButtons.Left;
        foreach (var (cx, cy) in BrushStroke.Cells(from.Item1, from.Item2, x, y, _brushSize, simulation.Width, simulation.Height))
        {
            simulation.SetWall(cx, cy, wall);
        }

        _lastCell = (x, y);
        RefreshField();
    }
}
