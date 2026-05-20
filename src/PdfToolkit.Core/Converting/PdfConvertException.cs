using PdfToolkit.Core.Common;

namespace PdfToolkit.Core.Converting;

public class PdfConvertException : PdfToolkitException
{
    public PdfConvertException(string message, Exception? inner = null)
        : base(message, inner) { }
}
