using System.Drawing.Drawing2D;
using LatticeBoltzmann.Core.Visualization;

namespace LatticeBoltzmann.App;

/// <summary>Legend: concentration gradient with "0"/"1" labels and a wall swatch.</summary>
internal sealed class ColorLegend : Control
{
    private Bitmap? _gradient;

    public ColorLegend()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        Height = LogicalToDeviceUnits(64);
    }

    protected override Size DefaultSize => new(200, 64);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var pad = LogicalToDeviceUnits(4);
        var barHeight = LogicalToDeviceUnits(16);
        var swatch = LogicalToDeviceUnits(16);
        var swatchGap = LogicalToDeviceUnits(8);
        using var textBrush = new SolidBrush(SystemColors.ControlText);
        using var borderPen = new Pen(SystemColors.ControlDark);

        g.DrawString("Stężenie", Font, textBrush, pad, pad);
        var titleHeight = Font.Height;
        var barTop = pad + titleHeight + pad;

        var wallLabelWidth = TextRenderer.MeasureText("Ściana", Font).Width;
        var barWidth = ClientSize.Width - (2 * pad) - swatch - swatchGap - wallLabelWidth - swatchGap;
        if (barWidth < 2)
        {
            return;
        }

        EnsureGradient(barWidth, barHeight);
        if (_gradient is not null)
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.DrawImageUnscaled(_gradient, pad, barTop);
        }

        g.DrawRectangle(borderPen, pad, barTop, barWidth - 1, barHeight - 1);
        var labelTop = barTop + barHeight + (pad / 2);
        g.DrawString("0", Font, textBrush, pad, labelTop);
        var oneWidth = TextRenderer.MeasureText("1", Font).Width;
        g.DrawString("1", Font, textBrush, pad + barWidth - oneWidth, labelTop);

        var swatchLeft = pad + barWidth + swatchGap;
        using (var wallBrush = new SolidBrush(Color.FromArgb(Colormap.WallArgb)))
        {
            g.FillRectangle(wallBrush, swatchLeft, barTop, swatch, swatch);
        }

        g.DrawRectangle(borderPen, swatchLeft, barTop, swatch - 1, swatch - 1);
        g.DrawString("Ściana", Font, textBrush, swatchLeft + swatch + (swatchGap / 2), barTop);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gradient?.Dispose();
            _gradient = null;
        }

        base.Dispose(disposing);
    }

    private void EnsureGradient(int width, int height)
    {
        if (_gradient is not null && _gradient.Width == width && _gradient.Height == height)
        {
            return;
        }

        _gradient?.Dispose();
        _gradient = new Bitmap(width, height);
        for (var x = 0; x < width; x++)
        {
            var color = Color.FromArgb(Colormap.ToArgb(x / (double)(width - 1)));
            for (var y = 0; y < height; y++)
            {
                _gradient.SetPixel(x, y, color);
            }
        }
    }
}
