using PdfToolkit.Core.Common;

namespace PdfToolkit.Core.Merging;

public class PdfMergeException : PdfToolkitException
{
    public PdfMergeException(string message) : base(message) { }
    public PdfMergeException(string message, Exception inner) : base(message, inner) { }
}
