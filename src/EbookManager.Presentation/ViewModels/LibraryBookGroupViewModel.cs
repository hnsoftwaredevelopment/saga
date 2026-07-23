using System.Collections.ObjectModel;

namespace EbookManager.Presentation.ViewModels;

public sealed class LibraryBookGroupViewModel(string header, IEnumerable<BookRowViewModel> books)
{
    public string Header { get; } = header;
    public ObservableCollection<BookRowViewModel> Books { get; } = new(books);
}
