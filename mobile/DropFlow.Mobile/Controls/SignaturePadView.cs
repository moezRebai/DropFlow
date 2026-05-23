using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace DropFlow.Mobile.Controls;

public class SignaturePadView : ContentView
{
    public event Action<string?>? SignatureChanged;

    private readonly SKCanvasView _canvas;
    private readonly List<List<SKPoint>> _strokes = [];
    private List<SKPoint>? _currentStroke;

    private static readonly SKPaint StrokePaint = new()
    {
        Color = SKColors.Black,
        StrokeWidth = 4f,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round
    };

    private static readonly SKFont PlaceholderFont = new(SKTypeface.Default, 20f);

    private static readonly SKPaint PlaceholderPaint = new()
    {
        Color = SKColors.LightGray,
        IsAntialias = true,
    };

    public SignaturePadView()
    {
        _canvas = new SKCanvasView();
        _canvas.PaintSurface += OnPaintSurface;
        _canvas.EnableTouchEvents = true;
        _canvas.Touch += OnTouch;

        Content = new Border
        {
            Stroke = Color.FromArgb("#BDBDBD"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            HeightRequest = 180,
            Content = _canvas
        };
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _currentStroke = [e.Location];
                break;
            case SKTouchAction.Moved:
                _currentStroke?.Add(e.Location);
                break;
            case SKTouchAction.Released:
                if (_currentStroke?.Count > 0)
                {
                    _strokes.Add(_currentStroke);
                    _currentStroke = null;
                    SignatureChanged?.Invoke(ExportBase64());
                }
                break;
        }
        e.Handled = true;
        _canvas.InvalidateSurface();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        if (_strokes.Count == 0 && _currentStroke is null)
        {
            canvas.DrawText("Signez ici", e.Info.Width / 2f, e.Info.Height / 2f, SKTextAlign.Center, PlaceholderFont, PlaceholderPaint);
            return;
        }

        foreach (var stroke in _strokes)
            DrawStroke(canvas, stroke);

        if (_currentStroke is not null)
            DrawStroke(canvas, _currentStroke);
    }

    private static void DrawStroke(SKCanvas canvas, List<SKPoint> points)
    {
        if (points.Count == 0) return;

        var path = new SKPath();
        path.MoveTo(points[0]);

        for (var i = 1; i < points.Count - 1; i++)
        {
            var mid = new SKPoint(
                (points[i].X + points[i + 1].X) / 2f,
                (points[i].Y + points[i + 1].Y) / 2f);
            path.QuadTo(points[i], mid);
        }

        if (points.Count > 1)
            path.LineTo(points[^1]);

        canvas.DrawPath(path, StrokePaint);
    }

    private string? ExportBase64()
    {
        if (_strokes.Count == 0) return null;

        const int w = 600, h = 300;
        using var surface = SKSurface.Create(new SKImageInfo(w, h));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var bounds = GetBounds();
        var scaleX = (w - 20f) / Math.Max(bounds.Width, 1);
        var scaleY = (h - 20f) / Math.Max(bounds.Height, 1);
        var scale = Math.Min(scaleX, scaleY);

        canvas.Translate(10f - bounds.Left * scale, 10f - bounds.Top * scale);
        canvas.Scale(scale);

        foreach (var stroke in _strokes)
            DrawStroke(canvas, stroke);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return Convert.ToBase64String(data.ToArray());
    }

    private SKRect GetBounds()
    {
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var stroke in _strokes)
        foreach (var p in stroke)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        return new SKRect(minX, minY, maxX, maxY);
    }

    public void Clear()
    {
        _strokes.Clear();
        _currentStroke = null;
        _canvas.InvalidateSurface();
        SignatureChanged?.Invoke(null);
    }
}
