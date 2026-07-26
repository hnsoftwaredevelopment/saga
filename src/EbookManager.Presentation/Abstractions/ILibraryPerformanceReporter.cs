using EbookManager.Presentation.ViewModels;

namespace EbookManager.Presentation.Abstractions;

public interface ILibraryPerformanceReporter
{
    void Report(LibraryPerformanceSnapshot snapshot);
}
