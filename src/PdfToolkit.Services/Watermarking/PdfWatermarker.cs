using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfToolkit.Core.Watermarking;

namespace PdfToolkit.Services.Watermarking;

public class PdfWatermarker : IWatermarker
{
    public Task<WatermarkResult> AddWatermarkAsync(WatermarkRequest request, CancellationToken ct = default)
        => Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            using var doc = PdfReader.Open(request.SourcePath, PdfDocumentOpenMode.Modify);

            var hex = request.ColorHex.TrimStart('#');
            byte r = Convert.ToByte(hex[0..2], 16);
            byte g = Convert.ToByte(hex[2..4], 16);
            byte b = Convert.ToByte(hex[4..6], 16);
            var color = XColor.FromArgb(request.Opacity, r, g, b);
            var font = new XFont("Arial", request.FontSize, XFontStyle.Bold);
            var brush = new XSolidBrush(color);

            foreach (var page in doc.Pages)
            {
                ct.ThrowIfCancellationRequested();
                using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                var size = gfx.MeasureString(request.Text, font);
                double cx = page.Width.Point / 2;
                double cy = page.Height.Point / 2;
                gfx.TranslateTransform(cx, cy);
                gfx.RotateTransform(request.AngleDegrees);
                gfx.DrawString(request.Text, font, brush,
                    new XPoint(-size.Width / 2, size.Height / 4));
            }

            doc.Save(request.OutputPath);
            return new WatermarkResult(request.OutputPath, doc.PageCount);
        }, ct);
}
