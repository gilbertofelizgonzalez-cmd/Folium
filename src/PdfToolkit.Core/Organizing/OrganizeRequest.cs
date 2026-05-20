namespace PdfToolkit.Core.Organizing;
public record OrganizeRequest(string SourcePath, string OutputPath, IReadOnlyList<int> PageOrder);
// PageOrder: 0-based page indices in the desired output order; omitting an index deletes that page
