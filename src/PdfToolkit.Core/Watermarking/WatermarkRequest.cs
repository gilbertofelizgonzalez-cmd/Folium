namespace PdfToolkit.Core.Watermarking;
public record WatermarkRequest(
    string SourcePath,
    string OutputPath,
    string Text,
    double FontSize,       // default 48
    int    Opacity,        // 0-255, default 80
    double AngleDegrees,   // default -45
    string ColorHex,       // e.g. "#808080"
    bool   AllPages        // true = all, false = use PageIndices
);
