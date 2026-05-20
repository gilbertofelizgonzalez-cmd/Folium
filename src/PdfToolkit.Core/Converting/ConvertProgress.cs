namespace PdfToolkit.Core.Converting;

public record ConvertProgress(int ProcessedFiles, int TotalFiles, string CurrentFileName);
