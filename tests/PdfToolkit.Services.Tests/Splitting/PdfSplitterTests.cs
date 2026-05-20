using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfToolkit.Core.Splitting;
using PdfToolkit.Services.Splitting;
using Shouldly;

namespace PdfToolkit.Services.Tests.Splitting;

public class PdfSplitterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ISplitter _splitter = new PdfSplitter();

    public PdfSplitterTests()
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
    public async Task SplitAsync_WithEmptySource_Throws()
    {
        var request = new SplitRequest("", _tempDir);

        await Should.ThrowAsync<ArgumentException>(
            () => _splitter.SplitAsync(request));
    }

    [Fact]
    public async Task SplitAsync_WithEmptyOutputDir_Throws()
    {
        var request = new SplitRequest("source.pdf", "");

        await Should.ThrowAsync<ArgumentException>(
            () => _splitter.SplitAsync(request));
    }

    [Fact]
    public async Task SplitAsync_WithNonExistentSource_Throws()
    {
        var request = new SplitRequest("nope.pdf", _tempDir);

        await Should.ThrowAsync<FileNotFoundException>(
            () => _splitter.SplitAsync(request));
    }

    [Fact]
    public async Task SplitAsync_WithNonExistentOutputDir_Throws()
    {
        var source = CreatePdf("source.pdf", pages: 2);
        var request = new SplitRequest(source, Path.Combine(_tempDir, "no-existe"));

        await Should.ThrowAsync<DirectoryNotFoundException>(
            () => _splitter.SplitAsync(request));
    }

    [Fact]
    public async Task SplitAsync_With3PagePdf_Produces3Files()
    {
        var source = CreatePdf("source.pdf", pages: 3);
        var request = new SplitRequest(source, _tempDir);

        var result = await _splitter.SplitAsync(request);

        result.TotalPages.ShouldBe(3);
        result.OutputFiles.Count.ShouldBe(3);

        foreach (var output in result.OutputFiles)
        {
            File.Exists(output).ShouldBeTrue();
            using var doc = PdfReader.Open(output, PdfDocumentOpenMode.ReadOnly);
            doc.PageCount.ShouldBe(1);
        }
    }

    [Fact]
    public async Task SplitAsync_WithCancellation_ThrowsOperationCanceled()
    {
        var source = CreatePdf("source.pdf", pages: 2);
        var request = new SplitRequest(source, _tempDir);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => _splitter.SplitAsync(request, cancellationToken: cts.Token));
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
}
