namespace PdfToolkit.Core.Common;

public class PdfToolkitException : Exception
{
    public PdfToolkitException(string message) : base(message) { }
    public PdfToolkitException(string message, Exception inner) : base(message, inner) { }
}
