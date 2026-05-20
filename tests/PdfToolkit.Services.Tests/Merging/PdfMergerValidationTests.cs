using PdfToolkit.Core.Merging;
using PdfToolkit.Services.Merging;
using Shouldly;

namespace PdfToolkit.Services.Tests.Merging;

public class PdfMergerValidationTests
{
    private readonly IPdfMerger _merger = new PdfMerger();

    [Fact]
    public async Task MergeAsync_WithLessThanTwoFiles_Throws()
    {
        var request = new MergeRequest(new[] { "only.pdf" }, "out.pdf");

        await Should.ThrowAsync<ArgumentException>(
            () => _merger.MergeAsync(request));
    }

    [Fact]
    public async Task MergeAsync_WithEmptyOutputPath_Throws()
    {
        var request = new MergeRequest(new[] { "a.pdf", "b.pdf" }, "");

        await Should.ThrowAsync<ArgumentException>(
            () => _merger.MergeAsync(request));
    }

    [Fact]
    public async Task MergeAsync_WithEmptyPathInList_Throws()
    {
        var request = new MergeRequest(new[] { "a.pdf", "" }, "out.pdf");

        await Should.ThrowAsync<ArgumentException>(
            () => _merger.MergeAsync(request));
    }

    [Fact]
    public async Task MergeAsync_WithNonExistentFile_Throws()
    {
        var request = new MergeRequest(
            new[] { "nonexistent1.pdf", "nonexistent2.pdf" },
            "out.pdf");

        await Should.ThrowAsync<FileNotFoundException>(
            () => _merger.MergeAsync(request));
    }
}
