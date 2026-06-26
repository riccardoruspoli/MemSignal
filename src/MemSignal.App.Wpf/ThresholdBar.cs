using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace MemSignal.App.Wpf;

public sealed class ThresholdBar : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(ThresholdBar),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ModerateThresholdProperty =
        DependencyProperty.Register(
            nameof(ModerateThreshold),
            typeof(double),
            typeof(ThresholdBar),
            new FrameworkPropertyMetadata(0.8d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double ModerateThreshold
    {
        get => (double)GetValue(ModerateThresholdProperty);
        set => SetValue(ModerateThresholdProperty, value);
    }

    public static readonly DependencyProperty ElevatedThresholdProperty =
        DependencyProperty.Register(
            nameof(ElevatedThreshold),
            typeof(double),
            typeof(ThresholdBar),
            new FrameworkPropertyMetadata(0.9d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double ElevatedThreshold
    {
        get => (double)GetValue(ElevatedThresholdProperty);
        set => SetValue(ElevatedThresholdProperty, value);
    }

    public static readonly DependencyProperty GreenBrushProperty =
        DependencyProperty.Register(nameof(GreenBrush), typeof(Brush), typeof(ThresholdBar), CreateBrushMetadata());

    public static readonly DependencyProperty YellowBrushProperty =
        DependencyProperty.Register(nameof(YellowBrush), typeof(Brush), typeof(ThresholdBar), CreateBrushMetadata());

    public static readonly DependencyProperty RedBrushProperty =
        DependencyProperty.Register(nameof(RedBrush), typeof(Brush), typeof(ThresholdBar), CreateBrushMetadata());

    public static readonly DependencyProperty SeparatorBrushProperty =
        DependencyProperty.Register(nameof(SeparatorBrush), typeof(Brush), typeof(ThresholdBar), CreateBrushMetadata());

    public static readonly DependencyProperty MarkerFillBrushProperty =
        DependencyProperty.Register(nameof(MarkerFillBrush), typeof(Brush), typeof(ThresholdBar), CreateBrushMetadata());

    public static readonly DependencyProperty MarkerStrokeBrushProperty =
        DependencyProperty.Register(nameof(MarkerStrokeBrush), typeof(Brush), typeof(ThresholdBar), CreateBrushMetadata());

    public static readonly DependencyProperty LabelBrushProperty =
        DependencyProperty.Register(nameof(LabelBrush), typeof(Brush), typeof(ThresholdBar), CreateBrushMetadata());

    public Brush GreenBrush
    {
        get => (Brush)GetValue(GreenBrushProperty);
        set => SetValue(GreenBrushProperty, value);
    }

    public Brush YellowBrush
    {
        get => (Brush)GetValue(YellowBrushProperty);
        set => SetValue(YellowBrushProperty, value);
    }

    public Brush RedBrush
    {
        get => (Brush)GetValue(RedBrushProperty);
        set => SetValue(RedBrushProperty, value);
    }

    public Brush SeparatorBrush
    {
        get => (Brush)GetValue(SeparatorBrushProperty);
        set => SetValue(SeparatorBrushProperty, value);
    }

    public Brush MarkerFillBrush
    {
        get => (Brush)GetValue(MarkerFillBrushProperty);
        set => SetValue(MarkerFillBrushProperty, value);
    }

    public Brush MarkerStrokeBrush
    {
        get => (Brush)GetValue(MarkerStrokeBrushProperty);
        set => SetValue(MarkerStrokeBrushProperty, value);
    }

    public Brush LabelBrush
    {
        get => (Brush)GetValue(LabelBrushProperty);
        set => SetValue(LabelBrushProperty, value);
    }

    private static FrameworkPropertyMetadata CreateBrushMetadata() =>
        new(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender);

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(availableSize.Width, 58);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        const double trackTop = 16;
        const double trackHeight = 12;
        const double labelTop = 38;
        const double markerHeight = 10;

        var width = Math.Max(0, ActualWidth);
        if (width <= 0)
        {
            return;
        }

        var radius = trackHeight / 2;
        var (moderateThreshold, elevatedThreshold) = NormalizeThresholds(ModerateThreshold, ElevatedThreshold);
        var greenEnd = width * moderateThreshold;
        var yellowEnd = width * elevatedThreshold;
        var markerX = width * Math.Clamp(Value, 0, 100) / 100;

        DrawSegment(drawingContext, new Rect(0, trackTop, greenEnd, trackHeight), GreenBrush, radius, topLeft: true, bottomLeft: true);
        DrawSegment(drawingContext, new Rect(greenEnd, trackTop, yellowEnd - greenEnd, trackHeight), YellowBrush, radius);
        DrawSegment(drawingContext, new Rect(yellowEnd, trackTop, width - yellowEnd, trackHeight), RedBrush, radius, topRight: true, bottomRight: true);

        const double separatorThickness = 3;
        var separatorPen = new Pen(SeparatorBrush, separatorThickness);
        drawingContext.DrawLine(separatorPen, new Point(greenEnd, trackTop - 1), new Point(greenEnd, trackTop + trackHeight + 1));
        drawingContext.DrawLine(separatorPen, new Point(yellowEnd, trackTop - 1), new Point(yellowEnd, trackTop + trackHeight + 1));

        var markerPen = new Pen(MarkerStrokeBrush, 1);
        var marker = new StreamGeometry();
        using (var context = marker.Open())
        {
            context.BeginFigure(new Point(markerX, trackTop - 7), true, true);
            context.LineTo(new Point(markerX - 6, trackTop - 7 - markerHeight), true, false);
            context.LineTo(new Point(markerX + 6, trackTop - 7 - markerHeight), true, false);
        }

        marker.Freeze();
        drawingContext.DrawGeometry(MarkerFillBrush, markerPen, marker);

        var separatorLeftEdgeOffset = separatorThickness / 2;
        DrawLabel(drawingContext, "0%", 0, labelTop, LabelAnchor.Left);
        DrawLabel(drawingContext, FormatThreshold(moderateThreshold), greenEnd - separatorLeftEdgeOffset, labelTop, LabelAnchor.Right);
        DrawLabel(drawingContext, FormatThreshold(elevatedThreshold), yellowEnd - separatorLeftEdgeOffset, labelTop, LabelAnchor.Right);
        DrawLabel(drawingContext, "100%", width, labelTop, LabelAnchor.Right);
    }

    public static (double Moderate, double Elevated) NormalizeThresholds(double moderate, double elevated)
    {
        moderate = NormalizeThreshold(moderate);
        elevated = NormalizeThreshold(elevated);
        return elevated < moderate ? (moderate, moderate) : (moderate, elevated);
    }

    private static double NormalizeThreshold(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;

    private static string FormatThreshold(double value) =>
        (value * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";

    private static void DrawSegment(
        DrawingContext drawingContext,
        Rect rect,
        Brush brush,
        double radius,
        bool topLeft = false,
        bool topRight = false,
        bool bottomRight = false,
        bool bottomLeft = false)
    {
        if (topLeft && topRight && bottomRight && bottomLeft)
        {
            drawingContext.DrawRoundedRectangle(brush, null, rect, radius, radius);
            return;
        }

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            var left = rect.Left;
            var top = rect.Top;
            var right = rect.Right;
            var bottom = rect.Bottom;

            context.BeginFigure(new Point(left + (topLeft ? radius : 0), top), true, true);
            context.LineTo(new Point(right - (topRight ? radius : 0), top), true, false);
            if (topRight)
            {
                context.ArcTo(new Point(right, top + radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
            }

            context.LineTo(new Point(right, bottom - (bottomRight ? radius : 0)), true, false);
            if (bottomRight)
            {
                context.ArcTo(new Point(right - radius, bottom), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
            }

            context.LineTo(new Point(left + (bottomLeft ? radius : 0), bottom), true, false);
            if (bottomLeft)
            {
                context.ArcTo(new Point(left, bottom - radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
            }

            context.LineTo(new Point(left, top + (topLeft ? radius : 0)), true, false);
            if (topLeft)
            {
                context.ArcTo(new Point(left + radius, top), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
            }
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(brush, null, geometry);
    }

    private enum LabelAnchor
    {
        Left,
        Right
    }

    private void DrawLabel(DrawingContext drawingContext, string text, double x, double y, LabelAnchor anchor)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            11,
            LabelBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip)
        {
            TextAlignment = TextAlignment.Left
        };

        var textWidth = formattedText.WidthIncludingTrailingWhitespace;
        var drawX = anchor switch
        {
            LabelAnchor.Right => x - textWidth,
            _ => x
        };

        drawingContext.DrawText(formattedText, new Point(drawX, y));
    }
}
