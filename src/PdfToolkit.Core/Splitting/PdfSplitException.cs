using PdfToolkit.Core.Common;

namespace PdfToolkit.Core.Splitting;

public class PdfSplitException : PdfToolkitException
{
    public PdfSplitException(string message) : base(message) { }
    public PdfSplitException(string message, Exception inner) : base(message, inner) { }
}
