using EbookManager.Domain.Libraries;

namespace EbookManager.Libraries;

public sealed class CurrentLibrary
{
    private LibraryDescriptor? current;

    public event EventHandler? Changed;

    public LibraryDescriptor? Current => current;

    public void Set(LibraryDescriptor? library)
    {
        if (EqualityComparer<LibraryDescriptor?>.Default.Equals(current, library))
        {
            return;
        }

        current = library;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear() => Set(null);
}
