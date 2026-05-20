namespace PdfToolkit.Core.Exporting;
public class PdfExportException : Exception
{
    public PdfExportException(string message) : base(message) { }
    public PdfExportException(string message, Exception inner) : base(message, inner) { }
}
