namespace PdfToolkit.Core.Rotating;
public class PdfRotateException : Exception
{
    public PdfRotateException(string message) : base(message) { }
    public PdfRotateException(string message, Exception inner) : base(message, inner) { }
}
