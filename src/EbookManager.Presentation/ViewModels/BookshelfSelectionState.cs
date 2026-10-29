namespace EbookManager.Presentation.ViewModels;

public sealed class BookshelfSelectionState
{
    private Guid? anchor;
    public void Reset() => anchor = null;
    public IReadOnlyList<Guid> Select(IReadOnlyList<Guid> order, IEnumerable<Guid> selected, Guid target, bool control, bool shift)
    {
        var unique = order.Distinct().ToArray();
        var targetIndex = Array.IndexOf(unique, target);
        if (targetIndex < 0) return selected.Distinct().ToArray();
        var result = control ? selected.ToHashSet() : [];
        if (shift)
        {
            var start = anchor is { } id ? Array.IndexOf(unique, id) : -1;
            if (start < 0) { start = targetIndex; anchor = target; }
            foreach (var item in unique.Skip(Math.Min(start, targetIndex)).Take(Math.Abs(start - targetIndex) + 1)) result.Add(item);
        }
        else
        {
            anchor = target;
            if (!control || !result.Remove(target)) result.Add(target);
        }
        // Explicit selections in collapsed groups survive an additive gesture, but never enter a new range.
        return unique.Where(result.Contains).Concat(result.Except(unique)).ToArray();
    }
}
