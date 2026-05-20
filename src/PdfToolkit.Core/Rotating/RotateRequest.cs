namespace PdfToolkit.Core.Rotating;
// PageRotations: key=page index (0-based), value=degrees to add (90, 180, 270)
public record RotateRequest(string SourcePath, string OutputPath, IReadOnlyDictionary<int, int> PageRotations);
