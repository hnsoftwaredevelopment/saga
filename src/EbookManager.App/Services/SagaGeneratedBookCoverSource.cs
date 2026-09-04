using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EbookManager.Application.Metadata;

namespace EbookManager.App.Services;

public sealed class SagaGeneratedBookCoverSource : IBookCoverSource
{
    public const string Key = "saga-generated";
    private const int Width = 1200;
    private const int Height = 1600;
    private const int MaximumStoredCovers = 32;
    private static readonly TimeSpan CandidateLifetime = TimeSpan.FromMinutes(15);
    private readonly object candidateGate = new();
    private readonly Dictionary<string, GeneratedCover> generatedCovers = new(StringComparer.Ordinal);

    public string SourceKey => Key;

    public Task<BookCoverSearchResult> SearchAsync(
        BookCoverSearchQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var title = string.IsNullOrWhiteSpace(query.Title) ? "Saga" : query.Title.Trim();
        var authors = query.Authors.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToArray();
        var bytes = GenerateJpeg(title, authors);
        var id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        lock (candidateGate)
        {
            RemoveExpiredAndExcessCandidates();
            generatedCovers[id] = new(bytes, DateTimeOffset.UtcNow);
        }

        BookCoverCandidate candidate = new(
            Key,
            id,
            "Saga",
            title,
            authors,
            bytes,
            Width,
            Height);
        return Task.FromResult(new BookCoverSearchResult(BookCoverSearchStatus.Succeeded, [candidate]));
    }

    public Task<BookCoverDownloadResult> DownloadAsync(
        string candidateId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GeneratedCover? cover;
        lock (candidateGate)
        {
            if (!generatedCovers.Remove(candidateId, out cover) ||
                DateTimeOffset.UtcNow - cover.CreatedUtc > CandidateLifetime)
            {
                return Task.FromResult(new BookCoverDownloadResult(BookCoverDownloadStatus.InvalidCandidate));
            }
        }

        if (cover is null)
        {
            return Task.FromResult(new BookCoverDownloadResult(BookCoverDownloadStatus.InvalidCandidate));
        }

        return Task.FromResult(new BookCoverDownloadResult(
            BookCoverDownloadStatus.Succeeded,
            cover.Bytes,
            Width,
            Height));
    }

    private void RemoveExpiredAndExcessCandidates()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in generatedCovers.Where(item => now - item.Value.CreatedUtc > CandidateLifetime))
        {
            generatedCovers.Remove(item.Key);
        }

        foreach (var item in generatedCovers
                     .OrderBy(item => item.Value.CreatedUtc)
                     .Take(Math.Max(0, generatedCovers.Count - MaximumStoredCovers + 1)))
        {
            generatedCovers.Remove(item.Key);
        }
    }

    private static byte[] GenerateJpeg(string title, IReadOnlyList<string> authors)
    {
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            var background = new LinearGradientBrush(
                Color.FromRgb(29, 49, 78),
                Color.FromRgb(72, 112, 142),
                new Point(0, 0),
                new Point(1, 1));
            drawing.DrawRectangle(background, null, new Rect(0, 0, Width, Height));
            drawing.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                null,
                new Rect(86, 86, Width - 172, Height - 172));

            DrawText(drawing, title, 92, FontWeights.SemiBold, 130, 205, Height - 520);
            var authorText = authors.Count == 0 ? string.Empty : string.Join(" · ", authors);
            if (!string.IsNullOrWhiteSpace(authorText))
            {
                DrawText(drawing, authorText, 46, FontWeights.Normal, 130, Height - 340, 150);
            }

            DrawText(drawing, "SAGA", 34, FontWeights.Bold, 130, Height - 155, 55);
        }

        var bitmap = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new JpegBitmapEncoder { QualityLevel = 92 };
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = new MemoryStream();
        encoder.Save(output);
        return output.ToArray();
    }

    private static void DrawText(
        DrawingContext drawing,
        string text,
        double size,
        FontWeight weight,
        double x,
        double y,
        double maximumHeight)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size,
            Brushes.White,
            1)
        {
            MaxTextWidth = Width - (x * 2),
            MaxTextHeight = maximumHeight,
            TextAlignment = TextAlignment.Center
        };
        drawing.DrawText(formatted, new Point(x, y));
    }

    private sealed record GeneratedCover(byte[] Bytes, DateTimeOffset CreatedUtc);
}
