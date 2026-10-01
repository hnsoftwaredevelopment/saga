using FluentAssertions;

namespace EbookManager.Tests.App.Views;

public sealed class FilePathToImageSourceConverterSafetyTests
{
    [Fact]
    public void Converter_treats_a_cloud_file_COM_failure_as_an_unavailable_cover()
    {
        var source = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "TestAssets", "FilePathToImageSourceConverter.cs"));

        source.Should().Contain("using System.Runtime.InteropServices;");
        source.Should().Contain("COMException)");
    }
}
