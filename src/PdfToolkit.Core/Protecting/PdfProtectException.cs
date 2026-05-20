namespace PdfToolkit.Core.Protecting;
public class PdfProtectException : Exception
{
    public PdfProtectException(string message) : base(message) { }
    public PdfProtectException(string message, Exception inner) : base(message, inner) { }
}
