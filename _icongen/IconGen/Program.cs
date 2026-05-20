// PDFStudio icon generator — produces a multi-resolution .ico file
// Sizes: 16, 24, 32, 48, 64, 128, 256
// Design: deep-blue gradient background with white PDF document, folded corner,
//         a small gear badge (bottom-right), and bold "PDF" text on the doc face.

using SkiaSharp;
using System.IO;

string outPath = args.Length > 0 ? args[0]
    : Path.Combine("..", "..", "src", "PdfToolkit.UI", "Resources", "app.ico");

int[] sizes = [16, 24, 32, 48, 64, 128, 256];
var pngBytesList = new List<byte[]>();

foreach (var s in sizes)
    pngBytesList.Add(RenderIcon(s));

WriteIco(outPath, sizes, pngBytesList);
Console.WriteLine($"Icon written to: {Path.GetFullPath(outPath)}");

// ── render one size ─────────────────────────────────────────────────────────
static byte[] RenderIcon(int s)
{
    var info = new SKImageInfo(s, s, SKColorType.Rgba8888, SKAlphaType.Premul);
    using var surface = SKSurface.Create(info);
    var c = surface.Canvas;
    c.Clear(SKColors.Transparent);

    float pad  = s * 0.07f;
    float full = s - pad * 2;

    // ── 1. background: rounded square with blue gradient ───────────────────
    float bgR   = s * 0.18f;
    var bgRect  = new SKRect(pad, pad, pad + full, pad + full);

    using var bgPaint = new SKPaint { IsAntialias = true };
    bgPaint.Shader = SKShader.CreateLinearGradient(
        new SKPoint(bgRect.Left,  bgRect.Top),
        new SKPoint(bgRect.Right, bgRect.Bottom),
        [new SKColor(0x0D, 0x47, 0xA1), new SKColor(0x19, 0x76, 0xD2)],
        [0f, 1f],
        SKShaderTileMode.Clamp);
    c.DrawRoundRect(bgRect, bgR, bgR, bgPaint);

    // subtle top-gloss
    using var glossPaint = new SKPaint { IsAntialias = true };
    glossPaint.Shader = SKShader.CreateLinearGradient(
        new SKPoint(bgRect.Left, bgRect.Top),
        new SKPoint(bgRect.Left, bgRect.Top + full * 0.45f),
        [new SKColor(255, 255, 255, 45), new SKColor(255, 255, 255, 0)],
        [0f, 1f],
        SKShaderTileMode.Clamp);
    c.DrawRoundRect(bgRect, bgR, bgR, glossPaint);

    // ── 2. white document body ──────────────────────────────────────────────
    float dW   = full * 0.62f;
    float dH   = full * 0.70f;
    float dX   = pad + (full - dW) * 0.42f;
    float dY   = pad + (full - dH) * 0.36f;
    float fold = dW * 0.26f;
    float dR   = Math.Max(0.8f, s * 0.03f);

    using var docPath = new SKPath();
    docPath.MoveTo(dX,        dY + dR);
    docPath.QuadTo(dX,        dY,        dX + dR,        dY);
    docPath.LineTo(dX + dW - fold, dY);
    docPath.LineTo(dX + dW,  dY + fold);
    docPath.LineTo(dX + dW,  dY + dH - dR);
    docPath.QuadTo(dX + dW,  dY + dH,   dX + dW - dR,   dY + dH);
    docPath.LineTo(dX + dR,  dY + dH);
    docPath.QuadTo(dX,        dY + dH,   dX,             dY + dH - dR);
    docPath.Close();

    using var docPaint = new SKPaint { IsAntialias = true, Color = SKColors.White };
    c.DrawPath(docPath, docPaint);

    // fold shadow
    using var foldPath = new SKPath();
    foldPath.MoveTo(dX + dW - fold, dY);
    foldPath.LineTo(dX + dW - fold, dY + fold);
    foldPath.LineTo(dX + dW,        dY + fold);
    foldPath.Close();
    using var foldPaint = new SKPaint
    {
        IsAntialias = true,
        Color = new SKColor(0x19, 0x76, 0xD2, 190)
    };
    c.DrawPath(foldPath, foldPaint);

    // crease lines
    if (s >= 24)
    {
        using var crPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0x0D, 0x47, 0xA1, 130),
            StrokeWidth = Math.Max(0.5f, s * 0.012f),
            IsStroke = true
        };
        c.DrawLine(dX + dW - fold, dY,       dX + dW - fold, dY + fold, crPaint);
        c.DrawLine(dX + dW - fold, dY + fold, dX + dW,        dY + fold, crPaint);
    }

    // ── 3. "PDF" text on document ──────────────────────────────────────────
    if (s >= 24)
    {
        float ts = dH * (s >= 64 ? 0.22f : 0.20f);
        using var tf = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold,
            SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            ?? SKTypeface.Default;
        using var tp = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0x0D, 0x47, 0xA1),
            TextSize = ts,
            Typeface = tf,
            FakeBoldText = true
        };
        c.DrawText("PDF", dX + dW * 0.12f, dY + dH * 0.50f, tp);

        // decorative doc lines
        if (s >= 32)
        {
            using var lp = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(0x19, 0x76, 0xD2, 110),
                StrokeWidth = Math.Max(1f, s * 0.018f),
                StrokeCap = SKStrokeCap.Round
            };
            float lx1 = dX + dW * 0.12f;
            float lx2 = dX + dW * 0.80f;
            float ly  = dY + dH * 0.63f;
            c.DrawLine(lx1, ly, lx2, ly, lp);
            if (s >= 48)
                c.DrawLine(lx1, ly + dH * 0.115f, lx1 + (lx2 - lx1) * 0.70f, ly + dH * 0.115f, lp);
        }
    }

    // ── 4. gear badge (bottom-right of bg square) ─────────────────────────
    if (s >= 32)
    {
        float gCx = bgRect.Right  - full * 0.20f;
        float gCy = bgRect.Bottom - full * 0.20f;
        float gR  = full * 0.165f;

        // white backing circle
        using var badgePaint = new SKPaint { IsAntialias = true, Color = SKColors.White };
        c.DrawCircle(gCx, gCy, gR * 1.30f, badgePaint);

        DrawGear(c, gCx, gCy, gR);
    }

    using var snap = surface.Snapshot();
    using var data = snap.Encode(SKEncodedImageFormat.Png, 100);
    return data.ToArray();
}

