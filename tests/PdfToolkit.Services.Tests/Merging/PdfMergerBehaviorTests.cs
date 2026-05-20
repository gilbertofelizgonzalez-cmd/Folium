using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfToolkit.Core.Merging;
using PdfToolkit.Services.Merging;
using Shouldly;

namespace PdfToolkit.Services.Tests.Merging;

public class PdfMergerBehaviorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IPdfMerger _merger = new PdfMerger();

    public PdfMergerBehaviorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MergeAsync_WithTwoValidPdfs_ProducesCorrectOutput()
    {
        var file1 = CreatePdf("a.pdf", pages: 3);
        var file2 = CreatePdf("b.pdf", pages: 2);
        var output = Path.Combine(_tempDir, "merged.pdf");

        var request = new MergeRequest(new[] { file1, file2 }, output);
        var result = await _merger.MergeAsync(request);

        result.TotalPages.ShouldBe(5);
        result.OutputPath.ShouldBe(output);
        File.Exists(output).ShouldBeTrue();

        using var resultDoc = PdfReader.Open(output, PdfDocumentOpenMode.ReadOnly);
        resultDoc.PageCount.ShouldBe(5);
    }

    [Fact]
    public async Task MergeAsync_WithCancellation_ThrowsOperationCanceled()
    {
        var file1 = CreatePdf("a.pdf", pages: 2);
        var file2 = CreatePdf("b.pdf", pages: 2);
        var output = Path.Combine(_tempDir, "merged.pdf");
        var request = new MergeRequest(new[] { file1, file2 }, output);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => _merger.MergeAsync(request, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task MergeAsync_ReportsProgress()
    {
        var file1 = CreatePdf("a.pdf", pages: 1);
        var file2 = CreatePdf("b.pdf", pages: 1);
        var file3 = CreatePdf("c.pdf", pages: 1);
        var output = Path.Combine(_tempDir, "merged.pdf");
        var request = new MergeRequest(new[] { file1, file2, file3 }, output);

        var captured = new CapturingProgress<MergeProgress>();
        await _merger.MergeAsync(request, captured);

        captured.Reports.ShouldNotBeEmpty();
        captured.Reports.Last().FilesProcessed.ShouldBe(3);
        captured.Reports.Last().Percentage.ShouldBe(100);
    }

    private string CreatePdf(string name, int pages)
    {
        var path = Path.Combine(_tempDir, name);
        using var doc = new PdfDocument();
        for (int i = 0; i < pages; i++)
            doc.AddPage();
        doc.Save(path);
        return path;
    }

    private class CapturingProgress<T> : IProgress<T>
    {
        public List<T> Reports { get; } = new();
        public void Report(T value) => Reports.Add(value);
    }
}
