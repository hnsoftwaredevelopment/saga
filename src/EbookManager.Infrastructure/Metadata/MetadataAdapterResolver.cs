using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Books;
using EbookManager.Domain.Metadata;

namespace EbookManager.Infrastructure.Metadata;

public sealed class MetadataAdapterResolver
{
    private readonly IReadOnlyList<IMetadataAdapter> specificAdapters;
    private readonly IMetadataAdapter fallbackAdapter;

    public MetadataAdapterResolver(IEnumerable<IMetadataAdapter> adapters)
    {
        var adapterList = adapters.ToList();
        specificAdapters = adapterList
            .Where(adapter => adapter is not FallbackMetadataAdapter)
            .ToArray();
        fallbackAdapter = adapterList.OfType<FallbackMetadataAdapter>().LastOrDefault() ?? new FallbackMetadataAdapter();
    }

    public IMetadataAdapter Resolve(EbookFormat format)
    {
        var specificAdapter = specificAdapters.FirstOrDefault(adapter => adapter.CanHandle(format));
        return specificAdapter ?? fallbackAdapter;
    }
}
