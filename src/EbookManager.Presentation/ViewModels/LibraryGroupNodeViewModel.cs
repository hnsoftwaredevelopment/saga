using System.Collections.ObjectModel;

namespace EbookManager.Presentation.ViewModels;

public sealed class LibraryGroupNodeViewModel
{
    public LibraryGroupNodeViewModel(
        string header,
        IEnumerable<LibraryGroupNodeViewModel> groups,
        IEnumerable<BookRowViewModel> books)
    {
        var groupItems = groups.ToArray();
        var bookItems = books.ToArray();

        Header = header;
        Groups = new ObservableCollection<LibraryGroupNodeViewModel>(groupItems);
        Books = new ObservableCollection<BookRowViewModel>(bookItems);
        BookCount = CountUniqueBooks(groupItems, bookItems);
    }

    public string Header { get; }
    public ObservableCollection<LibraryGroupNodeViewModel> Groups { get; }
    public ObservableCollection<BookRowViewModel> Books { get; }
    public int BookCount { get; }
    public bool HasGroups => Groups.Count > 0;
    public bool HasBooks => Books.Count > 0;

    private static int CountUniqueBooks(
        IEnumerable<LibraryGroupNodeViewModel> groups,
        IEnumerable<BookRowViewModel> books) =>
        books.Select(book => book.Id)
            .Concat(groups.SelectMany(group => group.GetBookIds()))
            .Distinct()
            .Count();

    private IEnumerable<Guid> GetBookIds() =>
        Books.Select(book => book.Id)
            .Concat(Groups.SelectMany(group => group.GetBookIds()));
}
