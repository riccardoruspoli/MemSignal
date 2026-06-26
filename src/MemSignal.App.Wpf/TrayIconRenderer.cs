using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace MemSignal.App.Wpf;

public sealed class TrayIconRenderer
{
    public Icon Render(TrayPresentation presentation, int size = 32)
    {
        using var bitmap = RenderBitmap(presentation, size);
        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    internal Bitmap RenderBitmap(TrayPresentation presentation, int size)
    {
        var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var color = presentation.VisualState switch
        {
            TrayVisualState.Normal => Color.FromArgb(34, 165, 59),
            TrayVisualState.Warning => Color.FromArgb(210, 145, 0),
            TrayVisualState.Critical => Color.FromArgb(229, 57, 53),
            _ => Color.FromArgb(100, 116, 139)
        };

        // Fill almost the entire notification-area canvas. A solid semantic
        // background gives the percentage substantially more usable contrast
        // than the previous ring-with-light-centre treatment at 16 px.
        var inset = Math.Max(0.5f, size * 0.02f);
        using var background = new SolidBrush(color);
        graphics.FillEllipse(background, inset, inset, size - inset * 2, size - inset * 2);

        // Shape cues supplement color: warning has a top notch; critical has two.
        using var cue = new SolidBrush(Color.White);
        if (presentation.VisualState is TrayVisualState.Warning or TrayVisualState.Critical)
        {
            graphics.FillEllipse(cue, size * 0.44f, 0, size * 0.12f, size * 0.12f);
        }
        if (presentation.VisualState == TrayVisualState.Critical)
        {
            graphics.FillEllipse(cue, size * 0.44f, size * 0.88f, size * 0.12f, size * 0.12f);
        }

        var fontSize = presentation.DisplayToken.Length switch
        {
            >= 3 => size * 0.34f,
            2 => size * 0.54f,
            _ => size * 0.60f
        };
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(Color.White);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString(presentation.DisplayToken, font, textBrush, new RectangleF(0, 0, size, size), format);

        return bitmap;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);
}