// ── gear shape ───────────────────────────────────────────────────────────────
static void DrawGear(SKCanvas c, float cx, float cy, float r)
{
    const int teeth = 8;
    float inner  = r * 0.60f;
    float outer  = r;
    float holeR  = r * 0.28f;
    double tw    = Math.PI / teeth * 0.55; // tooth half-width (rad)

    using var path = new SKPath();
    for (int i = 0; i < teeth; i++)
    {
        double angle = 2 * Math.PI * i / teeth - Math.PI / 2;
        double a0 = angle - tw;
        double a1 = angle + tw;

        float ix0 = cx + inner * (float)Math.Cos(a0);
        float iy0 = cy + inner * (float)Math.Sin(a0);
        float ox0 = cx + outer * (float)Math.Cos(a0);
        float oy0 = cy + outer * (float)Math.Sin(a0);
        float ox1 = cx + outer * (float)Math.Cos(a1);
        float oy1 = cy + outer * (float)Math.Sin(a1);
        float ix1 = cx + inner * (float)Math.Cos(a1);
        float iy1 = cy + inner * (float)Math.Sin(a1);

        if (i == 0) path.MoveTo(ix0, iy0);
        else        path.LineTo(ix0, iy0);

        path.LineTo(ox0, oy0);
        path.LineTo(ox1, oy1);
        path.LineTo(ix1, iy1);

        double nextA0 = 2 * Math.PI * (i + 1) / teeth - Math.PI / 2 - tw;
        var arcRect = new SKRect(cx - inner, cy - inner, cx + inner, cy + inner);
        float startDeg = (float)(a1 * 180 / Math.PI);
        float sweepDeg = (float)((nextA0 - a1) * 180 / Math.PI);
        using var arc = new SKPath();
        arc.AddArc(arcRect, startDeg, sweepDeg);
        path.AddPath(arc);
    }
    path.Close();
    path.AddCircle(cx, cy, holeR, SKPathDirection.CounterClockwise);

    using var paint = new SKPaint
    {
        IsAntialias = true,
        Color = new SKColor(0x0D, 0x47, 0xA1)
    };
    c.DrawPath(path, paint);
}

// ── write .ico ───────────────────────────────────────────────────────────────
static void WriteIco(string path, int[] sizes, List<byte[]> pngs)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    int count      = sizes.Length;
    int dataOffset = 6 + count * 16;

    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);

    // ICONDIR header
    bw.Write((ushort)0); // reserved
    bw.Write((ushort)1); // type ICO
    bw.Write((ushort)count);

    // entries
    int offset = dataOffset;
    for (int i = 0; i < count; i++)
    {
        int sz = sizes[i];
        bw.Write((byte)(sz == 256 ? 0 : sz));
        bw.Write((byte)(sz == 256 ? 0 : sz));
        bw.Write((byte)0);    // color count
        bw.Write((byte)0);    // reserved
        bw.Write((ushort)1);  // planes
        bw.Write((ushort)32); // bpp
        bw.Write((uint)pngs[i].Length);
        bw.Write((uint)offset);
        offset += pngs[i].Length;
    }

    foreach (var png in pngs)
        bw.Write(png);

    File.WriteAllBytes(path, ms.ToArray());
}
