namespace PdfToolkit.Core.Compressing;
public class PdfCompressException : Exception
{
    public PdfCompressException(string message) : base(message) { }
    public PdfCompressException(string message, Exception inner) : base(message, inner) { }
}
